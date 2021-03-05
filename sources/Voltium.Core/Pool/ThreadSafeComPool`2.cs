using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Voltium.Common;
using Voltium.Common.Debugging;
using Voltium.Common.Threading;

namespace Voltium.Core.Pool
{
    internal abstract class ThreadSafeComPool<T, TRentState> : IDisposable where T : unmanaged
    {
        private SpinLock _poolLock = new(ConfigVars.IsDebug);
        private Queue<UniqueComPtr<T>> _pool;

        public ThreadSafeComPool(int capacity = 0)
        {
            Debug.Assert(capacity >= 0);
            _pool = new(capacity);
        }

        public UniqueComPtr<T> Rent(TRentState state)
        {
            UniqueComPtr<T> result;

            using (_poolLock.EnterScoped())
            {
                if (_pool.TryPeek(out var ptr) && CanRent(ref ptr, state))
                {
                    _ = _pool.Dequeue();
                    result = ptr.Move();
                }
                else
                {
                    result = Create(state).Move();
                }
            }

            ManageRent(ref result, state);

            return result;
        }

        public void Return(UniqueComPtr<T> value)
        {
            ManageReturn(ref value);
            using (_poolLock.EnterScoped())
            {
                _pool.Enqueue(value.Move());
            }
        }

        protected abstract UniqueComPtr<T> Create(TRentState state);
        protected virtual void ManageRent(ref UniqueComPtr<T> value, TRentState state) { }
        protected virtual void ManageReturn(ref UniqueComPtr<T> value) { }
        protected virtual bool CanRent(ref UniqueComPtr<T> value, TRentState state) => true;

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
