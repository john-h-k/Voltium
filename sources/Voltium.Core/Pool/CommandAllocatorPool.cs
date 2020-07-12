using System.Diagnostics;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;
using Voltium.Core.Pool;
using ZLogger;

namespace Voltium.Core.Devices
{
    /// <summary>
    /// A pool of <see cref="ID3D12CommandAllocator"/>s
    /// </summary>
    internal unsafe sealed class CommandAllocatorPool : ThreadSafeComPool<ID3D12CommandAllocator>
    {
        private ComputeDevice _device;
        private ExecutionContext _type;

        public CommandAllocatorPool(ComputeDevice device, ExecutionContext type)
        {;
            _device = device;
            _type = type;
        }

        private int _allocatorCount = 0;
        protected override ComPtr<ID3D12CommandAllocator> Create()
        {
            using ComPtr<ID3D12CommandAllocator> allocator = default;
            Guard.ThrowIfFailed(_device.DevicePointer->CreateCommandAllocator(
                (D3D12_COMMAND_LIST_TYPE)_type,
                allocator.Iid,
                ComPtr.GetVoidAddressOf(&allocator)
            ));

            LogHelper.Logger.ZLogDebug($"New command allocator allocated (this is the #{_allocatorCount++} allocator)");

            DebugHelpers.SetName(allocator.Get(), $"Pooled allocator #{_allocatorCount}");

            return allocator.Move();
        }

        protected sealed override void InternalDispose()
        {
            base.InternalDispose();
            _device.Dispose();
        }

        protected override void ManageRent(ref ComPtr<ID3D12CommandAllocator> value)
        {
        }

        protected override void ManageReturn(ref ComPtr<ID3D12CommandAllocator> state)
        {
            Guard.ThrowIfFailed(state.Get()->Reset());
        }
    }
}
