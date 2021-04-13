using System;
using Voltium.Core.NativeApi;

namespace Voltium.Core.Memory
{
    /// <summary>
    /// Represents an opaque acceleration structure used for raytracing
    /// </summary>
    public unsafe struct RaytracingAccelerationStructure : IDisposable
    {
        internal RaytracingAccelerationStructure(ulong length, ulong deviceAddress, RaytracingAccelerationStructureHandle handle, Disposal<RaytracingAccelerationStructureHandle> disposal)
        {
            Length = length;
            DeviceAddress = deviceAddress;
            Handle = handle;
            _dispose = disposal;
        }

        /// <summary>
        /// The size, in bytes, of the buffer
        /// </summary>
        public readonly ulong Length;

        public readonly ulong DeviceAddress;

        internal RaytracingAccelerationStructureHandle Handle;
        private Disposal<RaytracingAccelerationStructureHandle> _dispose;

        /// <inheritdoc/>
        public void Dispose() => _dispose.Dispose(ref Handle);
    }

}
