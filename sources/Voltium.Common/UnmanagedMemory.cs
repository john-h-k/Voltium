using System;
using System.Buffers;

namespace Voltium.Common
{
    internal sealed unsafe class UnmanagedMemory<T> : MemoryManager<T> where T : unmanaged
    {
        private T* _ptr;
        private int _length;
        private Action? _dispose;

        public UnmanagedMemory(T* ptr, int length, Action? dispose)
        {
            _ptr = ptr;
            _length = length;
            _dispose = dispose;
        }

        public override Span<T> GetSpan() => new Span<T>(_ptr, _length);

        public override MemoryHandle Pin(int elementIndex = 0) => new MemoryHandle(_ptr);

        public override void Unpin() { }

        protected override void Dispose(bool disposing) => _dispose?.Invoke();
    }
}
