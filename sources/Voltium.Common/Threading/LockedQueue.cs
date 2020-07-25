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
            _underlyingQueue = new Queue<T>(capacity);
            _lock = @lock;
        }

        private Queue<T> _underlyingQueue;
        private TLock _lock;

        public Queue<T> GetUnderlyingQueue() => _underlyingQueue;

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => _underlyingQueue.Count;

        public Queue<T>.Enumerator GetEnumerator()
        {
            using var _ = _lock.EnterScoped();
            return _underlyingQueue.GetEnumerator();
        }

        public void Enqueue(T value)
        {
            using var _ = _lock.EnterScoped();
            _underlyingQueue.Enqueue(value);
        }

        public T Dequeue()
        {
            using var _ = _lock.EnterScoped();
            return _underlyingQueue.Dequeue();
        }

        public T Peek()
        {
            using var _ = _lock.EnterScoped();
            return _underlyingQueue.Peek();
        }

        public bool TryPeek([MaybeNullWhen(false)] out T value)
        {
            using var _ = _lock.EnterScoped();
            return _underlyingQueue.TryPeek(out value);
        }

        public bool TryDequeue([MaybeNullWhen(false)] out T value)
        {
            using var _ = _lock.EnterScoped();
            return _underlyingQueue.TryDequeue(out value);
        }

        public void Clear()
        {
            using var _ = _lock.EnterScoped();
            _underlyingQueue.Clear();
        }
    }
}
