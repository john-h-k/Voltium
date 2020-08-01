using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using static TerraFX.Interop.Windows;

namespace Voltium.Core.Devices
{
    internal unsafe struct SynchronizedCommandQueue : IDisposable, IInternalD3D12Object
    {
        private ComPtr<ID3D12CommandQueue> _queue;
        private ComPtr<ID3D12Fence> _fence;
        private ulong _lastFence;

        private ExecutionContext _type;
        public readonly ulong Frequency;

        public ID3D12CommandQueue* GetQueue() => _queue.Get();

        private static ulong StartingFenceForContext(ExecutionContext context) => context switch
        {
            // we do this to prevent conflicts when comparing markers
            ExecutionContext.Copy => (ulong.MaxValue / 4) * 0,
            ExecutionContext.Compute => (ulong.MaxValue / 4) * 1,
            ExecutionContext.Graphics => (ulong.MaxValue / 4) * 2,
            _ => 0xFFFFFFFFFFFFFFFF
        };

        public SynchronizedCommandQueue(
            ComputeDevice device,
            ExecutionContext context
        )
        {
            Debug.Assert(device is object);

            _type = context;

            _queue = device.CreateQueue(context);
            _fence = device.CreateFence();

            DebugHelpers.SetName(_queue.Get(), GetListTypeName(context) + " Queue");
            DebugHelpers.SetName(_fence.Get(), GetListTypeName(context) + " Fence");

            _lastFence = StartingFenceForContext(context);
            Guard.ThrowIfFailed(_queue.Get()->Signal(_fence.Get(), _lastFence));

            ulong frequency;
            int hr = _queue.Get()->GetTimestampFrequency(&frequency);

            // E_FAIL is returned when the queue doesn't support timestamps
            if (hr != E_FAIL)
            {
                Frequency = hr == E_FAIL ? 0 : frequency;
            }
            else
            {
                Frequency = 0;
                Guard.ThrowIfFailed(hr, "_queue.Get()->GetTimestampFrequency(&frequency)");
            }
        }

        public GpuTask ExecuteCommandLists(uint numLists, ID3D12CommandList** ppLists)
        {
            _queue.Get()->ExecuteCommandLists(numLists, ppLists);
            return Signal();
        }

        public bool TryQueryTimestamps(ulong* gpu, ulong* cpu) => SUCCEEDED(_queue.Get()->GetClockCalibration(gpu, cpu));

        private static string GetListTypeName(ExecutionContext type) => type switch
        {
            ExecutionContext.Graphics => nameof(ExecutionContext.Graphics),
            ExecutionContext.Compute => nameof(ExecutionContext.Compute),
            ExecutionContext.Copy => nameof(ExecutionContext.Copy),
            _ => "Unknown"
        };

        internal GpuTask GetSynchronizerForIdle() => Signal();

        internal void Wait(in GpuTask waitable)
        {
            waitable.GetFenceAndMarker(out var fence, out var marker);
            Guard.ThrowIfFailed(_queue.Get()->Wait(fence, marker));
        }

        internal GpuTask Signal()
        {
            Guard.ThrowIfFailed(_queue.Get()->Signal(_fence.Get(), Interlocked.Increment(ref _lastFence)));
            return new GpuTask(_fence, _lastFence);
        }

        public void Dispose()
        {
            _queue.Dispose();
            _fence.Dispose();
        }

        ID3D12Object* IInternalD3D12Object.GetPointer() => (ID3D12Object*)_queue.Get();
    }
}
