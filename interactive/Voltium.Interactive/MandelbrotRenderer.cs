#define DOUBLE

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

        [StructLayout(LayoutKind.Sequential)]
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
                copy.UploadBuffer(device.Allocator, GetColors(), out _colors);
                _constants.ColorCount = _colors.Length / (uint)sizeof(RgbaColor);
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
                RenderTargetFormats = new FormatBuffer8(_device.BackBuffer.Format),
                VertexShader = ShaderManager.CompileShader("Shaders/Mandelbrot/EntireScreenCopyVS.hlsl", DxcCompileTarget.Vs_6_0, flags),
                PixelShader = ShaderManager.CompileShader("Shaders/Mandelbrot/Mandelbrot.hlsl", DxcCompileTarget.Ps_6_0, flags)
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

        private RgbaColor[] GetColors()
        {
            const int iters = 256 * 4 * 10;
            var colors = new RgbaColor[iters];

            int blockSize = iters / _rgbaColors.Length;
            for (int i = 0; i < colors.Length; i += _rgbaColors.Length)
            {
                _rgbaColors.CopyTo(colors.AsSpan(i));
            }

            return colors;
        }

        public override void Resize(Size newScreenData)
        {
            _outputResolution = newScreenData;
            //uint width = 16384, height = 16384;
            uint width = (uint)newScreenData.Width, height = (uint)newScreenData.Height;

            _renderTarget.Dispose();

            var target = TextureDesc.CreateRenderTargetDesc(DataFormat.R8G8B8A8UnsignedNormalized, height, width, RgbaColor.Black);

            _renderTarget = _device.Allocator.AllocateTexture(target, ResourceState.RenderTarget);
            _renderTargetView = _device.CreateRenderTargetView(_renderTarget);

            _constants.AspectRatio = newScreenData.Width / (float)newScreenData.Height;
        }

        private GraphicsPso _pso = null!;

        public override PipelineStateObject? GetInitialPso()
            => _pso;
 
        private byte[] _chunk = ArrayPool<byte>.Shared.Rent(1024 * 1024 * 32 * 8);
        private Buffer _output;
        public override void Render(ref GraphicsContext recorder)
        {
            recorder.ResourceTransition(_renderTarget, ResourceState.RenderTarget);

            recorder.Discard(_renderTarget);

            recorder.SetViewportAndScissor(_outputResolution);
            recorder.SetRenderTargets(_renderTargetView);
            recorder.SetTopology(Topology.TriangeList);
            recorder.SetBuffer(0, _colors);
            recorder.SetRoot32BitConstants(1, _constants);
            recorder.Draw(3);

            recorder.CopyResource(_renderTarget, _device.BackBuffer);
            recorder.ReadbackSubresource(_device.Allocator ,_renderTarget, 0, out _output);

            recorder.ResourceTransition(_device.BackBuffer, ResourceState.Present);
        }

        private GifWriter _writer = new GifWriter("output.gif", 16, 0);
        public override void Update(ApplicationTimer timer)
        {
            if (_output.Data.IsEmpty)
            {
                return;
            }

            _constants.Scale *= (FP)(1 - timer.ElapsedSeconds);

            var footprint = _device.GetSubresourceFootprint(_renderTarget, 0);
            _device.ReadbackIntermediateBuffer(footprint, _output, _chunk);

            RgbaToArgb(_chunk);

            fixed (void* pData = _chunk)
            {
                var image = new Bitmap(_outputResolution.Width, _outputResolution.Height, sizeof(RgbaColor), PixelFormat.Format32bppArgb, (IntPtr)pData);
                _writer.WriteFrame(image);
            }
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
