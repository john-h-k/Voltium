#define DOUBLE

using System;
using System.Buffers;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Voltium.Core;
using Voltium.Core.GpuResources;
using Voltium.Core.Managers;
using Voltium.Core.Managers.Shaders;
using Voltium.Core.Memory.GpuResources;
using Voltium.Core.Pipeline;
using static Voltium.Core.Pipeline.GraphicsPipelineDesc;
using Buffer = Voltium.Core.Memory.GpuResources.Buffer;

#if DOUBLE
using FP = System.Double;
#else
using FP = System.Single;
#endif

namespace Voltium.Interactive
{
    public sealed unsafe class MandelbrotRenderer : Renderer
    {
        private Texture _renderTarget;
        private DescriptorHandle _renderTargetView;
        private Buffer _colors;
        private GraphicsDevice _device = null!;

        private Size _outputResolution;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct MandelbrotConstants
        {
            public FP Scale;
            public FP CenterX;
            public FP CenterY;
            public float AspectRatio;
            public uint ColorCount;
        }

        private MandelbrotConstants _constants;

        private uint SsaaFactor { get; set; } = 8;

        public override void Init(GraphicsDevice device, GraphicalConfiguration config, in Size screen)
        {
            _device = device;

            //var colors = ;

            using (var copy = device.BeginCopyContext())
            {
                copy.UploadBuffer(device.Allocator, GetColors(), ResourceState.PixelShaderResource, out _colors);
            }

            var @params = new RootParameter[]
            {
                RootParameter.CreateDescriptor(RootParameterType.ShaderResourceView, 0, 0, ShaderVisibility.Pixel),
                RootParameter.CreateConstants((uint)sizeof(MandelbrotConstants) / sizeof(uint), 0, 0, ShaderVisibility.Pixel),
            };

            var rootSig = RootSignature.Create(device, @params, null);
            PipelineManager.Reset();

            var flags = new DxcCompileFlags.Flag[]
            {
#if DOUBLE
                DxcCompileFlags.DefineMacro("DOUBLE")
#endif
            };

            var psoDesc = new GraphicsPipelineDesc
            {
                RootSignature = rootSig,
                Topology = TopologyClass.Triangle,
                DepthStencil = DepthStencilDesc.DisableDepthStencil,
                RenderTargetFormats = new FormatBuffer8(_renderTarget.Format),
                VertexShader = ShaderManager.CompileShader("Shaders/Mandelbrot/EntireScreenCopyVS.hlsl", DxcCompileTarget.Vs_5_0, flags),
                PixelShader = ShaderManager.CompileShader("Shaders/Mandelbrot/Mandelbrot.hlsl", DxcCompileTarget.Ps_5_0, flags)
            };

            _pso = PipelineManager.CreatePso(device, "Mandelbrot", psoDesc);

            _constants = new MandelbrotConstants
            {
                Scale = (FP)1,
                CenterX = (FP)(-1.789169018604823106674468341188838763),
                CenterY = (FP)(0.00000033936851576718256602823026614)
            };

            Resize(screen);
        }

        private Rgba128[] GetColors()
        {
            const int iters = 256 * 4 * 10;
            var colors = new Rgba128[iters];

            int blockSize = iters / _rgba128s.Length;
            for (int i = 0; i < colors.Length; i += _rgba128s.Length)
            {
                _rgba128s.CopyTo(colors.AsSpan(i));
            }

            return colors;
        }

        public override void Resize(Size newScreenData)
        {
            _outputResolution = newScreenData;
            const int width = 16384 / 4, height = 16384 / 4;
            _outputResolution = new Size(width, height);
            //uint width = (uint)newScreenData.Width, height = (uint)newScreenData.Height;
            _image = new Bitmap(_outputResolution.Width, _outputResolution.Height);
            _renderTarget.Dispose();
            _output.Dispose();

            _output = _device.Allocator.AllocateBuffer(width * height * 10, MemoryAccess.CpuReadback, ResourceState.CopyDestination);
            var target = TextureDesc.CreateRenderTargetDesc(DataFormat.R8G8B8A8UnsignedNormalized, height, width, Rgba128.Black);

            _renderTarget = _device.Allocator.AllocateTexture(target, ResourceState.RenderTarget);
            _renderTargetView = _device.CreateRenderTargetView(_renderTarget);

            _constants.AspectRatio = newScreenData.Width / (float)newScreenData.Height;
            _constants.ColorCount = 256 * 4 * 10;
        }

        private GraphicsPso _pso = null!;

        public override PipelineStateObject? GetInitialPso()
            => _pso;

        private Buffer _output;
        public override void Render(ref GraphicsContext recorder, out Texture render)
        {
            recorder.ResourceTransition(_renderTarget, ResourceState.RenderTarget);

            recorder.SetViewportAndScissor(_outputResolution);
            recorder.SetRenderTargets(_renderTargetView);
            recorder.SetTopology(Topology.TriangeList);
            recorder.SetBuffer(0, _colors);
            recorder.SetRoot32BitConstants(1, _constants);
            recorder.Draw(3);

            //recorder.CopyResource(_renderTarget, _device.BackBuffer);
            recorder.ReadbackSubresource(_device.Allocator, _renderTarget, 0, _output);

            render = _renderTarget;

            //recorder.ResourceTransition(_device.BackBuffer, ResourceState.Present);
        }

        private GifWriter _writer = new GifWriter("output.gif", 16, 0);
        private Bitmap _image = null!;
        private int _frameIndex = 0;

        private Task _save = Task.CompletedTask;
        public override void Update(ApplicationTimer timer)
        {
            _frameIndex++;
            if (_output.Data.IsEmpty)
            {
                Console.WriteLine("bad");
                return;
            }

            _constants.Scale *= (FP)(0.9); // (FP)(1 - timer.ElapsedSeconds);

            var footprint = _device.GetSubresourceFootprint(_renderTarget, 0);

            _save.Wait();

            var bits = _image.LockBits(new System.Drawing.Rectangle(0, 0, _outputResolution.Width, _outputResolution.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            var bitSpan = new Span<byte>((byte*)bits.Scan0, bits.Height * bits.Stride);
            _device.ReadbackIntermediateBuffer(footprint, _output, bitSpan);
            _image.UnlockBits(bits);

            _save = Task.Run(() => _image.Save($"imgs/img{_frameIndex:D5}.bmp", ImageFormat.Bmp));

            //_writer.WriteFrame(_image);
        }

        private void RgbaToArgb(Span<byte> data)
        {
            for (var i = 0; i < data.Length; i += 4)
            {
                var rgba = BitOperations.RotateRight(MemoryMarshal.Read<uint>(data), 8);
                MemoryMarshal.Write(data, ref rgba);

                data = data.Slice(4);
            }
        }

        public override void Dispose()
        {
            _writer.Dispose();
            _renderTarget.Dispose();
            _renderTarget.Dispose();
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

        private Rgba128[] _rgba128s = new Rgba128[]
        {
            Rgba128.FromPacked(0x00, 0x00, 0x00, 0xFF),
            Rgba128.FromPacked(0x58, 0x10, 0x50, 0xFF),
            Rgba128.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
            Rgba128.FromPacked(0x40, 0x24, 0x64, 0xFF),
            Rgba128.FromPacked(0x34, 0x30, 0x70, 0xFF),
            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            Rgba128.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            Rgba128.FromPacked(0x1c, 0x44, 0x84, 0xFF),
            Rgba128.FromPacked(0x28, 0x38, 0x78, 0xFF),
            Rgba128.FromPacked(0x38, 0x2c, 0x6c, 0xFF),
            Rgba128.FromPacked(0x44, 0x20, 0x60, 0xFF),
            Rgba128.FromPacked(0x54, 0x14, 0x54, 0xFF),
            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
            Rgba128.FromPacked(0x70, 0x0c, 0x40, 0xFF),
            Rgba128.FromPacked(0x90, 0x14, 0x34, 0xFF),
            Rgba128.FromPacked(0xac, 0x1c, 0x24, 0xFF),
            Rgba128.FromPacked(0xcc, 0x24, 0x18, 0xFF),
            Rgba128.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
            Rgba128.FromPacked(0xec, 0x58, 0x08, 0xFF),
            Rgba128.FromPacked(0xf4, 0x84, 0x04, 0xFF),
            Rgba128.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
            Rgba128.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
            Rgba128.FromPacked(0xf0, 0x68, 0x08, 0xFF),
            Rgba128.FromPacked(0xec, 0x40, 0x08, 0xFF),
            Rgba128.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
            Rgba128.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
            Rgba128.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
            Rgba128.FromPacked(0x80, 0x0c, 0x38, 0xFF),
            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
            Rgba128.FromPacked(0x58, 0x10, 0x50, 0xFF),
            Rgba128.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
            Rgba128.FromPacked(0x40, 0x24, 0x64, 0xFF),
            Rgba128.FromPacked(0x34, 0x30, 0x70, 0xFF),
            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            Rgba128.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            Rgba128.FromPacked(0x30, 0x34, 0x74, 0xFF),
            Rgba128.FromPacked(0x3c, 0x28, 0x68, 0xFF),
            Rgba128.FromPacked(0x48, 0x1c, 0x5c, 0xFF),
            Rgba128.FromPacked(0x54, 0x14, 0x54, 0xFF),
            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
            Rgba128.FromPacked(0x70, 0x0c, 0x40, 0xFF),
            Rgba128.FromPacked(0x90, 0x14, 0x34, 0xFF),
            Rgba128.FromPacked(0xac, 0x1c, 0x24, 0xFF),
            Rgba128.FromPacked(0xcc, 0x24, 0x18, 0xFF),
            Rgba128.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
            Rgba128.FromPacked(0xec, 0x58, 0x08, 0xFF),
            Rgba128.FromPacked(0xf4, 0x84, 0x04, 0xFF),
            Rgba128.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
            Rgba128.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
            Rgba128.FromPacked(0xf0, 0x68, 0x08, 0xFF),
            Rgba128.FromPacked(0xec, 0x40, 0x08, 0xFF),
            Rgba128.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
            Rgba128.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
            Rgba128.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
            Rgba128.FromPacked(0x80, 0x0c, 0x38, 0xFF),
            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
            Rgba128.FromPacked(0x58, 0x10, 0x50, 0xFF),
            Rgba128.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
            Rgba128.FromPacked(0x40, 0x24, 0x64, 0xFF),
            Rgba128.FromPacked(0x34, 0x30, 0x70, 0xFF),
            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            Rgba128.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            Rgba128.FromPacked(0x30, 0x34, 0x74, 0xFF),
            Rgba128.FromPacked(0x3c, 0x28, 0x68, 0xFF),
            Rgba128.FromPacked(0x48, 0x1c, 0x5c, 0xFF),
            Rgba128.FromPacked(0x54, 0x14, 0x54, 0xFF),
            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
            Rgba128.FromPacked(0x70, 0x0c, 0x40, 0xFF),
            Rgba128.FromPacked(0x90, 0x14, 0x34, 0xFF),
            Rgba128.FromPacked(0xac, 0x1c, 0x24, 0xFF),
            Rgba128.FromPacked(0xcc, 0x24, 0x18, 0xFF),
            Rgba128.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
            Rgba128.FromPacked(0xec, 0x58, 0x08, 0xFF),
            Rgba128.FromPacked(0xf4, 0x84, 0x04, 0xFF),
            Rgba128.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
            Rgba128.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
            Rgba128.FromPacked(0xf0, 0x68, 0x08, 0xFF),
            Rgba128.FromPacked(0xec, 0x40, 0x08, 0xFF),
            Rgba128.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
            Rgba128.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
            Rgba128.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
            Rgba128.FromPacked(0x80, 0x0c, 0x38, 0xFF),
            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
            Rgba128.FromPacked(0x58, 0x10, 0x50, 0xFF),
            Rgba128.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
            Rgba128.FromPacked(0x40, 0x24, 0x64, 0xFF),
            Rgba128.FromPacked(0x34, 0x30, 0x70, 0xFF),
            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            Rgba128.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            Rgba128.FromPacked(0x30, 0x34, 0x74, 0xFF),
            Rgba128.FromPacked(0x3c, 0x28, 0x68, 0xFF),
            Rgba128.FromPacked(0x48, 0x1c, 0x5c, 0xFF),
            Rgba128.FromPacked(0x54, 0x14, 0x54, 0xFF),
            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
            Rgba128.FromPacked(0x70, 0x0c, 0x40, 0xFF),
            Rgba128.FromPacked(0x90, 0x14, 0x34, 0xFF),
            Rgba128.FromPacked(0xac, 0x1c, 0x24, 0xFF),
            Rgba128.FromPacked(0xcc, 0x24, 0x18, 0xFF),
            Rgba128.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
            Rgba128.FromPacked(0xec, 0x58, 0x08, 0xFF),
            Rgba128.FromPacked(0xf4, 0x84, 0x04, 0xFF),
            Rgba128.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
            Rgba128.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
            Rgba128.FromPacked(0xf0, 0x68, 0x08, 0xFF),
            Rgba128.FromPacked(0xec, 0x40, 0x08, 0xFF),
            Rgba128.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
            Rgba128.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
            Rgba128.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
            Rgba128.FromPacked(0x80, 0x0c, 0x38, 0xFF),
            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
            Rgba128.FromPacked(0x58, 0x10, 0x50, 0xFF),
            Rgba128.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
            Rgba128.FromPacked(0x40, 0x24, 0x64, 0xFF),
            Rgba128.FromPacked(0x34, 0x30, 0x70, 0xFF),
            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            Rgba128.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            Rgba128.FromPacked(0x30, 0x34, 0x74, 0xFF),
            Rgba128.FromPacked(0x3c, 0x28, 0x68, 0xFF),
            Rgba128.FromPacked(0x48, 0x1c, 0x5c, 0xFF),
            Rgba128.FromPacked(0x54, 0x14, 0x54, 0xFF),
            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
            Rgba128.FromPacked(0x70, 0x0c, 0x40, 0xFF),
            Rgba128.FromPacked(0x90, 0x14, 0x34, 0xFF),
            Rgba128.FromPacked(0xac, 0x1c, 0x24, 0xFF),
            Rgba128.FromPacked(0xcc, 0x24, 0x18, 0xFF),
            Rgba128.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
            Rgba128.FromPacked(0xec, 0x58, 0x08, 0xFF),
            Rgba128.FromPacked(0xf4, 0x84, 0x04, 0xFF),
            Rgba128.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
            Rgba128.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
            Rgba128.FromPacked(0xf0, 0x68, 0x08, 0xFF),
            Rgba128.FromPacked(0xec, 0x40, 0x08, 0xFF),
            Rgba128.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
            Rgba128.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
            Rgba128.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
            Rgba128.FromPacked(0x80, 0x0c, 0x38, 0xFF),
            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
            Rgba128.FromPacked(0x58, 0x10, 0x50, 0xFF),
            Rgba128.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
            Rgba128.FromPacked(0x40, 0x24, 0x64, 0xFF),
            Rgba128.FromPacked(0x34, 0x30, 0x70, 0xFF),
            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            Rgba128.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            Rgba128.FromPacked(0x30, 0x34, 0x74, 0xFF),
            Rgba128.FromPacked(0x3c, 0x28, 0x68, 0xFF),
            Rgba128.FromPacked(0x48, 0x1c, 0x5c, 0xFF),
            Rgba128.FromPacked(0x54, 0x14, 0x54, 0xFF),
            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
            Rgba128.FromPacked(0x70, 0x0c, 0x40, 0xFF),
            Rgba128.FromPacked(0x90, 0x14, 0x34, 0xFF),
            Rgba128.FromPacked(0xac, 0x1c, 0x24, 0xFF),
            Rgba128.FromPacked(0xcc, 0x24, 0x18, 0xFF),
            Rgba128.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
            Rgba128.FromPacked(0xec, 0x58, 0x08, 0xFF),
            Rgba128.FromPacked(0xf4, 0x84, 0x04, 0xFF),
            Rgba128.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
            Rgba128.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
            Rgba128.FromPacked(0xf0, 0x68, 0x08, 0xFF),
            Rgba128.FromPacked(0xec, 0x40, 0x08, 0xFF),
            Rgba128.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
            Rgba128.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
            Rgba128.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
            Rgba128.FromPacked(0x80, 0x0c, 0x38, 0xFF),
            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
            Rgba128.FromPacked(0x58, 0x10, 0x50, 0xFF),
            Rgba128.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
            Rgba128.FromPacked(0x40, 0x24, 0x64, 0xFF),
            Rgba128.FromPacked(0x34, 0x30, 0x70, 0xFF),
            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            Rgba128.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            Rgba128.FromPacked(0x30, 0x34, 0x74, 0xFF),
            Rgba128.FromPacked(0x3c, 0x28, 0x68, 0xFF),
            Rgba128.FromPacked(0x48, 0x1c, 0x5c, 0xFF),
            Rgba128.FromPacked(0x54, 0x14, 0x54, 0xFF),
            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
            Rgba128.FromPacked(0x70, 0x0c, 0x40, 0xFF),
            Rgba128.FromPacked(0x90, 0x14, 0x34, 0xFF),
            Rgba128.FromPacked(0xac, 0x1c, 0x24, 0xFF),
            Rgba128.FromPacked(0xcc, 0x24, 0x18, 0xFF),
            Rgba128.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
            Rgba128.FromPacked(0xec, 0x58, 0x08, 0xFF),
            Rgba128.FromPacked(0xf4, 0x84, 0x04, 0xFF),
            Rgba128.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
            Rgba128.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
            Rgba128.FromPacked(0xf0, 0x68, 0x08, 0xFF),
            Rgba128.FromPacked(0xec, 0x40, 0x08, 0xFF),
            Rgba128.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
            Rgba128.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
            Rgba128.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
            Rgba128.FromPacked(0x80, 0x0c, 0x38, 0xFF),
            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
            Rgba128.FromPacked(0x58, 0x10, 0x50, 0xFF),
            Rgba128.FromPacked(0x4c, 0x1c, 0x5c, 0xFF),
            Rgba128.FromPacked(0x40, 0x24, 0x64, 0xFF),
            Rgba128.FromPacked(0x34, 0x30, 0x70, 0xFF),
            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            Rgba128.FromPacked(0x00, 0x5c, 0x9c, 0xFF),
            Rgba128.FromPacked(0x0c, 0x50, 0x90, 0xFF),
            Rgba128.FromPacked(0x18, 0x48, 0x88, 0xFF),
            Rgba128.FromPacked(0x24, 0x3c, 0x7c, 0xFF),
            Rgba128.FromPacked(0x30, 0x34, 0x74, 0xFF),
            Rgba128.FromPacked(0x3c, 0x28, 0x68, 0xFF),
            Rgba128.FromPacked(0x48, 0x1c, 0x5c, 0xFF),
            Rgba128.FromPacked(0x54, 0x14, 0x54, 0xFF),
            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF),
            Rgba128.FromPacked(0x70, 0x0c, 0x40, 0xFF),
            Rgba128.FromPacked(0x90, 0x14, 0x34, 0xFF),
            Rgba128.FromPacked(0xac, 0x1c, 0x24, 0xFF),
            Rgba128.FromPacked(0xcc, 0x24, 0x18, 0xFF),
            Rgba128.FromPacked(0xe8, 0x2c, 0x08, 0xFF),
            Rgba128.FromPacked(0xec, 0x58, 0x08, 0xFF),
            Rgba128.FromPacked(0xf4, 0x84, 0x04, 0xFF),
            Rgba128.FromPacked(0xfc, 0xb0, 0x00, 0xFF),
            Rgba128.FromPacked(0xf8, 0x8c, 0x04, 0xFF),
            Rgba128.FromPacked(0xf0, 0x68, 0x08, 0xFF),
            Rgba128.FromPacked(0xec, 0x40, 0x08, 0xFF),
            Rgba128.FromPacked(0xe4, 0x1c, 0x0c, 0xFF),
            Rgba128.FromPacked(0xc4, 0x18, 0x1c, 0xFF),
            Rgba128.FromPacked(0xa4, 0x14, 0x2c, 0xFF),
            Rgba128.FromPacked(0x80, 0x0c, 0x38, 0xFF),
            Rgba128.FromPacked(0x60, 0x08, 0x48, 0xFF)
        };
    }
}
