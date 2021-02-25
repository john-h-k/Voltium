using System;
using Voltium.Core.Memory;

namespace Voltium.Core.Contexts
{
    public struct Heap : IDisposable
    {
        internal HeapHandle Handle;
        public uint Length;
        private Disposal<HeapHandle> _disposal;

        public void Dispose() => _disposal.Dispose(ref Handle);
    }
}
