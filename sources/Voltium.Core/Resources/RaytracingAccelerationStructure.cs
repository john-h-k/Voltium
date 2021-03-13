using System;
using Voltium.Core.NativeApi;

namespace Voltium.Core.Memory
{
    /// <summary>
    /// Represents an opaque acceleration structure used for raytracing
    /// </summary>
    public unsafe struct RaytracingAccelerationStructure : IDisposable
    {
        internal RaytracingAccelerationStructure(ulong length, RaytracingAccelerationStructureHandle handle, Disposal<RaytracingAccelerationStructureHandle> disposal)
        {
            Length = length;
            Handle = handle;
            _dispose = disposal;
        }

        /// <summary>
        /// The size, in bytes, of the buffer
        /// </summary>
        public readonly ulong Length;

        internal RaytracingAccelerationStructureHandle Handle;
        private Disposal<RaytracingAccelerationStructureHandle> _dispose;

        /// <inheritdoc/>
        public void Dispose() => _dispose.Dispose(ref Handle);
    }

}
