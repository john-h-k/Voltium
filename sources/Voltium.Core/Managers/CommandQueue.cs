using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Common.Threading;
using Voltium.Core.Contexts;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.Pool;
using static TerraFX.Interop.Windows;

namespace Voltium.Core.Devices
{
    

    public class Heap
    {

    }

    public unsafe abstract class CommandQueue : IDisposable
    {
        public ulong? QueueFrequency { get; }
        public ulong CpuFrequency { get; }
        public ExecutionContext Context { get; }

        protected Fence Fence;

        public CommandQueue(ExecutionContext context, Fence fence)
        {
            Context = context;
            CpuFrequency = GetCpuFrequency();
            QueueFrequency = GetGpuFrequency();
            Fence = fence;
        }

        protected abstract ulong? GetGpuFrequency();
        protected abstract ulong GetCpuFrequency();

        public void QueryTimestamps(out TimeSpan gpu, out TimeSpan cpu)
        {
            ulong gpuTick, cpuTick;

            QueryTimestamps(&gpuTick, &cpuTick);

            gpu = TimeSpan.FromSeconds(gpuTick / (double)QueueFrequency!.Value);
            cpu = TimeSpan.FromSeconds(cpuTick / (double)CpuFrequency);
        }

        public void QueryTimestamps(out ulong gpu, out ulong cpu)
        {
            fixed (ulong* pGpu = &gpu)
            fixed (ulong* pCpu = &cpu)
            {
                QueryTimestamps(pGpu, pCpu);
            }
        }

        [MemberNotNull(nameof(QueueFrequency))]
        public void QueryTimestamps(ulong* gpu, ulong* cpu)
        {
            if (QueueFrequency is null)
            {
                ThrowHelper.ThrowPlatformNotSupportedException($"Queue type '{Context}' does not support timestamps on this device");
            }

            InternalQueryTimestamps(gpu, cpu);
        }


        public abstract GpuTask Execute(ReadOnlySpan<GpuContext> contexts);

        protected abstract void InternalQueryTimestamps(ulong* gpu, ulong* cpu);

        public readonly struct WorkLock : IDisposable
        {
            private readonly CommandQueue _queue;

            internal WorkLock(CommandQueue queue)
            {
                queue.Lock();
                _queue = queue;
            }

            public void Release() => Dispose();
            public void Dispose()
            {
                _queue.Unlock();
            }
        }

        public void Idle(ref WorkLock blocker)
        {
            blocker = new(this);
            var idle = IdleTask;
            idle.Block();
        }

        protected abstract GpuTask IdleTask { get; }

        private readonly object _defaultLock = new();

        protected virtual void Lock() => Monitor.Enter(_defaultLock);
        protected virtual void Unlock() => Monitor.Exit(_defaultLock);

        public abstract void Wait(in GpuTask waitable);

        public abstract void Dispose();
    }

    internal unsafe class D3D12CommandQueue : CommandQueue
    {
        private readonly ComputeDevice _device;
        private UniqueComPtr<ID3D12CommandQueue> _queue;

        [ThreadStatic]
        private UniqueComPtr<ID3D12CommandList> _list;

        private LockedQueue<(UniqueComPtr<ID3D12CommandAllocator> Allocator, GpuTask Task), SpinLockWrapped> _allocators = new(new(true));

        internal ID3D12CommandQueue* GetQueue() => _queue.Ptr;

        public D3D12CommandQueue(ComputeDevice device, ExecutionContext context) : base(context)
        {
            Debug.Assert(device is object);

            _device = device;
            _queue = device.CreateQueue(context, 0);
        }

        public GpuTask ExecuteCommandLists(ReadOnlySpan<GpuContext> contexts)
        {
            static bool IsAllocatorFinished(ref (UniqueComPtr<ID3D12CommandAllocator> Allocator, GpuTask Task) allocator) => allocator.Task.IsCompleted;

            if (_allocators.TryDequeue(out var allocator, &IsAllocatorFinished))
            {
                _device.ThrowIfFailed(allocator.Allocator.Ptr->Reset());
            }
            else
            {
                allocator = (_device.CreateAllocator(Context), GpuTask.Completed);
            }


            var list = _list;

            if (!list.Exists)
            {
                list = _list = _device.CreateList(Context, allocator.Allocator.Ptr);
            }
            else
            {
                list = _list;

                _device.ThrowIfFailed(Context switch
                {
                    ExecutionContext.Copy or ExecutionContext.Compute or ExecutionContext.Graphics => list.As<ID3D12GraphicsCommandList>().Ptr->Reset(allocator.Allocator.Ptr, null),
                    ExecutionContext.VideoDecode => list.As<ID3D12VideoDecodeCommandList>().Ptr->Reset(allocator.Allocator.Ptr),
                    ExecutionContext.VideoEncode => list.As<ID3D12VideoEncodeCommandList>().Ptr->Reset(allocator.Allocator.Ptr),
                    ExecutionContext.VideoProcess => list.As<ID3D12VideoProcessCommandList>().Ptr->Reset(allocator.Allocator.Ptr),
                    _ => E_INVALIDARG,
                }
                );
            }

            foreach (var context in contexts)
            {
                _encoder.Encode(context, list);
            }

            _queue.Ptr->ExecuteCommandLists(1, (ID3D12CommandList**)&list);
            _device.ThrowIfDeviceRemoved();
            return Signal();
        }

        protected override void InternalQueryTimestamps(ulong* gpu, ulong* cpu)
        {
            if (FAILED(_queue.Ptr->GetClockCalibration(gpu, cpu)))
            {
                ThrowHelper.ThrowPlatformNotSupportedException("Copy-queue timestamps not supported");
            }
        }

        private static string GetListTypeName(ExecutionContext type) => type switch
        {
            ExecutionContext.Graphics => nameof(ExecutionContext.Graphics),
            ExecutionContext.Compute => nameof(ExecutionContext.Compute),
            ExecutionContext.Copy => nameof(ExecutionContext.Copy),
            _ => "Unknown"
        };

        protected override GpuTask IdleTask => Signal();

        public override void Wait(in GpuTask waitable)
        {
            if (waitable.IsCompleted)
            {   
                return;
            }

            waitable.GetFenceAndMarker(out var fence, out var marker);
            _device.ThrowIfFailed(_queue.Ptr->Wait(fence, marker));
        }

        private GpuTask Signal()
        {
            _fence.SetValue(Interlocked.Increment(ref _lastFence));
            return new GpuTask(_fence, _lastFence);
        }

        public override void Dispose()
        {
            _queue.Dispose();
            _fence.Dispose();
        }

        protected override ulong? GetGpuFrequency()
        {
            if (Context != ExecutionContext.Copy
                || _device.QueryFeatureSupport<D3D12_FEATURE_DATA_D3D12_OPTIONS3>(D3D12_FEATURE.D3D12_FEATURE_D3D12_OPTIONS3).CopyQueueTimestampQueriesSupported != FALSE)
            {
                ulong frequency = 0;
                _device.ThrowIfFailed(_queue.Ptr->GetTimestampFrequency(&frequency));
                return frequency;
            }
            return null;
        }

        protected override ulong GetCpuFrequency()
        {
            LARGE_INTEGER cpuFrequency = default;
            QueryPerformanceFrequency(&cpuFrequency);
            return Helpers.LargeIntegerToUInt64(cpuFrequency);
        }

        public override GpuTask Execute(ReadOnlySpan<GpuContext> contexts) => throw new NotImplementedException();
    }
}
