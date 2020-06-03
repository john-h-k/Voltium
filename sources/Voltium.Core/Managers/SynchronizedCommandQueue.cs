using System;
using System.Collections.Generic;
using System.Diagnostics;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.D3D12;
using Voltium.Core.Pool;

namespace Voltium.Core.Managers
{
    internal unsafe struct SynchronizedCommandQueue : IDisposable
    {
        private struct ExecutingAllocator : IDisposable
        {
            public ComPtr<ID3D12CommandAllocator> Allocator;
            public FenceMarker Marker;

            public ExecutingAllocator Move()
            {
                var copy = this;
                copy.Allocator = Allocator.Move();
                return copy;
            }

            public ExecutingAllocator Copy()
            {
                var copy = this;
                copy.Allocator = Allocator.Copy();
                return copy;
            }

            public void Dispose()
            {
                Allocator.Dispose();
            }
        }

        private ComPtr<ID3D12CommandQueue> _queue;
        private ComPtr<ID3D12Fence> _fence;
        private ExecutionContext _type;
        private FenceMarker _marker;
        private Queue<ExecutingAllocator> _executingAllocators;
        private CommandAllocatorPool _allocatorPool;

        public ID3D12CommandQueue* GetQueue() => _queue.Get();

        public SynchronizedCommandQueue(
            ComPtr<ID3D12Device> device,
            ExecutionContext context,
            ComPtr<ID3D12CommandQueue> queue,
            ComPtr<ID3D12Fence> fence
        )
        {
            Debug.Assert(queue.Exists);
            Debug.Assert(fence.Exists);
            Debug.Assert(device.Exists);

            _type = context;
            _queue = queue;
            _fence = fence;
            _marker = new FenceMarker(DeviceManager.BackBufferCount);
            _executingAllocators = new();
            Guard.ThrowIfFailed(_queue.Get()->Signal(_fence.Get(), _marker.FenceValue));
            _allocatorPool = new(device.Copy(), context);
        }

        public ComPtr<ID3D12CommandAllocator> RentAllocator()
        {
            //return _allocatorPool.ForceCreate();

            // if the pool has nothing left and we have in flight allocators
            if (_allocatorPool.IsEmpty && _executingAllocators.Count != 0)
            {
                var currentReachedFence = GetReachedFence();

                // try and give back any allocators we have
                while (_executingAllocators.Count != 0 && currentReachedFence >= _executingAllocators.Peek().Marker)
                {
                    _allocatorPool.Return(_executingAllocators.Dequeue().Allocator.Move());
                }
            }

            return _allocatorPool.Rent().Move();
        }

        public void Execute(
            ReadOnlySpan<GpuCommandSet> lists,
            out FenceMarker completion
        )
        {
            // TODO make the lists blittable to elide this copy
            ComPtr<ID3D12GraphicsCommandList>* pLists = stackalloc ComPtr<ID3D12GraphicsCommandList>[lists.Length];

            for (var i = 0; i < lists.Length; i++)
            {
                pLists[i] = lists[i].List.Move();
                _executingAllocators.Enqueue(new ExecutingAllocator { Allocator = lists[i].Allocator.Move(), Marker = _marker + 1 });
            }

            if (lists.IsEmpty)
            {
                completion = GetReachedFence();
                return;
            }

            _queue.Get()->ExecuteCommandLists((uint)lists.Length, (ID3D12CommandList**)pLists);

            InsertNextFence();
            completion = _marker;
        }

        internal FenceMarker GetReachedFence()
        {
            return new FenceMarker(_fence.Get()->GetCompletedValue());
        }

        internal FenceMarker GetNextFence() => _marker;

        internal GpuDispatchSynchronizer GetSynchronizerForIdle()
        {
            InsertNextFence();
            return GetSynchronizer(_marker);
        }

        private void InsertNextFence()
        {
            _marker++;
            Guard.ThrowIfFailed(_queue.Get()->Signal(_fence.Get(), _marker.FenceValue));
        }

        internal GpuDispatchSynchronizer GetSynchronizer(FenceMarker fenceMarker)
        {
            return new GpuDispatchSynchronizer(_fence.Copy(), fenceMarker);
        }

        internal void Signal(ID3D12Fence* fence, FenceMarker marker)
        {
            _queue.Get()->Signal(fence, marker.FenceValue);
        }

        public void Dispose()
        {
            _queue.Dispose();
            _fence.Dispose();
            _allocatorPool.Dispose();
        }

        internal bool IsIdle()
        {
            return GetReachedFence() >= _marker;
        }
    }
}
