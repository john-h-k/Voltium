using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Voltium.Core;
using Voltium.Core.GpuResources;
using Voltium.Core.Managers;
using Voltium.Core.Managers.Shaders;
using Voltium.Core.Memory.GpuResources;
using Voltium.Core.Pipeline;
using static Voltium.Core.Pipeline.GraphicsPipelineDesc;
using Buffer = Voltium.Core.Memory.GpuResources.Buffer;

namespace Voltium.Interactive
{
    public sealed unsafe class MandelbrotRenderer : Renderer
    {
        private Texture _renderTarget;
        private Texture _ssaaRenderTarget;
        private DescriptorHandle _renderTargetView;
        private DescriptorHandle _ssaaRenderTargetView;
        private DescriptorHandle _shaderResourceView;
        private Buffer _colors;
        private GraphicsDevice _device = null!;

        private uint SsaaFactor { get; set; } = 8;

        public override void Init(GraphicsDevice device, GraphicalConfiguration config, in ScreenData screen)
        {
            _device = device;
            //uint width = 16384, height = 16384;
            uint width = screen.Width, height = screen.Height;
            var target = TextureDesc.CreateRenderTargetDesc(DataFormat.R8G8B8A8UnsignedNormalized, height, width, RgbaColor.Black);

            _renderTarget = device.Allocator.AllocateTexture(target, ResourceState.RenderTarget);

            target.Width *= SsaaFactor;
            target.Height *= SsaaFactor;
            _ssaaRenderTarget = device.Allocator.AllocateTexture(target, ResourceState.RenderTarget);

            _renderTargetView = device.CreateRenderTargetView(_renderTarget);
            _ssaaRenderTargetView = device.CreateRenderTargetView(_ssaaRenderTarget);
            _shaderResourceView = device.CreateShaderResourceView(_ssaaRenderTarget);

            using (var copy = device.BeginCopyContext())
            {
                copy.UploadBuffer(device.Allocator, _rgbaColors, out _colors);
            }

            var @params = new RootParameter[]
            {
                RootParameter.CreateDescriptor(RootParameterType.ShaderResourceView, 0, 0, ShaderVisibility.Pixel)
            };

            var rootSig = RootSignature.Create(device, @params, null);
            PipelineManager.Reset();

            var psoDesc = new GraphicsPipelineDesc
            {
                RootSignature = rootSig,
                Topology = TopologyClass.Triangle,
                DepthStencil = DepthStencilDesc.DisableDepthStencil,
                RenderTargetFormats = new FormatBuffer8(_renderTarget.Format),
                VertexShader = ShaderManager.CompileShader("Shaders/Mandelbrot/EntireScreenCopyVS.hlsl", DxcCompileTarget.Vs_6_0),
                PixelShader = ShaderManager.CompileShader("Shaders/Mandelbrot/Mandelbrot.hlsl", DxcCompileTarget.Ps_6_0)
            };

            _pso = PipelineManager.CreatePso(device, "Mandelbrot", psoDesc);

            @params = new RootParameter[]
            {
                RootParameter.CreateDescriptorTable(DescriptorRangeType.ShaderResourceView, 0, 1, 0, visibility: ShaderVisibility.Pixel),
                RootParameter.CreateConstants(1, 0, 0, ShaderVisibility.Pixel)
            };

            var samplers = new StaticSampler[]
            {
                new StaticSampler(TextureAddressMode.BorderColor, SamplerFilterType.Anistropic, 0, 0, ShaderVisibility.Pixel, StaticSampler.OpaqueWhite)
            };

            var ssaaRootSig = RootSignature.Create(device, @params, samplers);
            psoDesc.RootSignature = ssaaRootSig;
            psoDesc.PixelShader = ShaderManager.CompileShader("Shaders/Mandelbrot/SSAA.hlsl", DxcCompileTarget.Ps_6_0);
            _ssaaPso = PipelineManager.CreatePso(device, "SSAA", psoDesc);
        }

        private GraphicsPso _pso = null!;
        private GraphicsPso _ssaaPso = null!;

        public override PipelineStateObject? GetInitialPso()
            => _pso;

        public override void Render(ref GraphicsContext recorder)
        {
            recorder.ResourceTransition(_ssaaRenderTarget, ResourceState.RenderTarget);

            ScreenData d = new (_device.ScreenData.Height * SsaaFactor, _device.ScreenData.Width * SsaaFactor);
            recorder.SetViewportAndScissor(d.Width, d.Height);
            recorder.SetRenderTargets(_ssaaRenderTargetView);
            recorder.SetTopology(Topology.TriangeList);
            recorder.SetBuffer(0, _colors);
            recorder.Draw(3);

            recorder.ResourceTransition(_renderTarget, ResourceState.RenderTarget);
            recorder.ResourceTransition(_ssaaRenderTarget, ResourceState.PixelShaderResource);

            recorder.SetViewportAndScissor(_device.ScreenData.Width, _device.ScreenData.Height);
            recorder.SetPipelineState(_ssaaPso);
            recorder.SetRootSignature(_ssaaPso.Desc.RootSignature);
            recorder.SetRenderTargets(_renderTargetView);
            recorder.SetRootDescriptorTable(0, _shaderResourceView);
            recorder.SetRoot32BitConstant(1, 1);
            recorder.Draw(3);

            recorder.CopyResource(_renderTarget, _device.BackBuffer);

            //recorder.CopyResource(_ssaaRenderTarget, _device.BackBuffer);
            recorder.ResourceTransition(_device.BackBuffer, ResourceState.Present);
        }

        public override void Update(ApplicationTimer timer)
        {
        }

        public override void Dispose()
        {
            _renderTarget.Dispose();
            _ssaaRenderTarget.Dispose();
            _colors.Dispose();
        }

        public override void ToggleMsaa()
        {
            if (SsaaFactor != 1)
            {
                SsaaFactor = 1;
            }
            else
            {
                SsaaFactor = 4;
            }
        }

        private RgbaColor[] _rgbaColors = new RgbaColor[]
        {
            RgbaColor.FromPacked(0x00, 0x00, 0x00, 0xFF),
            RgbaColor.FromPacked(0x58, 0x10, 0x50, 0xFF),
            RgbaColor.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
            RgbaColor.FromPacked(0x40, 0x24, 0x64, 0xFF),
            RgbaColor.FromPacked(0x34, 0x30, 0x70, 0xFF),
            RgbaColor.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            RgbaColor.FromPacked(0x18, 0x48, 0x88, 0xFF),
            RgbaColor.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            RgbaColor.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
            RgbaColor.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            RgbaColor.FromPacked(0x1c, 0x44, 0x84, 0xFF),
            RgbaColor.FromPacked(0x28, 0x38, 0x78, 0xFF),
            RgbaColor.FromPacked(0x38, 0x2c, 0x6c, 0xFF),
            RgbaColor.FromPacked(0x44, 0x20, 0x60, 0xFF),
            RgbaColor.FromPacked(0x54, 0x14, 0x54, 0xFF),
            RgbaColor.FromPacked(0x60, 0x08, 0x48, 0xFF),
            RgbaColor.FromPacked(0x70, 0x0c, 0x40, 0xFF),
            RgbaColor.FromPacked(0x90, 0x14, 0x34, 0xFF),
            RgbaColor.FromPacked(0xac, 0x1c, 0x24, 0xFF),
            RgbaColor.FromPacked(0xcc, 0x24, 0x18, 0xFF),
            RgbaColor.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
            RgbaColor.FromPacked(0xec, 0x58, 0x08, 0xFF),
            RgbaColor.FromPacked(0xf4, 0x84, 0x04, 0xFF),
            RgbaColor.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
            RgbaColor.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
            RgbaColor.FromPacked(0xf0, 0x68, 0x08, 0xFF),
            RgbaColor.FromPacked(0xec, 0x40, 0x08, 0xFF),
            RgbaColor.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
            RgbaColor.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
            RgbaColor.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
            RgbaColor.FromPacked(0x80, 0x0c, 0x38, 0xFF),
            RgbaColor.FromPacked(0x60, 0x08, 0x48, 0xFF),
            RgbaColor.FromPacked(0x58, 0x10, 0x50, 0xFF),
            RgbaColor.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
            RgbaColor.FromPacked(0x40, 0x24, 0x64, 0xFF),
            RgbaColor.FromPacked(0x34, 0x30, 0x70, 0xFF),
            RgbaColor.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            RgbaColor.FromPacked(0x18, 0x48, 0x88, 0xFF),
            RgbaColor.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            RgbaColor.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
            RgbaColor.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            RgbaColor.FromPacked(0x18, 0x48, 0x88, 0xFF),
            RgbaColor.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            RgbaColor.FromPacked(0x30, 0x34, 0x74, 0xFF),
            RgbaColor.FromPacked(0x3c, 0x28, 0x68, 0xFF),
            RgbaColor.FromPacked(0x48, 0x1c, 0x5c, 0xFF),
            RgbaColor.FromPacked(0x54, 0x14, 0x54, 0xFF),
            RgbaColor.FromPacked(0x60, 0x08, 0x48, 0xFF),
            RgbaColor.FromPacked(0x70, 0x0c, 0x40, 0xFF),
            RgbaColor.FromPacked(0x90, 0x14, 0x34, 0xFF),
            RgbaColor.FromPacked(0xac, 0x1c, 0x24, 0xFF),
            RgbaColor.FromPacked(0xcc, 0x24, 0x18, 0xFF),
            RgbaColor.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
            RgbaColor.FromPacked(0xec, 0x58, 0x08, 0xFF),
            RgbaColor.FromPacked(0xf4, 0x84, 0x04, 0xFF),
            RgbaColor.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
            RgbaColor.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
            RgbaColor.FromPacked(0xf0, 0x68, 0x08, 0xFF),
            RgbaColor.FromPacked(0xec, 0x40, 0x08, 0xFF),
            RgbaColor.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
            RgbaColor.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
            RgbaColor.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
            RgbaColor.FromPacked(0x80, 0x0c, 0x38, 0xFF),
            RgbaColor.FromPacked(0x60, 0x08, 0x48, 0xFF),
            RgbaColor.FromPacked(0x58, 0x10, 0x50, 0xFF),
            RgbaColor.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
            RgbaColor.FromPacked(0x40, 0x24, 0x64, 0xFF),
            RgbaColor.FromPacked(0x34, 0x30, 0x70, 0xFF),
            RgbaColor.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            RgbaColor.FromPacked(0x18, 0x48, 0x88, 0xFF),
            RgbaColor.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            RgbaColor.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
            RgbaColor.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            RgbaColor.FromPacked(0x18, 0x48, 0x88, 0xFF),
            RgbaColor.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            RgbaColor.FromPacked(0x30, 0x34, 0x74, 0xFF),
            RgbaColor.FromPacked(0x3c, 0x28, 0x68, 0xFF),
            RgbaColor.FromPacked(0x48, 0x1c, 0x5c, 0xFF),
            RgbaColor.FromPacked(0x54, 0x14, 0x54, 0xFF),
            RgbaColor.FromPacked(0x60, 0x08, 0x48, 0xFF),
            RgbaColor.FromPacked(0x70, 0x0c, 0x40, 0xFF),
            RgbaColor.FromPacked(0x90, 0x14, 0x34, 0xFF),
            RgbaColor.FromPacked(0xac, 0x1c, 0x24, 0xFF),
            RgbaColor.FromPacked(0xcc, 0x24, 0x18, 0xFF),
            RgbaColor.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
            RgbaColor.FromPacked(0xec, 0x58, 0x08, 0xFF),
            RgbaColor.FromPacked(0xf4, 0x84, 0x04, 0xFF),
            RgbaColor.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
            RgbaColor.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
            RgbaColor.FromPacked(0xf0, 0x68, 0x08, 0xFF),
            RgbaColor.FromPacked(0xec, 0x40, 0x08, 0xFF),
            RgbaColor.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
            RgbaColor.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
            RgbaColor.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
            RgbaColor.FromPacked(0x80, 0x0c, 0x38, 0xFF),
            RgbaColor.FromPacked(0x60, 0x08, 0x48, 0xFF),
            RgbaColor.FromPacked(0x58, 0x10, 0x50, 0xFF),
            RgbaColor.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
            RgbaColor.FromPacked(0x40, 0x24, 0x64, 0xFF),
            RgbaColor.FromPacked(0x34, 0x30, 0x70, 0xFF),
            RgbaColor.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            RgbaColor.FromPacked(0x18, 0x48, 0x88, 0xFF),
            RgbaColor.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            RgbaColor.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
            RgbaColor.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            RgbaColor.FromPacked(0x18, 0x48, 0x88, 0xFF),
            RgbaColor.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            RgbaColor.FromPacked(0x30, 0x34, 0x74, 0xFF),
            RgbaColor.FromPacked(0x3c, 0x28, 0x68, 0xFF),
            RgbaColor.FromPacked(0x48, 0x1c, 0x5c, 0xFF),
            RgbaColor.FromPacked(0x54, 0x14, 0x54, 0xFF),
            RgbaColor.FromPacked(0x60, 0x08, 0x48, 0xFF),
            RgbaColor.FromPacked(0x70, 0x0c, 0x40, 0xFF),
            RgbaColor.FromPacked(0x90, 0x14, 0x34, 0xFF),
            RgbaColor.FromPacked(0xac, 0x1c, 0x24, 0xFF),
            RgbaColor.FromPacked(0xcc, 0x24, 0x18, 0xFF),
            RgbaColor.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
            RgbaColor.FromPacked(0xec, 0x58, 0x08, 0xFF),
            RgbaColor.FromPacked(0xf4, 0x84, 0x04, 0xFF),
            RgbaColor.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
            RgbaColor.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
            RgbaColor.FromPacked(0xf0, 0x68, 0x08, 0xFF),
            RgbaColor.FromPacked(0xec, 0x40, 0x08, 0xFF),
            RgbaColor.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
            RgbaColor.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
            RgbaColor.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
            RgbaColor.FromPacked(0x80, 0x0c, 0x38, 0xFF),
            RgbaColor.FromPacked(0x60, 0x08, 0x48, 0xFF),
            RgbaColor.FromPacked(0x58, 0x10, 0x50, 0xFF),
            RgbaColor.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
            RgbaColor.FromPacked(0x40, 0x24, 0x64, 0xFF),
            RgbaColor.FromPacked(0x34, 0x30, 0x70, 0xFF),
            RgbaColor.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            RgbaColor.FromPacked(0x18, 0x48, 0x88, 0xFF),
            RgbaColor.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            RgbaColor.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
            RgbaColor.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            RgbaColor.FromPacked(0x18, 0x48, 0x88, 0xFF),
            RgbaColor.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            RgbaColor.FromPacked(0x30, 0x34, 0x74, 0xFF),
            RgbaColor.FromPacked(0x3c, 0x28, 0x68, 0xFF),
            RgbaColor.FromPacked(0x48, 0x1c, 0x5c, 0xFF),
            RgbaColor.FromPacked(0x54, 0x14, 0x54, 0xFF),
            RgbaColor.FromPacked(0x60, 0x08, 0x48, 0xFF),
            RgbaColor.FromPacked(0x70, 0x0c, 0x40, 0xFF),
            RgbaColor.FromPacked(0x90, 0x14, 0x34, 0xFF),
            RgbaColor.FromPacked(0xac, 0x1c, 0x24, 0xFF),
            RgbaColor.FromPacked(0xcc, 0x24, 0x18, 0xFF),
            RgbaColor.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
            RgbaColor.FromPacked(0xec, 0x58, 0x08, 0xFF),
            RgbaColor.FromPacked(0xf4, 0x84, 0x04, 0xFF),
            RgbaColor.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
            RgbaColor.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
            RgbaColor.FromPacked(0xf0, 0x68, 0x08, 0xFF),
            RgbaColor.FromPacked(0xec, 0x40, 0x08, 0xFF),
            RgbaColor.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
            RgbaColor.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
            RgbaColor.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
            RgbaColor.FromPacked(0x80, 0x0c, 0x38, 0xFF),
            RgbaColor.FromPacked(0x60, 0x08, 0x48, 0xFF),
            RgbaColor.FromPacked(0x58, 0x10, 0x50, 0xFF),
            RgbaColor.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
            RgbaColor.FromPacked(0x40, 0x24, 0x64, 0xFF),
            RgbaColor.FromPacked(0x34, 0x30, 0x70, 0xFF),
            RgbaColor.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            RgbaColor.FromPacked(0x18, 0x48, 0x88, 0xFF),
            RgbaColor.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            RgbaColor.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
            RgbaColor.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            RgbaColor.FromPacked(0x18, 0x48, 0x88, 0xFF),
            RgbaColor.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            RgbaColor.FromPacked(0x30, 0x34, 0x74, 0xFF),
            RgbaColor.FromPacked(0x3c, 0x28, 0x68, 0xFF),
            RgbaColor.FromPacked(0x48, 0x1c, 0x5c, 0xFF),
            RgbaColor.FromPacked(0x54, 0x14, 0x54, 0xFF),
            RgbaColor.FromPacked(0x60, 0x08, 0x48, 0xFF),
            RgbaColor.FromPacked(0x70, 0x0c, 0x40, 0xFF),
            RgbaColor.FromPacked(0x90, 0x14, 0x34, 0xFF),
            RgbaColor.FromPacked(0xac, 0x1c, 0x24, 0xFF),
            RgbaColor.FromPacked(0xcc, 0x24, 0x18, 0xFF),
            RgbaColor.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
            RgbaColor.FromPacked(0xec, 0x58, 0x08, 0xFF),
            RgbaColor.FromPacked(0xf4, 0x84, 0x04, 0xFF),
            RgbaColor.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
            RgbaColor.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
            RgbaColor.FromPacked(0xf0, 0x68, 0x08, 0xFF),
            RgbaColor.FromPacked(0xec, 0x40, 0x08, 0xFF),
            RgbaColor.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
            RgbaColor.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
            RgbaColor.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
            RgbaColor.FromPacked(0x80, 0x0c, 0x38, 0xFF),
            RgbaColor.FromPacked(0x60, 0x08, 0x48, 0xFF),
            RgbaColor.FromPacked(0x58, 0x10, 0x50, 0xFF),
            RgbaColor.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
            RgbaColor.FromPacked(0x40, 0x24, 0x64, 0xFF),
            RgbaColor.FromPacked(0x34, 0x30, 0x70, 0xFF),
            RgbaColor.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            RgbaColor.FromPacked(0x18, 0x48, 0x88, 0xFF),
            RgbaColor.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            RgbaColor.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
            RgbaColor.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            RgbaColor.FromPacked(0x18, 0x48, 0x88, 0xFF),
            RgbaColor.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            RgbaColor.FromPacked(0x30, 0x34, 0x74, 0xFF),
            RgbaColor.FromPacked(0x3c, 0x28, 0x68, 0xFF),
            RgbaColor.FromPacked(0x48, 0x1c, 0x5c, 0xFF),
            RgbaColor.FromPacked(0x54, 0x14, 0x54, 0xFF),
            RgbaColor.FromPacked(0x60, 0x08, 0x48, 0xFF),
            RgbaColor.FromPacked(0x70, 0x0c, 0x40, 0xFF),
            RgbaColor.FromPacked(0x90, 0x14, 0x34, 0xFF),
            RgbaColor.FromPacked(0xac, 0x1c, 0x24, 0xFF),
            RgbaColor.FromPacked(0xcc, 0x24, 0x18, 0xFF),
            RgbaColor.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
            RgbaColor.FromPacked(0xec, 0x58, 0x08, 0xFF),
            RgbaColor.FromPacked(0xf4, 0x84, 0x04, 0xFF),
            RgbaColor.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
            RgbaColor.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
            RgbaColor.FromPacked(0xf0, 0x68, 0x08, 0xFF),
            RgbaColor.FromPacked(0xec, 0x40, 0x08, 0xFF),
            RgbaColor.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
            RgbaColor.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
            RgbaColor.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
            RgbaColor.FromPacked(0x80, 0x0c, 0x38, 0xFF),
            RgbaColor.FromPacked(0x60, 0x08, 0x48, 0xFF),
            RgbaColor.FromPacked(0x58, 0x10, 0x50, 0xFF),
            RgbaColor.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
            RgbaColor.FromPacked(0x40, 0x24, 0x64, 0xFF),
            RgbaColor.FromPacked(0x34, 0x30, 0x70, 0xFF),
            RgbaColor.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            RgbaColor.FromPacked(0x18, 0x48, 0x88, 0xFF),
            RgbaColor.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            RgbaColor.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
            RgbaColor.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            RgbaColor.FromPacked(0x18, 0x48, 0x88, 0xFF),
            RgbaColor.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            RgbaColor.FromPacked(0x30, 0x34, 0x74, 0xFF),
            RgbaColor.FromPacked(0x3c, 0x28, 0x68, 0xFF),
            RgbaColor.FromPacked(0x48, 0x1c, 0x5c, 0xFF),
            RgbaColor.FromPacked(0x54, 0x14, 0x54, 0xFF),
            RgbaColor.FromPacked(0x60, 0x08, 0x48, 0xFF),
            RgbaColor.FromPacked(0x70, 0x0c, 0x40, 0xFF),
            RgbaColor.FromPacked(0x90, 0x14, 0x34, 0xFF),
            RgbaColor.FromPacked(0xac, 0x1c, 0x24, 0xFF),
            RgbaColor.FromPacked(0xcc, 0x24, 0x18, 0xFF),
            RgbaColor.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
            RgbaColor.FromPacked(0xec, 0x58, 0x08, 0xFF),
            RgbaColor.FromPacked(0xf4, 0x84, 0x04, 0xFF),
            RgbaColor.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
            RgbaColor.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
            RgbaColor.FromPacked(0xf0, 0x68, 0x08, 0xFF),
            RgbaColor.FromPacked(0xec, 0x40, 0x08, 0xFF),
            RgbaColor.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
            RgbaColor.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
            RgbaColor.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
            RgbaColor.FromPacked(0x80, 0x0c, 0x38, 0xFF),
            RgbaColor.FromPacked(0x60, 0x08, 0x48, 0xFF)
        };
    }
}
