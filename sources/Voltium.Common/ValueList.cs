using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Common
{
    internal struct ValueList<T>
    {
        public ValueList(int capacity)
        {
            Guard.Positive(capacity);
            _items = new T[capacity];
            _length = capacity;
        }

        public void Add(in T item)
        {
            if (_length < _items.Length)
            {
                _items[_length++] = item;
                return;
            }

            AddWithExtension(item);
        }

        public void Add(T item)
        {
            if (_length < _items.Length)
            {
                _items[_length++] = item;
                return;
            }

            AddWithExtension(item);
        }

        private void AddWithExtension(T item)
        {
            var old = _items;
            _items = new T[_items.Length * 2];
            old.AsSpan().CopyTo(_items);
            _items[_length++] = item;
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

        private T[] _items;
        private int _length;

        public int Length => _length;
    }
}
