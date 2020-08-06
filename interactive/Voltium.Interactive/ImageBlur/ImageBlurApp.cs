using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iced.Intel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using Voltium.Core;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;
using Voltium.TextureLoading;

namespace Voltium.Interactive.FloatMultiplySample
{
    public sealed class D3D12TextureDecoder : IImageDecoder
    {
        public Image<TPixel> Decode<TPixel>(Configuration configuration, Stream stream) where TPixel : unmanaged, IPixel<TPixel>
        {
            throw new NotImplementedException();
        }

        public Image Decode(Configuration configuration, Stream stream)
        {
            throw new NotImplementedException();
        }

        public Task<Image<TPixel>> DecodeAsync<TPixel>(Configuration configuration, Stream stream) where TPixel : unmanaged, IPixel<TPixel>
        {
            throw new NotImplementedException();
        }

        public Task<Image> DecodeAsync(Configuration configuration, Stream stream)
        {
            throw new NotImplementedException();
        }
    }

    public unsafe class ImageBlurApp
    {
        private struct Settings
        {
            public int BlurRadius;
            public fixed float Weights[11];
        }

        private ComputeDevice _device;
        private ComputePipelineStateObject _horizontalBlurPso;
        private ComputePipelineStateObject _verticalBlurPso;
        private Settings _settings;

        public ImageBlurApp()
        {
            _device = new ComputeDevice(DeviceConfiguration.DefaultCompute, null);

            var rootParams = new RootParameter[]
            {
                RootParameter.CreateConstants(12, 0, 0),
                RootParameter.CreateDescriptorTable(DescriptorRangeType.UnorderedAccessView, 0, 1, 0)
            };

            var rootSig = _device.CreateRootSignature(rootParams);

            var psoDesc = new ComputePipelineDesc
            {
                RootSignature = rootSig,
                ComputeShader = ShaderManager.CompileShader("ImageBlur/ImageBlur.hlsl", ShaderType.Compute, entrypoint: "BlurHorizontal"),
            };
            _horizontalBlurPso = _device.PipelineManager.CreatePipelineStateObject("BlurHorizontal", psoDesc);

            psoDesc = new ComputePipelineDesc
            {
                RootSignature = rootSig,
                ComputeShader = ShaderManager.CompileShader("ImageBlur/ImageBlur.hlsl", ShaderType.Compute, entrypoint: "BlurVertical"),
            };
            _verticalBlurPso = _device.PipelineManager.CreatePipelineStateObject("BlurVertical", psoDesc);

            _settings = new Settings();
            _settings.BlurRadius = 8;
            GetWeights();
        }

        private void GetWeights()
        {
            float sigma = 2.5f;
            float twoSigma2 = 2.0f * sigma * sigma;

            // Estimate the blur radius based on sigma since sigma controls the "width" of the bell curve.
            // For example, for sigma = 3, the width of the bell curve is 
            int blurRadius = (int)MathF.Ceiling(2.0f * sigma);

            float weightSum = 0.0f;

            for (int i = -blurRadius; i <= blurRadius; ++i)
            {
                float x = i;

                _settings.Weights[i + blurRadius] = MathF.Exp(-x * x / twoSigma2);

                weightSum += _settings.Weights[i + blurRadius];
            }

            // Divide by the sum so all the weights add up to 1.0.
            for (int i = 0; i < 11; ++i)
            {
                _settings.Weights[i] /= weightSum;
            }
        }

        private (Texture Image, string Name)[] _images = null!;

        public void Setup()
        {
            _images = new (Texture Image, string Name)[1];
            var bricks = TextureLoader.LoadTextureDesc("Assets/Textures/bricks3bgra.dds");

            using (var upload = _device.BeginUploadContext())
            {
                _images[0].Image = upload.UploadTexture(bricks, ResourceFlags.AllowUnorderedAccess);
                _images[0].Name = "bricks";
            }
        }

        public void Run()
        {
            Setup();

            foreach (var (image, name) in _images)
            {
                BlurImage(image);

                var memory = MemoryPool<Bgra32>.Shared.Rent((int)_device.GetRequiredSize(image, 1));

                using (var data = Image.LoadPixelData((ReadOnlySpan<Bgra32>)memory.Memory.Span, (int)image.Width, (int)image.Height))
                {
                    data.Save($"blur_{name}.bmp");
                    using (var readback = _device.BeginReadbackContext())
                    {

                    }
                }

                using (var data = Image.LoadPixelData((ReadOnlySpan<Bgra32>)memory.Memory.Span, (int)image.Width, (int)image.Height))
                {
                    data.Save($"blur_{name}.bmp");
                }
            }
        }

        public void BlurImage(in Texture texture)
        {
            var view = _device.CreateUnorderedAccessView(texture);

            var context = _device.BeginComputeContext(_horizontalBlurPso);

            context.ResourceTransition(texture, ResourceState.UnorderedAccess);
            context.SetRoot32BitConstants(0, _settings);
            context.SetRootDescriptorTable(1, view);

            uint x = (uint)MathF.Ceiling(texture.Width / 256.0f);
            context.Dispatch(x, texture.Height, 1);

            context.SetPipelineState(_verticalBlurPso);

            uint y = (uint)MathF.Ceiling(texture.Height / 256.0f);
            context.Dispatch((uint)texture.Width, y, 1);

            context.TransitionForCrossContextAccess(texture);

            context.Close();

            _device.Execute(context).Block();
        }
    }
}
