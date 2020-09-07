using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Core;
using Voltium.Core.Memory;
using Voltium.Core.Devices;
using Voltium.Core.Pipeline;

using Buffer = Voltium.Core.Memory.Buffer;
using SixLabors.ImageSharp;
using TerraFX.Interop;

namespace Voltium.Interactive.GalaxyRenderer
{
    internal class GalaxyRenderer
    {
        private GraphicsDevice _device;

        private RootSignature _rootSig;
        private PipelineStateObject _renderPso;

        private struct Pso : IPipelineStreamType
        {
            public RootSignatureElement RootSignature;
            public DepthStencilDesc DepthStencil;
            public CompiledShader VertexShader;
            public CompiledShader PixelShader;
        }

        public GalaxyRenderer()
        {
            DebugLayerConfiguration debug =
#if DEBUG
                DebugLayerConfiguration.Debug;
#else
                DebugLayerConfiguration.None;
#endif

            _device = GraphicsDevice.Create(FeatureLevel.ComputeLevel1_0, null, debug);


            var @params = new RootParameter[]
            {
                RootParameter.CreateDescriptorTable(DescriptorRangeType.ShaderResourceView, 0, 1, 0, visibility: ShaderVisibility.Pixel)
            };

            Windows.d3d12_max_di

            _rootSig = _device.CreateRootSignature(@params);

            var flags = Array.Empty<ShaderCompileFlag>();

            var renderPso = new Pso
            {
                RootSignature = _rootSig,
                DepthStencil = DepthStencilDesc.DisableDepthStencil,
                VertexShader = ShaderManager.CompileShader("Shaders/Fullscreen.hlsl", ShaderType.Vertex, flags),
                PixelShader = ShaderManager.CompileShader("Shaders/GalaxyShader.hlsl", ShaderType.Vertex, flags),
            };

            _renderPso = _device.PipelineManager.CreatePipelineStateObject(renderPso, nameof(_renderPso));
        }

        private Buffer _rawData;
        private Texture _renderTarget;
        private DescriptorHandle _renderTargetView;

        public void Render(in Viewport viewport)
        {
            var context = _device.BeginGraphicsContext(_renderPso);

            context.SetAndClearRenderTarget(_renderTargetView, Rgba128.Black);
            context.SetViewports(viewport);
            context.SetScissorRectangles(_renderTarget.Resolution);

            context.SetPipelineState(_renderPso);
            context.SetRootSignature(_rootSig);
            context.SetShaderResourceBuffer(0, _rawData);
            context.SetTopology(Topology.TriangleList);

            context.Draw(3);

            _device.Execute(context).Block();
        }
    }
}
