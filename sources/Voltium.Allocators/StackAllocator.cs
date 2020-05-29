using System;
using System.Buffers;
using System.Runtime.InteropServices;
using Voltium.Common;

namespace Voltium.Allocators
{
    /// <summary>
    /// A type used to allocate blocks of <typeparamref name="T"/>s in a stack-like manner
    /// </summary>
    /// <typeparam name="T">The type used in allocation</typeparam>
    public class StackAllocator<T> : MemoryPool<T>
    {
        private static readonly ArrayPool<T> DefaultPool = ArrayPool<T>.Create();
        private T[] _pool;
        private int _head;
        private readonly bool _isPrePinned;

        /// <summary>
        /// Creates a new allocator with <paramref name="elementCount"/> elements
        /// </summary>
        /// <param name="elementCount">The number of elements</param>
        /// <param name="pin">Whether the pool should be pinned</param>
        public StackAllocator(int elementCount, bool pin = false)
        {
            if (elementCount < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(elementCount));
            }

            _pool = DefaultPool.Rent(elementCount);
            _head = 0;
            _isPrePinned = pin;

            // https://github.com/dotnet/runtime/issues/36183
            if (pin)
            {
                ThrowHelper.ThrowNotSupportedException();
            }
        }

        /// <inheritdoc/>
        public override int MaxBufferSize => _pool.Length;

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            DefaultPool.Return(_pool);
            _pool = null!;
            _head = 0;
        }

        private const int DefaultMinBufferSize = 1;

        /// <inheritdoc/>
        public override IMemoryOwner<T> Rent(int minBufferSize = -1)
        {
            if (minBufferSize == -1)
            {
                minBufferSize = DefaultMinBufferSize;
            }

            var oldHead = _head;

            var memBlock = Memory<T>.Empty;

            if (minBufferSize > 0)
            {
                ThrowForBadAlloc(minBufferSize);
                memBlock = _isPrePinned
                    ? MemoryMarshal.CreateFromPinnedArray(_pool, oldHead, minBufferSize)
                    : new Memory<T>(_pool, _head, minBufferSize);
            }

            _head += minBufferSize;

            // https://github.com/dotnet/csharplang/issues/3445
            // removes boxing here
            return new StackMemoryOwner(memBlock, oldHead);
        }

        /// <summary>
        /// Deallocates a stack element and all elements after it
        /// </summary>
        /// <param name="marker">The first element to deallocate</param>
        public void Deallocate(StackMemoryOwner marker) => Deallocate(marker.Marker);

        internal void Deallocate(int marker)
        {
            if (marker < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(marker));
            }

            _head = marker;
        }

        /// <summary>
        /// Releases all blocks and allows the memory to be reused
        /// </summary>
        public void Reset() => _head = 0;

        private void ThrowForBadAlloc(int sz)
        {
            if (sz > _pool.Length - _head)
            {
                ThrowHelper.ThrowInsufficientMemoryException();
            }
        }

        /// <summary>
        /// Represents a section of memory from a <see cref="StackAllocator{T}"/>
        /// </summary>
        public readonly struct StackMemoryOwner : IMemoryOwner<T>
        {
            /// <summary>
            /// Creates a new instance of <see cref="StackMemoryOwner"/> from the specific memory block
            /// </summary>
            /// <param name="memory">The memory block</param>
            /// <param name="marker"></param>
            public StackMemoryOwner(Memory<T> memory, int marker)
            {
                Memory = memory;
                Marker = marker;
            }

            /// <inheritdoc cref="IMemoryOwner{T}"/>
            public Memory<T> Memory { get; }

            /// <summary>
            /// The marker used by <see cref="StackAllocator{T}"/> to deallocate
            /// </summary>
            public int Marker { get; }

            /// <inheritdoc cref="IMemoryOwner{T}"/>
            public void Dispose()
            {
            }
        }
    }
}
