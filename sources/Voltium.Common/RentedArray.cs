using System;
using System.Buffers;

namespace Voltium.Common
{
    // A thin wrapper over a rented array to allow 'using' blocks and minimise resource leaks
    internal readonly struct RentedArray<T> : IDisposable
    {
        public readonly T[] Value;
        public readonly ArrayPool<T> Pool;

        private RentedArray(T[] value, ArrayPool<T> pool)
        {
            Value = value;
            Pool = pool;
        }

        public static RentedArray<T> Create(int minimumLength, ArrayPool<T> pool = null!)
        {
            pool ??= ArrayPool<T>.Shared;

            return new RentedArray<T>(pool.Rent(minimumLength), pool);
        }

        public void Dispose() => Dispose(false);
        public void Dispose(bool clear)
        {
            Pool.Return(Value, clear);
        }
    }
}
