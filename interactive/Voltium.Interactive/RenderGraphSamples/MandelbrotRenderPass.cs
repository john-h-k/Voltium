//#define DOUBLE

//using System;
//using System.Buffers;
//using System.Drawing;
//using System.Drawing.Imaging;
//using System.Numerics;
//using System.Runtime.InteropServices;
//using System.Threading.Tasks;

//using Voltium.Core;
//using Voltium.Core.Memory;
//using Voltium.Core.Devices;
//using Voltium.Core.Devices.Shaders;
//using Voltium.Core.Pipeline;
//using Voltium.RenderEngine;
//using Buffer = Voltium.Core.Memory.Buffer;

//using static Voltium.Core.Pipeline.GraphicsPipelineDesc;
//using TerraFX.Interop;
//using Voltium.Core.Contexts;
//using Voltium.Common;

//#if DOUBLE
//using FloatType = System.Double;
//#else
//using FloatType = System.Single;
//#endif

//namespace Voltium.Interactive.RenderGraphSamples
//{
//    internal unsafe class MandelbrotRenderPass : Application
//    {
//        private GraphicsDevice _device = null!;
//        private Output _output = null!;
//        private PipelineStateObject _pso = null!;
//        private MandelbrotConstants _constants;
//        private Buffer _colors;
//        private const int IterCount = 1024;

//        public override void Initialize(Size outputSize, IOutputOwner output)
//        {
//#if DEBUG
//            var debug = DebugLayerConfiguration.Debug.AddDebugFlags(DebugFlags.GpuBasedValidation);
//#else
//            var debug = DebugLayerConfiguration.None;
//#endif

//            _device = GraphicsDevice.Create(FeatureLevel.GraphicsLevel11_0, null, debug);
//            _output = Output.Create(OutputConfiguration.Default, _device, output);

//            using (var copy = _device.BeginUploadContext())
//            {
//                _colors = copy.UploadBuffer(GetColors());
//            }

//            var @params = new RootParameter[]
//            {
//                RootParameter.CreateDescriptor(RootParameterType.ShaderResourceView, 0, 0, ShaderVisibility.Pixel),
//                RootParameter.CreateConstants<MandelbrotConstants>(0, 0, ShaderVisibility.Pixel),
//            };

//            var rootSig = _device.CreateRootSignature(@params, null);

//            var flags = new ShaderCompileFlag[]
//            {
//                ShaderCompileFlag.EnableDebugInformation,
//                ShaderCompileFlag.WriteDebugInformationToFile(),
//                ShaderCompileFlag.DefineMacro("ITER", IterCount.ToString()),
//#if DOUBLE
//                ShaderCompileFlag.DefineMacro("DOUBLE")
//#endif
//            };

//            var psoDesc = new GraphicsPipelineDesc
//            {
//                RootSignature = rootSig,
//                Topology = TopologyClass.Triangle,
//                DepthStencil = DepthStencilDesc.DisableDepthStencil,
//                RenderTargetFormats = BackBufferFormat.R8G8B8A8UnsignedNormalized,
//                VertexShader = ShaderManager.CompileShader("Shaders/Mandelbrot/EntireScreenCopyVS.hlsl", ShaderType.Vertex, flags),
//                PixelShader = ShaderManager.CompileShader("Shaders/Mandelbrot/Mandelbrot.hlsl", ShaderType.Pixel, flags),
//                Rasterizer = RasterizerDesc.Default.WithFrontFaceType(FaceType.Anticlockwise)
//            };

//            _pso = _device.PipelineManager.CreatePipelineStateObject(psoDesc, "Mandelbrot");

//            _constants = new MandelbrotConstants
//            {
//                Scale = (FloatType)1,
//                CenterX = (FloatType)(-1.789169018604823106674468341188838763),
//                CenterY = (FloatType)(0.00000033936851576718256602823026614),
//                ColorCount = _colors.LengthAs<Rgba128>()
//            };

//            OnResize(outputSize);
//        }

//        public override void Update(ApplicationTimer timer)
//        {
//            _constants.Scale *= (FloatType)0.99;
//        }

//        public override void Render()
//        {
//            var context = _device.BeginGraphicsContext(_pso);

//            using (context.ScopedBarrier(ResourceTransition.Create(_output.OutputBuffer, ResourceState.Present, ResourceState.RenderTarget)))
//            {
//                context.SetViewportAndScissor(_output.Resolution);
//                context.SetRenderTarget(_output.OutputBufferView);
//                context.SetTopology(Topology.TriangleList);
//                context.SetShaderResourceBuffer(0, _colors);
//                context.SetRoot32BitConstants(1, _constants);
//                context.Draw(3);
//            }

//            context.Close();
//            _device.Execute(context).Block();
//            _output.Present();
//        }

//        private Rgba128[] GetColors()
//        {
//            return _rgba128s;

//            const int iters = 256 * 4 * 10;
//            var colors = new Rgba128[iters];

//            for (int i = 0; i < colors.Length; i += _rgba128s.Length)
//            {
//                _rgba128s.CopyTo(colors.AsSpan(i));
//            }

//            return colors;
//        }

//        public override void OnResize(Size newScreenData)
//        {
//            _output.Resize(newScreenData);
//            _constants.AspectRatio = newScreenData.Width / (float)newScreenData.Height;
//        }

//        public override void Dispose() { }

//        [StructLayout(LayoutKind.Sequential, Pack = 1)]
//        private struct MandelbrotConstants
//        {
//            public FloatType Scale;
//#if false && DOUBLE
//            private FloatType _pad;
//#endif
//            public FloatType CenterX;
//            public FloatType CenterY;
//            public float AspectRatio;
//            public uint ColorCount;
//        }


//        private Rgba128[] _rgba128s = new Rgba128[]
//        {
//            Rgba128.FromPacked(0x00, 0x00, 0x00, 0xFF),
//            Rgba128.FromPacked(0x58, 0x10, 0x50, 0xFF),
//            Rgba128.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
//            Rgba128.FromPacked(0x40, 0x24, 0x64, 0xFF),
//            Rgba128.FromPacked(0x34, 0x30, 0x70, 0xFF),
//            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
//            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
//            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
//            Rgba128.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
//            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
//            Rgba128.FromPacked(0x1c, 0x44, 0x84, 0xFF),
//            Rgba128.FromPacked(0x28, 0x38, 0x78, 0xFF),
//            Rgba128.FromPacked(0x38, 0x2c, 0x6c, 0xFF),
//            Rgba128.FromPacked(0x44, 0x20, 0x60, 0xFF),
//            Rgba128.FromPacked(0x54, 0x14, 0x54, 0xFF),
//            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
//            Rgba128.FromPacked(0x70, 0x0c, 0x40, 0xFF),
//            Rgba128.FromPacked(0x90, 0x14, 0x34, 0xFF),
//            Rgba128.FromPacked(0xac, 0x1c, 0x24, 0xFF),
//            Rgba128.FromPacked(0xcc, 0x24, 0x18, 0xFF),
//            Rgba128.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
//            Rgba128.FromPacked(0xec, 0x58, 0x08, 0xFF),
//            Rgba128.FromPacked(0xf4, 0x84, 0x04, 0xFF),
//            Rgba128.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
//            Rgba128.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
//            Rgba128.FromPacked(0xf0, 0x68, 0x08, 0xFF),
//            Rgba128.FromPacked(0xec, 0x40, 0x08, 0xFF),
//            Rgba128.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
//            Rgba128.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
//            Rgba128.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
//            Rgba128.FromPacked(0x80, 0x0c, 0x38, 0xFF),
//            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
//            Rgba128.FromPacked(0x58, 0x10, 0x50, 0xFF),
//            Rgba128.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
//            Rgba128.FromPacked(0x40, 0x24, 0x64, 0xFF),
//            Rgba128.FromPacked(0x34, 0x30, 0x70, 0xFF),
//            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
//            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
//            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
//            Rgba128.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
//            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
//            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
//            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
//            Rgba128.FromPacked(0x30, 0x34, 0x74, 0xFF),
//            Rgba128.FromPacked(0x3c, 0x28, 0x68, 0xFF),
//            Rgba128.FromPacked(0x48, 0x1c, 0x5c, 0xFF),
//            Rgba128.FromPacked(0x54, 0x14, 0x54, 0xFF),
//            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
//            Rgba128.FromPacked(0x70, 0x0c, 0x40, 0xFF),
//            Rgba128.FromPacked(0x90, 0x14, 0x34, 0xFF),
//            Rgba128.FromPacked(0xac, 0x1c, 0x24, 0xFF),
//            Rgba128.FromPacked(0xcc, 0x24, 0x18, 0xFF),
//            Rgba128.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
//            Rgba128.FromPacked(0xec, 0x58, 0x08, 0xFF),
//            Rgba128.FromPacked(0xf4, 0x84, 0x04, 0xFF),
//            Rgba128.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
//            Rgba128.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
//            Rgba128.FromPacked(0xf0, 0x68, 0x08, 0xFF),
//            Rgba128.FromPacked(0xec, 0x40, 0x08, 0xFF),
//            Rgba128.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
//            Rgba128.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
//            Rgba128.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
//            Rgba128.FromPacked(0x80, 0x0c, 0x38, 0xFF),
//            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
//            Rgba128.FromPacked(0x58, 0x10, 0x50, 0xFF),
//            Rgba128.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
//            Rgba128.FromPacked(0x40, 0x24, 0x64, 0xFF),
//            Rgba128.FromPacked(0x34, 0x30, 0x70, 0xFF),
//            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
//            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
//            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
//            Rgba128.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
//            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
//            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
//            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
//            Rgba128.FromPacked(0x30, 0x34, 0x74, 0xFF),
//            Rgba128.FromPacked(0x3c, 0x28, 0x68, 0xFF),
//            Rgba128.FromPacked(0x48, 0x1c, 0x5c, 0xFF),
//            Rgba128.FromPacked(0x54, 0x14, 0x54, 0xFF),
//            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
//            Rgba128.FromPacked(0x70, 0x0c, 0x40, 0xFF),
//            Rgba128.FromPacked(0x90, 0x14, 0x34, 0xFF),
//            Rgba128.FromPacked(0xac, 0x1c, 0x24, 0xFF),
//            Rgba128.FromPacked(0xcc, 0x24, 0x18, 0xFF),
//            Rgba128.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
//            Rgba128.FromPacked(0xec, 0x58, 0x08, 0xFF),
//            Rgba128.FromPacked(0xf4, 0x84, 0x04, 0xFF),
//            Rgba128.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
//            Rgba128.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
//            Rgba128.FromPacked(0xf0, 0x68, 0x08, 0xFF),
//            Rgba128.FromPacked(0xec, 0x40, 0x08, 0xFF),
//            Rgba128.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
//            Rgba128.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
//            Rgba128.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
//            Rgba128.FromPacked(0x80, 0x0c, 0x38, 0xFF),
//            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
//            Rgba128.FromPacked(0x58, 0x10, 0x50, 0xFF),
//            Rgba128.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
//            Rgba128.FromPacked(0x40, 0x24, 0x64, 0xFF),
//            Rgba128.FromPacked(0x34, 0x30, 0x70, 0xFF),
//            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
//            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
//            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
//            Rgba128.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
//            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
//            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
//            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
//            Rgba128.FromPacked(0x30, 0x34, 0x74, 0xFF),
//            Rgba128.FromPacked(0x3c, 0x28, 0x68, 0xFF),
//            Rgba128.FromPacked(0x48, 0x1c, 0x5c, 0xFF),
//            Rgba128.FromPacked(0x54, 0x14, 0x54, 0xFF),
//            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
//            Rgba128.FromPacked(0x70, 0x0c, 0x40, 0xFF),
//            Rgba128.FromPacked(0x90, 0x14, 0x34, 0xFF),
//            Rgba128.FromPacked(0xac, 0x1c, 0x24, 0xFF),
//            Rgba128.FromPacked(0xcc, 0x24, 0x18, 0xFF),
//            Rgba128.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
//            Rgba128.FromPacked(0xec, 0x58, 0x08, 0xFF),
//            Rgba128.FromPacked(0xf4, 0x84, 0x04, 0xFF),
//            Rgba128.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
//            Rgba128.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
//            Rgba128.FromPacked(0xf0, 0x68, 0x08, 0xFF),
//            Rgba128.FromPacked(0xec, 0x40, 0x08, 0xFF),
//            Rgba128.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
//            Rgba128.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
//            Rgba128.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
//            Rgba128.FromPacked(0x80, 0x0c, 0x38, 0xFF),
//            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
//            Rgba128.FromPacked(0x58, 0x10, 0x50, 0xFF),
//            Rgba128.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
//            Rgba128.FromPacked(0x40, 0x24, 0x64, 0xFF),
//            Rgba128.FromPacked(0x34, 0x30, 0x70, 0xFF),
//            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
//            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
//            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
//            Rgba128.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
//            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
//            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
//            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
//            Rgba128.FromPacked(0x30, 0x34, 0x74, 0xFF),
//            Rgba128.FromPacked(0x3c, 0x28, 0x68, 0xFF),
//            Rgba128.FromPacked(0x48, 0x1c, 0x5c, 0xFF),
//            Rgba128.FromPacked(0x54, 0x14, 0x54, 0xFF),
//            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
//            Rgba128.FromPacked(0x70, 0x0c, 0x40, 0xFF),
//            Rgba128.FromPacked(0x90, 0x14, 0x34, 0xFF),
//            Rgba128.FromPacked(0xac, 0x1c, 0x24, 0xFF),
//            Rgba128.FromPacked(0xcc, 0x24, 0x18, 0xFF),
//            Rgba128.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
//            Rgba128.FromPacked(0xec, 0x58, 0x08, 0xFF),
//            Rgba128.FromPacked(0xf4, 0x84, 0x04, 0xFF),
//            Rgba128.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
//            Rgba128.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
//            Rgba128.FromPacked(0xf0, 0x68, 0x08, 0xFF),
//            Rgba128.FromPacked(0xec, 0x40, 0x08, 0xFF),
//            Rgba128.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
//            Rgba128.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
//            Rgba128.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
//            Rgba128.FromPacked(0x80, 0x0c, 0x38, 0xFF),
//            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
//            Rgba128.FromPacked(0x58, 0x10, 0x50, 0xFF),
//            Rgba128.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
//            Rgba128.FromPacked(0x40, 0x24, 0x64, 0xFF),
//            Rgba128.FromPacked(0x34, 0x30, 0x70, 0xFF),
//            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
//            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
//            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
//            Rgba128.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
//            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
//            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
//            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
//            Rgba128.FromPacked(0x30, 0x34, 0x74, 0xFF),
//            Rgba128.FromPacked(0x3c, 0x28, 0x68, 0xFF),
//            Rgba128.FromPacked(0x48, 0x1c, 0x5c, 0xFF),
//            Rgba128.FromPacked(0x54, 0x14, 0x54, 0xFF),
//            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
//            Rgba128.FromPacked(0x70, 0x0c, 0x40, 0xFF),
//            Rgba128.FromPacked(0x90, 0x14, 0x34, 0xFF),
//            Rgba128.FromPacked(0xac, 0x1c, 0x24, 0xFF),
//            Rgba128.FromPacked(0xcc, 0x24, 0x18, 0xFF),
//            Rgba128.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
//            Rgba128.FromPacked(0xec, 0x58, 0x08, 0xFF),
//            Rgba128.FromPacked(0xf4, 0x84, 0x04, 0xFF),
//            Rgba128.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
//            Rgba128.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
//            Rgba128.FromPacked(0xf0, 0x68, 0x08, 0xFF),
//            Rgba128.FromPacked(0xec, 0x40, 0x08, 0xFF),
//            Rgba128.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
//            Rgba128.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
//            Rgba128.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
//            Rgba128.FromPacked(0x80, 0x0c, 0x38, 0xFF),
//            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
//            Rgba128.FromPacked(0x58, 0x10, 0x50, 0xFF),
//            Rgba128.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
//            Rgba128.FromPacked(0x40, 0x24, 0x64, 0xFF),
//            Rgba128.FromPacked(0x34, 0x30, 0x70, 0xFF),
//            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
//            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
//            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
//            Rgba128.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
//            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
//            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
//            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
//            Rgba128.FromPacked(0x30, 0x34, 0x74, 0xFF),
//            Rgba128.FromPacked(0x3c, 0x28, 0x68, 0xFF),
//            Rgba128.FromPacked(0x48, 0x1c, 0x5c, 0xFF),
//            Rgba128.FromPacked(0x54, 0x14, 0x54, 0xFF),
//            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
//            Rgba128.FromPacked(0x70, 0x0c, 0x40, 0xFF),
//            Rgba128.FromPacked(0x90, 0x14, 0x34, 0xFF),
//            Rgba128.FromPacked(0xac, 0x1c, 0x24, 0xFF),
//            Rgba128.FromPacked(0xcc, 0x24, 0x18, 0xFF),
//            Rgba128.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
//            Rgba128.FromPacked(0xec, 0x58, 0x08, 0xFF),
//            Rgba128.FromPacked(0xf4, 0x84, 0x04, 0xFF),
//            Rgba128.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
//            Rgba128.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
//            Rgba128.FromPacked(0xf0, 0x68, 0x08, 0xFF),
//            Rgba128.FromPacked(0xec, 0x40, 0x08, 0xFF),
//            Rgba128.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
//            Rgba128.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
//            Rgba128.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
//            Rgba128.FromPacked(0x80, 0x0c, 0x38, 0xFF),
//            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
//            Rgba128.FromPacked(0x58, 0x10, 0x50, 0xFF),
//            Rgba128.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
//            Rgba128.FromPacked(0x40, 0x24, 0x64, 0xFF),
//            Rgba128.FromPacked(0x34, 0x30, 0x70, 0xFF),
//            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
//            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
//            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
//            Rgba128.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
//            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
//            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
//            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
//            Rgba128.FromPacked(0x30, 0x34, 0x74, 0xFF),
//            Rgba128.FromPacked(0x3c, 0x28, 0x68, 0xFF),
//            Rgba128.FromPacked(0x48, 0x1c, 0x5c, 0xFF),
//            Rgba128.FromPacked(0x54, 0x14, 0x54, 0xFF),
//            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
//            Rgba128.FromPacked(0x70, 0x0c, 0x40, 0xFF),
//            Rgba128.FromPacked(0x90, 0x14, 0x34, 0xFF),
//            Rgba128.FromPacked(0xac, 0x1c, 0x24, 0xFF),
//            Rgba128.FromPacked(0xcc, 0x24, 0x18, 0xFF),
//            Rgba128.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
//            Rgba128.FromPacked(0xec, 0x58, 0x08, 0xFF),
//            Rgba128.FromPacked(0xf4, 0x84, 0x04, 0xFF),
//            Rgba128.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
//            Rgba128.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
//            Rgba128.FromPacked(0xf0, 0x68, 0x08, 0xFF),
//            Rgba128.FromPacked(0xec, 0x40, 0x08, 0xFF),
//            Rgba128.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
//            Rgba128.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
//            Rgba128.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
//            Rgba128.FromPacked(0x80, 0x0c, 0x38, 0xFF),
//            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF)
//        };
//    }
//}
