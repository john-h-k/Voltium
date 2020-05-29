using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Common.Threading
{
    [Obsolete("TODO", true)]
    internal struct LockedList<T, TLock> where TLock : struct, IValueLock
    {
        public LockedList(TLock @lock, int capacity = -1)
        {
            Guard.Positive(capacity);
            UnderlyingList = new List<T>(capacity);
            _lock = @lock;
        }

        private List<T> UnderlyingList;
        private TLock _lock;

        public List<T> GetUnderlyingQueue() => UnderlyingList;

        public T this[int index]
        {
            get
            {
                var taken = false;
                _lock.Enter(ref taken);
                try
                {
                    return UnderlyingList[index];
                }
                finally
                {
                    ExitIf(taken);
                }
            }

            set
            {
                var taken = false;
                _lock.Enter(ref taken);
                try
                {
                    UnderlyingList[index] = value;
                }
                finally
                {
                    ExitIf(taken);
                }
            }
        }

        private void ExitIf(bool taken)
        {
            if (taken)
            {
                _lock.Exit();
            }
        }
    }
}
