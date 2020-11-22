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
using Voltium.Core.Contexts;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;
using Voltium.TextureLoading;

namespace Voltium.Interactive.FloatMultiplySample
{
    //public sealed class D3D12TextureDecoder : IImageDecoder
    //{
    //    public Image<TPixel> Decode<TPixel>(Configuration configuration, Stream stream) where TPixel : unmanaged, IPixel<TPixel>
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Image Decode(Configuration configuration, Stream stream)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Task<Image<TPixel>> DecodeAsync<TPixel>(Configuration configuration, Stream stream) where TPixel : unmanaged, IPixel<TPixel>
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public Task<Image> DecodeAsync(Configuration configuration, Stream stream)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    public unsafe class ImageBlurApp
    {
        private struct Settings
        {
            public int BlurRadius;
            public fixed float Weights[11];
        }

        private ComputeDevice _device;
        private PipelineStateObject _horizontalBlurPso;
        private PipelineStateObject _verticalBlurPso;
        private Settings _settings;

        public ImageBlurApp()
        {
            _device = GraphicsDevice.Create(FeatureLevel.GraphicsLevel11_0, null);

            var rootParams = new RootParameter[]
            {
                RootParameter.CreateConstants<Settings>(0, 0),
                RootParameter.CreateDescriptorTable(DescriptorRangeType.ShaderResourceView, 0, 1, 0),
                RootParameter.CreateDescriptorTable(DescriptorRangeType.UnorderedAccessView, 0, 1, 1)
            };

            var rootSig = _device.CreateRootSignature(rootParams);

            var psoDesc = new ComputePipelineDesc
            {
                RootSignature = rootSig,
                ComputeShader = ShaderManager.CompileShader("ImageBlur/ImageBlur.hlsl", ShaderType.Compute, entrypoint: "BlurHorizontal"),
            };
            _horizontalBlurPso = _device.PipelineManager.CreatePipelineStateObject(psoDesc, "BlurHorizontal");

            psoDesc = new ComputePipelineDesc
            {
                RootSignature = rootSig,
                ComputeShader = ShaderManager.CompileShader("ImageBlur/ImageBlur.hlsl", ShaderType.Compute, entrypoint: "BlurVertical"),
            };
            _verticalBlurPso = _device.PipelineManager.CreatePipelineStateObject(psoDesc, "BlurVertical");

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

        private (Texture Source, Texture Dest, string Name)[] _images = null!;

        public void Setup()
        {
            _images = new (Texture Source, Texture Dest, string Name)[1];
            var bricks = TextureLoader.LoadTextureDesc("Assets/Textures/bricks3bgra.dds");

            using (var upload = _device.BeginUploadContext())
            {
                var src = upload.UploadTexture(bricks);
                _images[0].Source = src;
                _images[0].Dest = _device.Allocator.AllocateTexture(TextureDesc.CreateUnorderedAccessResourceDesc(src.Format, src.Dimension, src.Width, src.Height, src.DepthOrArraySize), ResourceState.NonPixelShaderResource);
                _images[0].Name = "bricks";
            }
        }

        public void Run()
        {
            Setup();

            foreach (var (src, dest, name) in _images)
            {
                BlurImage(src, dest);

                var memory = MemoryPool<Bgra32>.Shared.Rent((int)_device.GetRequiredSize(dest, 1));

                using (var data = Image.LoadPixelData((ReadOnlySpan<Bgra32>)memory.Memory.Span, (int)dest.Width, (int)dest.Height))
                {
                    data.Save($"blur_{name}.bmp");
                    using (var readback = _device.BeginReadbackContext())
                    {

                    }
                }

                using (var data = Image.LoadPixelData((ReadOnlySpan<Bgra32>)memory.Memory.Span, (int)dest.Width, (int)dest.Height))
                {
                    data.Save($"blur_{name}.bmp");
                }
            }
        }

        public void BlurImage(in Texture source, in Texture dest)
        {
            var srcView = _device.CreateShaderResourceView(source);
            var destView = _device.CreateUnorderedAccessView(dest);

            var context = _device.BeginComputeContext(_horizontalBlurPso);

            context.SetRoot32BitConstants(0, _settings);
            context.SetRootDescriptorTable(1, srcView);
            context.SetRootDescriptorTable(2, destView);

            uint x = (uint)MathF.Ceiling(source.Width / 256.0f);
            context.Dispatch(x, source.Height, 1);

            context.SetPipelineState(_verticalBlurPso);

            uint y = (uint)MathF.Ceiling(source.Height / 256.0f);
            context.Dispatch((uint)source.Width, y, 1);

            context.TransitionForCrossContextAccess(source, ResourceState.UnorderedAccess);

            context.Close();

            _device.Execute(context).Block();
        }
    }
}
