using System;
using Voltium.Core.NativeApi;

namespace Voltium.Core.Memory
{
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

        public RaytracingAccelerationStructureHandle Handle;
        private Disposal<RaytracingAccelerationStructureHandle> _dispose;

        public void Dispose() => _dispose.Dispose(ref Handle);
    }

}
