using System;
using System.Collections.Generic;
using System.Diagnostics;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.D3D12;

namespace Voltium.Core.Managers
{
    internal unsafe struct SynchronizedCommandQueue : IDisposable
    {
        private ComPtr<ID3D12CommandQueue> _queue;
        private ComPtr<ID3D12Fence> _fence;
        private FenceMarker _marker;

        public ID3D12CommandQueue* GetQueue() => _queue.Get();

        public SynchronizedCommandQueue(
            ExecutionContext context,
            ComPtr<ID3D12CommandQueue> queue,
            ComPtr<ID3D12Fence> fence
        )
        {
            Debug.Assert(queue.Exists);
            Debug.Assert(fence.Exists);

            _queue = queue;
            _fence = fence;
            _marker = new FenceMarker(FenceMarker.GetFirstFenceForExecutionContext(context), context);
            _queue.Get()->Signal(_fence.Get(), _marker.FenceValue);
        }

        public void Execute(
            ReadOnlySpan<GpuCommandSet> lists,
            bool insertFence
        )
        {
            // TODO make the lists blittable to elide this copy
            ComPtr<ID3D12GraphicsCommandList>* pLists = stackalloc ComPtr<ID3D12GraphicsCommandList>[lists.Length];

            for (var i = 0; i < lists.Length; i++)
            {
                pLists[i] = lists[i].List.Move();
            }

            if (lists.IsEmpty)
            {
                return;
            }

            _queue.Get()->ExecuteCommandLists((uint)lists.Length, (ID3D12CommandList**)pLists);

            if (insertFence)
            {
                InsertNextFence();
            }
        }

        internal FenceMarker GetReachedFence()
        {
            return new FenceMarker(_fence.Get()->GetCompletedValue());
        }

        internal FenceMarker GetFenceForIdle() => _marker;

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
        }

        internal bool IsIdle()
        {
            return GetReachedFence().IsAtOrAfter(_marker);
        }
    }
}
