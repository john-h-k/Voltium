using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Common
{
    [Obsolete("Unfinished, TODO", true)]
    internal struct ValueQueue<T>
    {
        private T[] _items;
        private int _head;
        private int _tail;
        private int _size;

        public ValueQueue(int capacity)
        {
            _items = new T[capacity];
            _head = 0;
            _tail = capacity - 1;
            _size = 0;
        }

        public void Enqueue(in T value)
        {
            if (_tail < _items.Length)
            {
                _items[_tail++] = value;
                return;
            }

            EnqueueWithExtension(value);
        }
        public void Enqueue(T value)
        {
            if (_tail < _items.Length)
            {
                _items[_tail] = value;
            }

            EnqueueWithExtension(value);
        }

        public T Dequeue()
        {
            if (_head <= _tail)
            {
                var item = _items[_head];
                MoveNext(ref _tail);
                _size--;
                return item;
            }

            ThrowHelper.ThrowInvalidOperationException("Empty queue cannot be dequeued");
            return default;
        }

        public unsafe ref T Peek()
        {
            if (_head <= _tail)
            {
                return ref _items[_head];
            }

            ThrowHelper.ThrowInvalidOperationException("Empty queue cannot be peeked");
            return ref NullRef;
        }

        private static unsafe ref T NullRef => ref Unsafe.AsRef<T>(null);

        private void MoveNext(ref int val)
        {
            int tmp = val + 1;
            if (tmp == _size)
            {
                val = 0;
            }
            val = tmp;
        }

        private void EnqueueWithExtension(T item)
        {
            var old = _items;
            _items = new T[_items.Length * 2];
            old.AsSpan().CopyTo(_items);
            _items[_tail++] = item;
        }
    }
}
