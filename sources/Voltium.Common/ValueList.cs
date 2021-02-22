using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Voltium.Common
{
    internal struct ValueList<T>
    {
        private ArrayPool<T>? _pool;
        private T[] _items;
        private int _length;

        public Span<T> AsSpan() => _items.AsSpan(0, _length);

        public ValueList(int capacity, ArrayPool<T>? pool = null)
        {
            Guard.Positive(capacity);
            _items = new T[capacity];
            _length = 0;
            _pool = pool;
        }

        public void AddRange(int count)
        {
            ResizeIfNecessary(_length += count);
        }

        public void AddRange(ReadOnlySpan<T> values)
        {
            var length = _length;
            ResizeIfNecessary(_length += values.Length);
            values.CopyTo(_items.AsSpan(length));
        }

        public ref T GetReference() => ref MemoryMarshal.GetArrayDataReference(_items);
        public ref T GetPinnableReference() => ref (_length == 0 ? ref Unsafe.NullRef<T>() : ref MemoryMarshal.GetArrayDataReference(_items));

        public void Add()
        {
            ResizeIfNecessary(_length + 1);
            _length++;
        }

        public void Add(in T item)
        {
            ResizeIfNecessary(_length + 1);
            _items[_length++] = item;
        }

        public void Add(T item)
        {
            ResizeIfNecessary(_length + 1);
            _items[_length++] = item;
        }

        private void ResizeIfNecessary(int size)
        {
            if (size <= _items.Length)
            {
                return;
            }

            Resize(size);
        }

        private void Resize(int size)
        {
            var old = _items;
            _items = null!;

            if (_pool is null)
            {
                _items = new T[size];
            }
            else
            {
                _items = _pool.Rent(size);
            }

            old.AsSpan(0, _length).CopyTo(_items);

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                old.AsSpan().Clear();
            }
            
            if (_pool is not null)
            {
                _pool.Return(old);
            }
        }

        public T this[int index]
        {
            get => RefIndex(index);
            set => RefIndex(index) = value;
        }

        public ref T RefIndex(int index)
        {
            Guard.InRangeExclusive(index, -1, _length);
            return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_items), index);
        }

        public int Length => _length;
    }
}
