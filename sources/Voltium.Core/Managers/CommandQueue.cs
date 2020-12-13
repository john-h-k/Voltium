using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using static TerraFX.Interop.Windows;

namespace Voltium.Core.Devices
{
    internal unsafe class CommandQueue : IDisposable, IInternalD3D12Object
    {
        private readonly ComputeDevice _device;
        private UniqueComPtr<ID3D12CommandQueue> _queue;
        private UniqueComPtr<ID3D12Fence> _fence;
        private ulong _lastFence;

        public readonly ExecutionContext Type;
        public readonly ulong Frequency;

        public ID3D12CommandQueue* GetQueue() => _queue.Ptr;

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
            Debug.Assert(device is object);

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

        public GpuTask ExecuteCommandLists(ReadOnlySpan<UniqueComPtr<ID3D12CommandList>> lists)
        {
            fixed (void* ppLists = lists)
            {
                _queue.Ptr->ExecuteCommandLists((uint)lists.Length, (ID3D12CommandList**)ppLists);
            }
            return Signal();
        }

        public bool TryQueryTimestamps(out ulong gpu, out ulong cpu)
        {
            fixed (ulong* pGpu = &gpu)
            fixed (ulong* pCpu = &cpu)
            {
                return TryQueryTimestamps(pGpu, pCpu);
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

    internal static unsafe class QueueExtensions
    {
        public static void Wait(this ref ID3D12CommandQueue queue, in GpuTask waitable)
        {
            waitable.GetFenceAndMarker(out var fence, out var marker);
            Guard.ThrowIfFailed(queue.Wait(fence, marker));
        }
    }
}
