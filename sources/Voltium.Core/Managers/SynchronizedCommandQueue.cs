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
        public readonly ulong Frequency;

        public ID3D12CommandQueue* GetQueue() => _queue.Get();

        public SynchronizedCommandQueue(
            GraphicsDevice device,
            ExecutionContext context
        )
        {
            Debug.Assert(device is object);

            _type = context;

            _queue = CreateQueue(device, context);
            _fence = CreateFence(device);
            
            DirectXHelpers.SetObjectName(_queue.Get(), GetListTypeName(context) + " Queue");
            DirectXHelpers.SetObjectName(_fence.Get(), GetListTypeName(context) + " Fence");

            _marker = new FenceMarker(device.BackBufferCount);
            _executingAllocators = new();
            Guard.ThrowIfFailed(_queue.Get()->Signal(_fence.Get(), _marker.FenceValue));
            _allocatorPool = new(ComPtr<ID3D12Device>.CopyFromPointer(device.DevicePointer), context);

            ulong frequency;
            int hr = _queue.Get()->GetTimestampFrequency(&frequency);
            Frequency = Windows.SUCCEEDED(hr) ? frequency : 0;
        }

        public bool TryQueryTimestamps(ulong* gpu, ulong* cpu)
        {
            return Windows.SUCCEEDED(_queue.Get()->GetClockCalibration(gpu, cpu));
        }

        private static unsafe ComPtr<ID3D12CommandQueue> CreateQueue(GraphicsDevice device, ExecutionContext type)
        {
            var desc = new D3D12_COMMAND_QUEUE_DESC
            {
                Type = (D3D12_COMMAND_LIST_TYPE)type,
                Flags = D3D12_COMMAND_QUEUE_FLAGS.D3D12_COMMAND_QUEUE_FLAG_NONE,
                NodeMask = 0, // TODO: MULTI-GPU
                Priority = (int)D3D12_COMMAND_QUEUE_PRIORITY.D3D12_COMMAND_QUEUE_PRIORITY_NORMAL // why are you like this D3D12
            };

            ComPtr<ID3D12CommandQueue> p = default;

            Guard.ThrowIfFailed(device.DevicePointer->CreateCommandQueue(
                &desc,
                p.Guid,
                ComPtr.GetVoidAddressOf(&p)
            ));

            return p.Move();
        }

        private static unsafe ComPtr<ID3D12Fence> CreateFence(GraphicsDevice device)
        {
            ComPtr<ID3D12Fence> fence = default;

            Guard.ThrowIfFailed(device.DevicePointer->CreateFence(
                0,
                0,
                fence.Guid,
                (void**)&fence
            ));

            return fence;
        }

        private static string GetListTypeName(ExecutionContext type) => type switch
        {
            ExecutionContext.Graphics => "Graphics",
            ExecutionContext.Compute => "Compute",
            ExecutionContext.Copy => "Copy",
            _ => "Unknown"
        };

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

        public FenceMarker GetFenceForNextExecution() => _marker + 1;

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

            // we still increment the fence to keep it in sync with the other queues
            if (!lists.IsEmpty)
            {
                _queue.Get()->ExecuteCommandLists((uint)lists.Length, (ID3D12CommandList**)pLists);
            }

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

        internal void InsertNextFence()
        {
            _marker++;
            Guard.ThrowIfFailed(_queue.Get()->Signal(_fence.Get(), _marker.FenceValue));
        }

        internal GpuDispatchSynchronizer GetSynchronizer(FenceMarker fenceMarker)
        {
            return new GpuDispatchSynchronizer(_fence.Copy(), fenceMarker);
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
