using System;
using System.Diagnostics;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;
using Voltium.Core.Pool;


namespace Voltium.Core.Devices
{
    /// <summary>
    /// A pool of <see cref="ID3D12CommandAllocator"/>s
    /// </summary>
    internal unsafe sealed class CommandAllocatorPool : ThreadSafeComPool<ID3D12CommandAllocator, ExecutionContext>
    {
        private ComputeDevice _device;

        public CommandAllocatorPool(ComputeDevice device)
        {;
            _device = device;
        }

        private int _allocatorCount = 0;
        protected override ComPtr<ID3D12CommandAllocator> Create(ExecutionContext context)
        {
            using ComPtr<ID3D12CommandAllocator> allocator = default;
            _device.ThrowIfFailed(_device.DevicePointer->CreateCommandAllocator(
                (D3D12_COMMAND_LIST_TYPE)context,
                allocator.Iid,
                (void**)&allocator
            ));

            LogHelper.LogDebug($"New command allocator allocated (this is the #{_allocatorCount++} allocator)");

            DebugHelpers.SetName(allocator.Ptr, $"Pooled allocator #{_allocatorCount}");

            return allocator.Move();
        }

        protected sealed override void InternalDispose()
        {
            base.InternalDispose();
            _device.Dispose();
        }

        protected override void ManageRent(ref ComPtr<ID3D12CommandAllocator> value, ExecutionContext context)
        {
        }

        protected override void ManageReturn(ref ComPtr<ID3D12CommandAllocator> state)
            => _device.ThrowIfFailed(state.Ptr->Reset());
    }
}
