using System;
using System.Drawing;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Memory;
using Voltium.Core.Configuration.Graphics;

namespace Voltium.Core.Devices
{
    internal sealed class Win7Output : Output
    {
        private UniqueComPtr<ID3D12GraphicsCommandList> _presentContext;
        private UniqueComPtr<ID3D12CommandAllocator> _allocator;
        private UniqueComPtr<ID3D12CommandQueueDownlevel> _queue;
        private IntPtr _hwnd;
        private D3D12_DOWNLEVEL_PRESENT_FLAGS _presentFlags;

        public unsafe Win7Output(GraphicsDevice device, in DXGI_SWAP_CHAIN_DESC1 swapChainDesc, IntPtr hwnd, OutputConfiguration desc) : base(device, desc)
        {
            _allocator = device.CreateAllocator(ExecutionContext.Graphics);
            _presentContext = device.CreateList(ExecutionContext.Graphics, _allocator.Ptr, null).As<ID3D12GraphicsCommandList>();

            _presentFlags = desc.SyncInterval > 0 ? D3D12_DOWNLEVEL_PRESENT_FLAGS.D3D12_DOWNLEVEL_PRESENT_FLAG_WAIT_FOR_VBLANK : 0;
            _hwnd = hwnd;

            if (!ComPtr.TryQueryInterface(device.GetGraphicsQueue(), out ID3D12CommandQueueDownlevel* pDownlevel))
            {
                ThrowHelper.ThrowPlatformNotSupportedException("Not on windows7");
            }

            _queue = new(pDownlevel);
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

            for (var i = 0U; i < _desc.BackBufferCount; i++)
            {
                // ID3D12CommandQueueDownLevel::Present requires a committed resource
                _backBuffers[i] = _device.Allocator.AllocateTexture(desc, ResourceState.RenderTarget, AllocFlags.ForceAllocateComitted);
            }
        }

        internal override unsafe void InternalPresent()
        {
            Guard.ThrowIfFailed(_presentContext.Ptr->Reset(_allocator.Ptr, null));

            ref var backBuffer = ref _backBuffers[_backBufferIndex];

            // don't need to 
            Guard.ThrowIfFailed(_queue.Ptr->Present(_presentContext.Ptr, backBuffer.GetResourcePointer(), _hwnd, _presentFlags));
        }
    }
}
