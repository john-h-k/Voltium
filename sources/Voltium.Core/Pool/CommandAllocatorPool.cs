using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Common.Debugging;
using Voltium.Common.Threading;
using Voltium.Core.D3D12;
using Voltium.Core.Pool;
using static TerraFX.Interop.D3D12_COMMAND_LIST_TYPE;

namespace Voltium.Core.Managers
{
    /// <summary>
    /// A pool of <see cref="ID3D12CommandAllocator"/>s
    /// </summary>
    internal unsafe sealed class CommandAllocatorPool : ThreadSafeComPool<ID3D12CommandAllocator, ExecutionContext>
    {
        private ComPtr<ID3D12Device> _device;

        public CommandAllocatorPool(ComPtr<ID3D12Device> device)
        {
            Debug.Assert(device.Exists);
            _device = device.Move();
        }

        private int _allocatorCount = 0;
        protected override ComPtr<ID3D12CommandAllocator> Create(ExecutionContext state)
        {
            using ComPtr<ID3D12CommandAllocator> allocator = default;
            Guard.ThrowIfFailed(_device.Get()->CreateCommandAllocator(
                (D3D12_COMMAND_LIST_TYPE)state,
                allocator.Guid,
                ComPtr.GetVoidAddressOf(&allocator)
            ));

            DirectXHelpers.SetObjectName(allocator.Get(), $"Pooled allocator #{_allocatorCount++}");

            return allocator.Move();
        }

        protected sealed override void InternalDispose()
        {
            base.InternalDispose();
            _device.Dispose();
        }

        protected override void ManageRent(ref ComPtr<ID3D12CommandAllocator> value, ExecutionContext state)
        {
        }

        protected override void ManageReturn(ref ComPtr<ID3D12CommandAllocator> state)
        {
            state.Get()->Reset();
        }
    }
}
