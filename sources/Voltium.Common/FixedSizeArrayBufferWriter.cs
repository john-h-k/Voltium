using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Common
{
    internal sealed class FixedSizeArrayBufferWriter<T> : IBufferWriter<T>
    {
        private T[] _buffer;
        private int _size;
        private int _head;

        public FixedSizeArrayBufferWriter(int size, ArrayPool<T>? pool = null)
        {
            if (size == 0)
            {
                size = 1; // to fit the IBufferWriter contract that GetMemory/GetSpan never return empty
            }

            _size = size;
            _buffer = pool?.Rent(size) ?? new T[size];
        }

        public void Advance(int count)
        {
            if (_head + count > _size)
            {
                ThrowHelper.ThrowInvalidOperationException("Cannot advance past the end of a FixedSizeArrayBufferWriter");
            }
            _head += count;
        }

        public void ResetBuffer() => _head = 0;

        public Memory<T> GetMemory(int sizeHint = 0) => _buffer.AsMemory(_head);

        public Span<T> GetSpan(int sizeHint = 0) => _buffer.AsSpan(_head);

        public ReadOnlyMemory<T> GetWrittenMemory() => _buffer.AsMemory(0, _head);
        public ReadOnlySpan<T> GetWrittenSpan() => _buffer.AsSpan(0, _head);

        public bool IsEmpty => _head == _size;

        public int Capacity => _size;
    }
}
