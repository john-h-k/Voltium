using System;
using System.Diagnostics;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.D3D12;

namespace Voltium.Core.Devices
{
    /// <summary>
    /// Represents an opaque type used to synchronize execution of the GPU and CPU
    /// </summary>
    public unsafe struct GpuDispatchSynchronizer : IDisposable
    {
        private ComPtr<ID3D12Fence> _fence;
        private readonly FenceMarker _completionValue;

        /// <summary>
        /// Creates a new instance of <see cref="GpuDispatchSynchronizer"/>
        /// </summary>
        public GpuDispatchSynchronizer(ComPtr<ID3D12Fence> fence, FenceMarker completionValue)
        {
            _fence = fence;
            _completionValue = completionValue;
        }

        /// <summary>
        /// Blocks the calling thread until the synchronization event has occured
        /// </summary>
        public void Block()
        {
            if (!_fence.Exists)
            {
                return;
            }

            Guard.ThrowIfFailed(_fence.Get()->SetEventOnCompletion(_completionValue.FenceValue, default));
        }

        /// <summary>
        /// Returns a <see cref="GpuSyncEvent"/> used for synchronization
        /// </summary>
        /// <returns>A <see cref="GpuSyncEvent"/></returns>
        public GpuSyncEvent GetEvent()
        {
            var @event = Windows.CreateEventExW(null, null, 0, Windows.EVENT_ALL_ACCESS);
            Debug.Assert(_fence.Exists);
            Guard.ThrowIfFailed(_fence.Get()->SetEventOnCompletion(_completionValue.FenceValue, @event));
            return new GpuSyncEvent(@event);
        }

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose() => _fence.Dispose();
    }

    /// <summary>
    /// Represents an event used for GPU synchronization
    /// </summary>
    public readonly struct GpuSyncEvent
    {
        /// <summary>
        /// The underlying event handle
        /// </summary>
        public readonly IntPtr EventHandle;

        /// <summary>
        /// Creates a new instance of <see cref="GpuSyncEvent"/>
        /// </summary>
        public GpuSyncEvent(IntPtr eventHandle) => EventHandle = eventHandle;
    }
}
