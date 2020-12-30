using System;
using System.Drawing;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Memory;
using System.Buffers;
using Voltium.Core.Configuration.Graphics;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core.Devices
{
    [Obsolete("TODO")]
    internal sealed partial class VideoOutput : Output
    {

        [FixedBufferType(typeof(Buffer), 16)]
        private partial struct ReadbackBufferBuffer16 { }

#pragma warning disable CS0414
        private IBufferWriter<byte>? _bufferWriter = null!;
#pragma warning restore CS0414
        private UniqueComPtr<ID3D12SharingContract> _bufferWriterContract;
        private IntPtr _maybeHwnd;
        private ReadbackBufferBuffer16 _readbackBuffers;

        internal VideoOutput(GraphicsDevice device, OutputConfiguration desc) : base(device, desc)
        {
            _ = !device.TryQueryInterface(out _bufferWriterContract);
            _maybeHwnd = default;
        }

        internal override void InternalResize(Size newSize)
        {
            DataFormat format = _backBuffers[0].Format;
            for (var i = 0U; i < _desc.BackBufferCount; i++)
            {
                _backBuffers[i].Dispose();
            }

            ResourceFlags flags = ResourceFlags.None;

            if (_desc.Flags.HasFlag(OutputFlags.AllowRenderTarget))
            {
                flags |= ResourceFlags.AllowRenderTarget;
            }

            var desc = new TextureDesc
            {
                Width = (uint)newSize.Width,
                Height = (uint)newSize.Height,
                DepthOrArraySize = 1,
                Format = format,
                MipCount = 1,
                Dimension = TextureDimension.Tex2D,
                ClearValue = default,
                Msaa = MsaaDesc.None,
                ResourceFlags = flags,
            };

            var readbackDesc = new BufferDesc
            {
                Length = (long)_device.GetRequiredSize(desc, 0),
                ResourceFlags = ResourceFlags.DenyShaderResource
            };

            for (var i = 0U; i < _desc.BackBufferCount; i++)
            {
                _backBuffers[i] = _device.Allocator.AllocateTexture(desc, ResourceState.RenderTarget);
                _readbackBuffers[i] = _device.Allocator.AllocateBuffer(readbackDesc, MemoryAccess.CpuUpload);
            }

            CreateViews();
        }

        internal override unsafe void InternalPresent(in GpuTask presentAfter)
        {
            var toCpuCopy = _device.BeginCopyContext(ContextFlags.ExecuteOnClose);

            toCpuCopy.Close();
            _device.Execute(toCpuCopy);


            _bufferWriterContract.Ptr->Present(OutputBuffer.GetResourcePointer(), 0, _maybeHwnd);
        }
    }
}
