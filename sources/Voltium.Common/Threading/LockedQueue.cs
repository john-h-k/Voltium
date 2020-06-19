using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Voltium.Common.Threading;

namespace Voltium.Common
{
    internal struct LockedQueue<T, TLock> : IEnumerable<T> where TLock : struct, IValueLock
    {
        public LockedQueue(TLock @lock, int capacity = 0)
        {
            Guard.Positive(capacity);
            UnderlyingQueue = new Queue<T>(capacity);
            _lock = @lock;
        }

        private Queue<T> UnderlyingQueue;
        private TLock _lock;

        public Queue<T> GetUnderlyingQueue() => UnderlyingQueue;

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public Queue<T>.Enumerator GetEnumerator()
        {
            var taken = false;
            _lock.Enter(ref taken);

            try
            {
                return UnderlyingQueue.GetEnumerator();
            }
            finally
            {
                ExitIf(taken);
            }
        }

        private void ExitIf(bool taken)
        {
            if (taken)
            {
                _lock.Exit();
            }
        }

        public void Enqueue(T value)
        {
            var taken = false;
            _lock.Enter(ref taken);

            try
            {
                UnderlyingQueue.Enqueue(value);
            }
            finally
            {
                ExitIf(taken);
            }
        }

        public T Dequeue()
        {
            var taken = false;
            _lock.Enter(ref taken);

            try
            {
                return UnderlyingQueue.Dequeue();
            }
            finally
            {
                ExitIf(taken);
            }
        }

        public T Peek()
        {
            var taken = false;
            _lock.Enter(ref taken);

            try
            {
                return UnderlyingQueue.Peek();
            }
            finally
            {
                ExitIf(taken);
            }
        }

        public bool TryPeek([MaybeNullWhen(false)] out T value)
        {
            var taken = false;
            _lock.Enter(ref taken);

            try
            {

                return UnderlyingQueue.TryPeek(out value);
            }
            finally
            {
                ExitIf(taken);
            }
        }

        public bool TryDequeue([MaybeNullWhen(false)] out T value)
        {
            var taken = false;
            _lock.Enter(ref taken);

            try
            {

                return UnderlyingQueue.TryDequeue(out value);
            }
            finally
            {
                ExitIf(taken);
            }
        }
    }
}
