using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
            using (_poolLock.EnterScoped())
            {
                if (_pool.TryDequeue(out var ptr))
                {
                    return ptr.Move();
                }
            }

            return Create().Move();
        }

        public void Return(ComPtr<T> value)
        {
            using (_poolLock.EnterScoped())
            {
                _pool.Enqueue(value.Move());
            }
        }

        protected abstract ComPtr<T> Create();

        public void Dispose()
        {
            for (var i = 0; i < _pool.Count; i++)
            {
                _pool.Dequeue().Dispose();
            }
        }
    }
}
