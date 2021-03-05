using System;
using System.Buffers;
using System.Diagnostics;
using System.Threading;
using Voltium.Common;

namespace Voltium.Core
{
    public struct ValueResettableArrayBufferWriter<T> : IBufferWriter<T>
    {
        private const int MaxArrayLength = 0X7FEFFFFF;

        private const int DefaultInitialBufferSize = 256;

        private T[] _buffer;
        private int _index;

        /// <summary>
        /// Creates an instance of an <see cref="ArrayBufferWriter{T}"/>, in which data can be written to,
        /// with an initial capacity specified.
        /// </summary>
        /// <param name="initialCapacity">The minimum capacity with which to initialize the underlying buffer.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="initialCapacity"/> is not positive (i.e. less than or equal to 0).
        /// </exception>
        public ValueResettableArrayBufferWriter(int initialCapacity)
        {
            if (initialCapacity <= 0)
                throw new ArgumentException(null, nameof(initialCapacity));

            _buffer = new T[initialCapacity];
            _index = 0;
        }

        /// <summary>
        /// Returns the data written to the underlying buffer so far, as a <see cref="ReadOnlyMemory{T}"/>.
        /// </summary>
        public ReadOnlyMemory<T> WrittenMemory => _buffer.AsMemory(0, _index);

        /// <summary>
        /// Returns the data written to the underlying buffer so far, as a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        public ReadOnlySpan<T> WrittenSpan => _buffer.AsSpan(0, _index);

        /// <summary>
        /// Returns the amount of data written to the underlying buffer so far.
        /// </summary>
        public int WrittenCount => _index;

        /// <summary>
        /// Returns the total amount of space within the underlying buffer.
        /// </summary>
        public int Capacity => _buffer.Length;

        /// <summary>
        /// Returns the amount of space available that can still be written into without forcing the underlying buffer to grow.
        /// </summary>
        public int FreeCapacity => _buffer.Length - _index;

        /// <summary>
        /// Clears the data written to the underlying buffer.
        /// </summary>
        /// <remarks>
        /// You must clear the <see cref="ArrayBufferWriter{T}"/> before trying to re-use it.
        /// </remarks>
        public void Clear(bool zeroBackingBuffer)
        {
            Debug.Assert(_buffer.Length >= _index);
            if (zeroBackingBuffer)
            {
                _buffer.AsSpan(0, _index).Clear();
            }
            _index = 0;
        }

        /// <summary>
        /// Notifies <see cref="IBufferWriter{T}"/> that <paramref name="count"/> amount of data was written to the output <see cref="Span{T}"/>/<see cref="Memory{T}"/>
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="count"/> is negative.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to advance past the end of the underlying buffer.
        /// </exception>
        /// <remarks>
        /// You must request a new buffer after calling Advance to continue writing more data and cannot write to a previously acquired buffer.
        /// </remarks>
        public void Advance(int count)
        {
            if (count < 0)
                throw new ArgumentException(null, nameof(count));

            if (_index > _buffer.Length - count)
                ThrowInvalidOperationException_AdvancedTooFar(_buffer.Length);

            _index += count;
        }

        /// <summary>
        /// Returns a <see cref="Memory{T}"/> to write to that is at least the requested length (specified by <paramref name="sizeHint"/>).
        /// If no <paramref name="sizeHint"/> is provided (or it's equal to <code>0</code>), some non-empty buffer is returned.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="sizeHint"/> is negative.
        /// </exception>
        /// <remarks>
        /// This will never return an empty <see cref="Memory{T}"/>.
        /// </remarks>
        /// <remarks>
        /// There is no guarantee that successive calls will return the same buffer or the same-sized buffer.
        /// </remarks>
        /// <remarks>
        /// You must request a new buffer after calling Advance to continue writing more data and cannot write to a previously acquired buffer.
        /// </remarks>
        public Memory<T> GetMemory(int sizeHint = 0)
        {
            CheckAndResizeBuffer(sizeHint);
            Debug.Assert(_buffer.Length > _index);
            return _buffer.AsMemory(_index);
        }

        /// <summary>
        /// Returns a <see cref="Span{T}"/> to write to that is at least the requested length (specified by <paramref name="sizeHint"/>).
        /// If no <paramref name="sizeHint"/> is provided (or it's equal to <code>0</code>), some non-empty buffer is returned.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="sizeHint"/> is negative.
        /// </exception>
        /// <remarks>
        /// This will never return an empty <see cref="Span{T}"/>.
        /// </remarks>
        /// <remarks>
        /// There is no guarantee that successive calls will return the same buffer or the same-sized buffer.
        /// </remarks>
        /// <remarks>
        /// You must request a new buffer after calling Advance to continue writing more data and cannot write to a previously acquired buffer.
        /// </remarks>
        public Span<T> GetSpan(int sizeHint = 0)
        {
            CheckAndResizeBuffer(sizeHint);
            Debug.Assert(_buffer.Length > _index);
            return _buffer.AsSpan(_index);
        }

        private void CheckAndResizeBuffer(int sizeHint)
        {
            if (sizeHint < 0)
            {
                ThrowHelper.ThrowArgumentException(nameof(sizeHint));
            }

            if (sizeHint == 0)
            {
                sizeHint = 1;
            }

            if (sizeHint > FreeCapacity)
            {
                int currentLength = _buffer.Length;

                // Attempt to grow by the larger of the sizeHint and double the current size.
                int growBy = Math.Max(sizeHint, currentLength);

                if (currentLength == 0)
                {
                    growBy = Math.Max(growBy, DefaultInitialBufferSize);
                }

                int newSize = currentLength + growBy;

                if ((uint)newSize > int.MaxValue)
                {
                    // Attempt to grow to MaxArrayLength.
                    uint needed = (uint)(currentLength - FreeCapacity + sizeHint);
                    Debug.Assert(needed > currentLength);

                    if (needed > MaxArrayLength)
                    {
                        ThrowOutOfMemoryException(needed);
                    }

                    newSize = MaxArrayLength;
                }

                Array.Resize(ref _buffer, newSize);
            }

            Debug.Assert(FreeCapacity > 0 && FreeCapacity >= sizeHint);
        }

        private static void ThrowInvalidOperationException_AdvancedTooFar(int capacity)
        {
            throw new InvalidOperationException();
        }

        private static void ThrowOutOfMemoryException(uint capacity)
        {
            throw new OutOfMemoryException();
        }
    }

    public struct PooledBufferWriter<T> : IBufferWriter<T>, IDisposable
    {
        private MemoryPool<T> _pool;
        private IMemoryOwner<T> _owner;
        private Memory<T> _memory;


        private const int DefaultCapacity = 0;

        public PooledBufferWriter(MemoryPool<T>? pool = null)
        {
            _pool = pool ?? MemoryPool<T>.Shared;
            _owner = _pool.Rent(DefaultCapacity);
            _memory = _owner.Memory;
        }

        public void Resize(int newSize)
        {
            var old = _owner;

            _owner = _pool.Rent(newSize);
            _memory = _owner.Memory;

            old.Memory.CopyTo(_memory);
            old.Dispose();
        }

        public void Advance(int count)
        {
            if (_memory.Length < count)
            {
                _memory = _memory.Slice(count);
                return;
            }

            Resize(Math.Max(_owner.Memory.Length * 2, count));
        }

        public Memory<T> GetMemory(int sizeHint = 0)
        {
            sizeHint = Math.Max(sizeHint, 1);

            if (_memory.Length < sizeHint)
            {
                Resize(Math.Max(_owner.Memory.Length * 2, sizeHint));
            }

            return _memory;
        }

        public Span<T> GetSpan(int sizeHint = 0) => GetMemory().Span;

        public void Dispose()
        {
            var owner = Interlocked.Exchange(ref _owner, null!);
            var pool = Interlocked.Exchange(ref _pool, null!);

            owner?.Dispose();
            pool?.Dispose();
        }
    }
}
