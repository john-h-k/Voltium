using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Voltium.Common
{
    internal struct ValueLinkedList<T>
    {
        public ValueLinkedListNode<T> Add(in T value)
        {
            Tail.Next = new ValueLinkedListNode<T>(value, Tail, null);
            Tail = Tail.Next;
            return Tail;
        }

        public ValueLinkedListNode<T> Emplace(in T value)
        {
            Head.Previous = new ValueLinkedListNode<T>(value, null, Head);
            Head = Head.Previous;
            return Head;
        }

        public ValueLinkedListNode<T> Head { get; private set; }
        public ValueLinkedListNode<T> Tail { get; private set; }
    }

    internal sealed class ValueLinkedListNode<T>
    {
        internal ValueLinkedListNode(in T value, ValueLinkedListNode<T>? previous, ValueLinkedListNode<T>? next)
        {
            Value = value;
            Previous = previous;
            Next = next;
        }

        public ValueLinkedListNode<T>? Previous;
        public ValueLinkedListNode<T>? Next;

        [AllowNull]
        public T Value;
    }


    internal struct ValueList<T>
    {
        private ArrayPool<T>? _pool;
        private T[] _items;
        private int _length;

        public Span<T> AsSpan() => _items.AsSpan(0, _length);

        public ValueList(int capacity, ArrayPool<T>? pool = null)
        {
            Guard.Positive(capacity);
            _pool = pool;
            _length = 0;
            _items = null!;
            _items = AllocateArray(capacity);
        }

        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                AsSpan().Clear();
            }
            _length = 0;
        }

        public void Trim()
        {
            var old = _items;

            _items = AllocateArray(_length);

            old.AsSpan(0, _length).CopyTo(AsSpan());

            FreeArray(_items);
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
            _items = AllocateArray(size);

            old.AsSpan(0, _length).CopyTo(_items);

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                old.AsSpan().Clear();
            }

            FreeArray(_items);
        }

        private T[] AllocateArray(int size)
        {
            if (_pool is null)
            {
                return new T[size];
            }
            else
            {
                return _pool.Rent(size);
            }
        }
        private void FreeArray(T[] arr)
        {
            if (_pool is not null)
            {
                _pool.Return(arr);
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
        public int Count => _length;
    }
}
