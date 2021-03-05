using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Voltium.Common;
using Voltium.Common.Debugging;
using Voltium.Common.Threading;

namespace Voltium.Core.Pool
{
    internal abstract class ThreadSafeComPool<T> : IDisposable where T : unmanaged
    {
        private SpinLock _poolLock = new(ConfigVars.IsDebug);
        private Queue<UniqueComPtr<T>> _pool;

        public ThreadSafeComPool(int capacity = 0)
        {
            Debug.Assert(capacity >= 0);
            _pool = new(capacity);
        }

        public UniqueComPtr<T> Rent()
        {
            UniqueComPtr<T> result;

            using (_poolLock.EnterScoped())
            {
                if (_pool.TryDequeue(out var ptr))
                {
                    result = ptr.Move();
                }
                else
                {
                    result = Create().Move();
                }
            }

            ManageRent(ref result);

            return result;
        }

        // i don't think this needs a lock? right?
        public bool IsEmpty => _pool.Count == 0;

        public void Return(UniqueComPtr<T> value)
        {
            ManageReturn(ref value);

            using (_poolLock.EnterScoped())
            {
                _pool.Enqueue(value.Move());
            }
        }

        protected abstract UniqueComPtr<T> Create();

        protected abstract void ManageRent(ref UniqueComPtr<T> value);
        protected abstract void ManageReturn(ref UniqueComPtr<T> value);

        protected virtual bool CanRent(ref UniqueComPtr<T> value) => true;

        protected virtual void InternalDispose()
        {
            for (var i = 0; i < _pool.Count; i++)
            {
                _pool.Dequeue().Dispose();
            }
        }

        public void Dispose()
        {
            InternalDispose();
        }
    }
}
