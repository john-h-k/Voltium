//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Voltium.Core;
//using Voltium.Core.Memory;
//using Voltium.Core.Devices;
//using Voltium.Core.Pipeline;

//using Buffer = Voltium.Core.Memory.Buffer;
//using SixLabors.ImageSharp;
//using TerraFX.Interop;
//using System.Diagnostics.CodeAnalysis;
//using System.Numerics;

//namespace Voltium.Interactive.GalaxyRenderer
//{
//    internal class GalaxyRenderer
//    {
//        private GraphicsDevice _device;

//        private ComputePipelineStateObject _densityPso;
//        private GraphicsPipelineStateObject _renderPso;

//        public GalaxyRenderer()
//        {
//            DebugLayerConfiguration debug =
//#if DEBUG
//                DebugLayerConfiguration.Debug;
//#else
//                DebugLayerConfiguration.None;
//#endif

//            _device = GraphicsDevice.Create(FeatureLevel.GraphicsLevel11_0, null, debug);


            

//            //_renderPso = _device.PipelineManager.CreatePipelineStateObject(renderPso, nameof(_renderPso));
//        }

//        private static class DensityPsoConstants
//        {
//            public const int Viewport = 0;
//            public const int Systems = 1;
//            public const int Max = 2;
//            public const int Density = 3;

//            public const int Count = 4;
//        }

//        [MemberNotNull(nameof(_densityPso))]
//        private void CreateDensityPso()
//        {
//            var @params = new RootParameter[DensityPsoConstants.Count];

//            @params[DensityPsoConstants.Viewport] = RootParameter.CreateConstants<ShaderViewport>(0, 0);
//            @params[DensityPsoConstants.Systems] = RootParameter.CreateDescriptor(RootParameterType.ShaderResourceView, 0, 0);
//            @params[DensityPsoConstants.Max] = RootParameter.CreateDescriptor(RootParameterType.UnorderedAccessView, 0, 0);
//            @params[DensityPsoConstants.Viewport] = RootParameter.CreateDescriptorTable(DescriptorRangeType.ShaderResourceView, 0, 1, 0);

//            var flags = Array.Empty<ShaderCompileFlag>();

//            var densityPso = new ComputePipelineDesc
//            {
//                RootSignature = _device.CreateRootSignature(@params),
//                ComputeShader = ShaderManager.CompileShader("Shaders/Density.hlsl", ShaderType.Compute, flags),
//            };


//            _densityPso = _device.PipelineManager.CreatePipelineStateObject(densityPso, nameof(_densityPso));
//        }

//        [MemberNotNull(nameof(_renderPso))]
//        private void CreateRenderPso()
//        {
//            var @params = new RootParameter[]
//            {
//                RootParameter.CreateDescriptorTable(DescriptorRangeType.ShaderResourceView, 0, 1, 0, visibility: ShaderVisibility.Pixel)
//            };

//            var flags = Array.Empty<ShaderCompileFlag>();

//            var renderPso = new GraphicsPipelineDesc
//            {
//                RootSignature = _device.CreateRootSignature(@params),
//                DepthStencil = DepthStencilDesc.DisableDepthStencil,
//                VertexShader = ShaderManager.CompileShader("Shaders/Fullscreen.hlsl", ShaderType.Vertex, flags),
//                PixelShader = ShaderManager.CompileShader("Shaders/Render.hlsl", ShaderType.Pixel, flags),
//            };


//            _renderPso = _device.PipelineManager.CreatePipelineStateObject(renderPso, nameof(_renderPso));
//        }

//        private Buffer _rawData;
//        private Texture _renderTarget;
//        private DescriptorHandle _renderTargetView;

//        public struct ShaderViewport
//        {
//            private Vector2 TopLeft;
//            private Vector2 BottomRight;
//        }

//        public void Render(in ShaderViewport viewport)
//        {
//            var context = _device.BeginGraphicsContext(_renderPso);

//            context.SetAndClearRenderTarget(_renderTargetView, Rgba128.Black);
//            context.SetViewports(viewport);
//            context.SetScissorRectangles(_renderTarget.Resolution);

//            context.SetPipelineState(_renderPso);
//            context.SetShaderResourceBuffer(0, _rawData);
//            context.SetTopology(Topology.TriangleList);


//            context.Draw(3);

//            _device.Execute(context).Block();
//        }

//        private Buffer _systems, _max;
//        private Texture _density;

//        public void ComputeDensity(ComputeContext context, in ShaderViewport viewport)
//        {
//            var view = _device.CreateUnorderedAccessView(_density);

//            context.SetRoot32BitConstants(DensityPsoConstants.Viewport, viewport);
//            context.SetShaderResourceBuffer(DensityPsoConstants.Systems, _systems);
//            context.SetUnorderedAccessBuffer(DensityPsoConstants.Max, _max);
//            context.SetRootDescriptorTable(DensityPsoConstants.Density, view);

//            const int DispatchSize = ushort.MaxValue;
//            for (var i = 0; i < _systems.Length; i++)
//            {
//                context.Dispatch(DispatchSize, 1, 1);
//         }
//        }
//    }
//}
