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
        private SpinLock _poolLock = new(EnvVars.IsDebug);
        private Queue<ComPtr<T>> _pool;

        public ThreadSafeComPool(int capacity = 0)
        {
            Debug.Assert(capacity >= 0);
            _pool = new(capacity);
        }

        public ComPtr<T> Rent()
        {
            ComPtr<T> result;

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

        public void Return(ComPtr<T> value)
        {
            ManageReturn(ref value);

            using (_poolLock.EnterScoped())
            {
                _pool.Enqueue(value.Move());
            }
        }

        protected abstract ComPtr<T> Create();

        protected abstract void ManageRent(ref ComPtr<T> value);
        protected abstract void ManageReturn(ref ComPtr<T> value);

        protected virtual bool CanRent(ref ComPtr<T> value) => true;

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
