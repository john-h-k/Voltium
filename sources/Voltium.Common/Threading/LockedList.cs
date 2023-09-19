using System;
using System.Collections;
using System.Collections.Generic;

namespace Voltium.Common.Threading
{
    internal struct LockedList<T, TLock> where TLock : struct, IValueLock
    {
        public LockedList(TLock @lock, int capacity = 4)
        {
            Guard.Positive(capacity);
            UnderlyingList = new List<T>(capacity);
            _lock = @lock;
        }

        public List<T> UnderlyingList { get; private set; }
        private TLock _lock;

        public object SyncRoot => ((ICollection)UnderlyingList).SyncRoot;
        public int Count => UnderlyingList.Count;

        // public ScopedIValueLockEntry<TLock> EnterScopedLock()
        //     => _lock.EnterScoped();

        public T this[int index]
        {
            get
            {
                using (_lock.EnterScoped())
                {
                    return UnderlyingList[index];
                }
            }

            set
            {
                using (_lock.EnterScoped())
                {
                    UnderlyingList[index] = value;
                }
            }
        }

        public void Add(T value)
        {
            using var _ = _lock.EnterScoped();
            UnderlyingList.Add(value);
        }
    }
}
