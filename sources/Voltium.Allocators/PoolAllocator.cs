using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using Voltium.Common;

namespace Voltium.Allocators
{
    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PoolAllocator<T> : ObjectPool<T> where T : class
    {
        private static readonly ArrayPool<T> DefaultPool = ArrayPool<T>.Create();
        private T[] _pool;

        private LinkedNode<int>? _freeTs;

        /// <summary>
        ///
        /// </summary>
        /// <param name="elementCount"></param>
        /// <param name="pin"></param>
        public PoolAllocator(int elementCount, bool pin = false)
        {
            if (elementCount < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(elementCount));
            }

            _pool = DefaultPool.Rent(elementCount);

            _freeTs = new LinkedNode<int>(0);
            var node = _freeTs;

            for (var i = 1; i < _pool.Length; i++)
            {
                node.Next = new LinkedNode<int>(i);
                node = node.Next;
            }

            // https://github.com/dotnet/runtime/issues/36183
            if (pin)
            {
                ThrowHelper.ThrowNotSupportedException();
            }
        }

        /// <inheritdoc/>
        public override T Rent(bool resetObject = false)
        {
            ThrowForBadAlloc();

            var node = _freeTs;
            _freeTs = node!.Next;

            var obj = _pool[node.Value];

            if (resetObject && obj is IResettable resettable)
            {
                resettable.Reset();
            }

            return obj;
        }

        /// <inheritdoc/>
        public override IObjectOwner<T> RentAsDisposable(bool resetObject = false)
            => new PooledObjectOwner(Rent(resetObject), this);

        /// <summary>
        ///
        /// </summary>
        public struct PooledObjectOwner : IObjectOwner<T>
        {
            private PoolAllocator<T> _allocator;
            /// <summary>
            ///
            /// </summary>
            public T Object { get; }

            /// <summary>
            ///
            /// </summary>
            /// <param name="obj"></param>
            /// <param name="allocator"></param>
            public PooledObjectOwner(T obj, PoolAllocator<T> allocator)
            {
                Object = obj;
                _allocator = allocator;
            }

            /// <summary>
            ///
            /// </summary>
            public void Dispose()
            {
                _allocator.Return(Object);
            }
        }

        private void ThrowForBadAlloc()
        {
            if (_freeTs is null)
            {
                ThrowHelper.ThrowInsufficientMemoryException();
            }
        }



        /// <inheritdoc/>
        public override void Return(T obj)
        {
            var node = _freeTs;
            while (node?.Next != null)
            {
                node = node.Next;
            }
            node.Next = new LinkedNode<int>();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            throw new System.NotImplementedException();
        }
    }
}
