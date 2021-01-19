using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using static TerraFX.Interop.Windows;
using Voltium.Common;
using Voltium.Core.Memory;
using Voltium.Core.Devices;
using System.Threading;


namespace Voltium.Core.Managers
{
    internal unsafe class CommandQueue : IDisposable, IInternalD3D12Object
    {
        private UniqueComPtr<ID3D12CommandQueue> _queue;
        private UniqueComPtr<ID3D12Fence> _fence;

        internal ID3D12CommandQueue* GetQueue() => _queue.Ptr;

        private static ulong StartingFenceForContext(ExecutionContext context) => 0; // context switch
        //{
        //    // we do this to prevent conflicts when comparing markers
        //    ExecutionContext.Copy => ulong.MaxValue / 4 * 0,
        //    ExecutionContext.Compute => ulong.MaxValue / 4 * 1,
        //    ExecutionContext.Graphics => ulong.MaxValue / 4 * 2,
        //    _ => 0xFFFFFFFFFFFFFFFF
        //};

        public CommandQueue(
            ComputeDevice device,
            ExecutionContext context,
            bool enableTdr
        )
        {
            SysDebug.Assert(device is object);

            Type = context;

            _device = device;
            _queue = device.CreateQueue(context, enableTdr ? D3D12_COMMAND_QUEUE_FLAGS.D3D12_COMMAND_QUEUE_FLAG_NONE : D3D12_COMMAND_QUEUE_FLAGS.D3D12_COMMAND_QUEUE_FLAG_DISABLE_GPU_TIMEOUT);
            _fence = device.CreateFence(StartingFenceForContext(context));
            _lastFence = _fence.Ptr->GetCompletedValue();

            var name = GetListTypeName(context);
            this.SetName(name + " Queue");

            DebugHelpers.SetName(_fence.Ptr, name + " Fence");

            ulong frequency;
            int hr = _queue.Ptr->GetTimestampFrequency(&frequency);

            // E_FAIL is returned when the queue doesn't support timestamps
            if (SUCCEEDED(hr) || hr == E_FAIL)
            {
                Frequency = hr == E_FAIL ? 0 : frequency;
            }
            else
            {
                Frequency = 0;
                _device.ThrowIfFailed(hr, "_queue.Ptr->GetTimestampFrequency(&frequency)");
            }
        }

        public partial GpuTask ExecuteCommandLists(ReadOnlySpan<ContextParams> lists, ReadOnlySpan<GpuTask> dependencies)
        {
            using RentedArray<UniqueComPtr<ID3D12CommandList>> rentedArray = default;
            Span<UniqueComPtr<ID3D12CommandList>> buff = default;
            if (StackSentinel.SafeToStackallocPointers(lists.Length))
            {
                var ptr = stackalloc UniqueComPtr<ID3D12CommandList>[lists.Length];
                buff = new(ptr, lists.Length);
            }
            else
            {
                // fuck safety
                Unsafe.AsRef(in rentedArray) = RentedArray<UniqueComPtr<ID3D12CommandList>>.Create(lists.Length);
                buff = rentedArray.AsSpan();
            }

            int i = 0;
            foreach (ref readonly var list in lists)
            {
                buff[i++] = list.List;
            }

            foreach (ref readonly var dependency in dependencies)
            {
                Wait(dependency);
            }

            fixed (UniqueComPtr<ID3D12CommandList>* pLists = buff)
            {
            }
        }

        public bool TryQueryTimestamps(ulong* gpu, ulong* cpu) => SUCCEEDED(_queue.Ptr->GetClockCalibration(gpu, cpu));

        private static string GetListTypeName(ExecutionContext type) => type switch
        {
            ExecutionContext.Graphics => nameof(ExecutionContext.Graphics),
            ExecutionContext.Compute => nameof(ExecutionContext.Compute),
            ExecutionContext.Copy => nameof(ExecutionContext.Copy),
            _ => "Unknown"
        };

        internal GpuTask GetSynchronizerForIdle() => Signal();
        internal void Idle() => GetSynchronizerForIdle().Block();

        public void Wait(in GpuTask waitable)
        {
            if (waitable.IsCompleted)
            {
                return;
            }

            waitable.GetFenceAndMarker(out var fence, out var marker);
            _device.ThrowIfFailed(_queue.Ptr->Wait(fence, marker));
        }

        public GpuTask Signal()
        {
            _device.ThrowIfFailed(_queue.Ptr->Signal(_fence.Ptr, Interlocked.Increment(ref _lastFence)));
            return new GpuTask(_device, _fence, _lastFence);
        }

        public void Dispose()
        {
            _queue.Dispose();
            _fence.Dispose();
        }

        ID3D12Object* IInternalD3D12Object.GetPointer() => (ID3D12Object*)_queue.Ptr;
    }
}
