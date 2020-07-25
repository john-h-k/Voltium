using System;
using System.Collections.Generic;

namespace Voltium.Common.Threading
{
    internal struct LockedList<T, TLock> where TLock : struct, IValueLock
    {
        public LockedList(TLock @lock, int capacity = 4)
        {
            Guard.Positive(capacity);
            _underlyingList = new List<T>(capacity);
            _lock = @lock;
        }

        private List<T> _underlyingList;
        private TLock _lock;

        public int Count => _underlyingList.Count;

        public T this[int index]
        {
            get
            {
                using (_lock.EnterScoped())
                {
                    return _underlyingList[index];
                }
            }

            set
            {
                using (_lock.EnterScoped())
                {
                    _underlyingList[index] = value;
                }
            }
        }

        public void Add(T value)
        {
            using var _ = _lock.EnterScoped();
            _underlyingList.Add(value);
        }
    }
}
