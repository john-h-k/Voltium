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
    public unsafe class ImageBlurApp
    {
        private struct Settings
        {
            public int BlurRadius;
            public fixed float Weights[11];
        }

        private GraphicsDevice _device;
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

            _settings = new Settings
            {
                BlurRadius = 8
            };
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

                IMemoryOwner<byte> tex;
                using (var readback = _device.BeginReadbackContext(ContextFlags.BlockOnClose))
                {
                    tex = readback.ReadbackTexture(dest);
                }

                using (var data = Image.LoadPixelData<Bgra32>(tex.Memory.Span, (int)dest.Width, (int)dest.Height))
                {
                    data.Save($"blur_{name}.bmp");
                }
            }
        }

        public void BlurImage(in Texture source, in Texture dest)
        {
            var views = _device.AllocateResourceDescriptors(2);

            _device.CreateShaderResourceView(source, views[0]);
            _device.CreateUnorderedAccessView(dest, views[1]);

            var context = _device.BeginComputeContext(_horizontalBlurPso);

            context.SetRoot32BitConstants(0, _settings);
            context.SetRootDescriptorTable(1, views[0]);
            context.SetRootDescriptorTable(2, views[1]);

            uint x = (uint)MathF.Ceiling(source.Width / 256.0f);
            context.Dispatch(x, source.Height);

            context.SetPipelineState(_verticalBlurPso);

            uint y = (uint)MathF.Ceiling(source.Height / 256.0f);
            context.Dispatch((uint)source.Width, y);

            context.TransitionForCrossContextAccess(source, ResourceState.UnorderedAccess);

            context.Attach(ref views);

            context.Close();

            _device.Execute(context).Block();
        }
    }
}
