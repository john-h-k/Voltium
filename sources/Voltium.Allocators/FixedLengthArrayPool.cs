using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Voltium.Common;
using Voltium.Common.Tracing;
using static Voltium.Common.MethodTypes;

namespace Voltium.Allocators
{
    /// <summary>
    /// An array pool for fixed length arrays
    /// </summary>
    /// <typeparam name="T">The element type of the arrays</typeparam>
    public sealed class FixedLengthArrayPool<T> : ArrayPool<T>
    {
        private readonly Stack<T[]> _arrays = new Stack<T[]>();
        private SpinLock _lock = new SpinLock();

        private ArrayPoolTracer<T> _tracer
#if TRACE_MEMORY
        { get; }
#else
        {
            get
            {
                ThrowHelper.ConditionalCompilationPath();
                return default;
            }

            set => ThrowHelper.ConditionalCompilationPath();
        }
#endif

        /// <summary>
        /// The size of arrays pooled by this type, as a count of elements of <typeparamref name="T"/>
        /// </summary>
        public int ArraySize { get; }

        /// <summary>
        /// The default number of arrays enqueued to the pool
        /// </summary>
        public static int DefaultInitialBufferCount => 16;

        /// <summary>
        /// The default max number of arrays that can be held by the pool
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType TODO maybe put in seperate
        public static int DefaultMaximumCapacity { get; set; } = 256;


        /// <summary>
        /// The default number of arrays enqueued to the pool
        /// </summary>
        public int MaximumCapacity { get; set; }


        /// <summary>
        /// Create a new fixed array pool with a size
        /// </summary>
        /// <param name="arraySize">The size of the arrays returned by the pool</param>
        /// <param name="initialBufferCount">The initial number of buffers added to the pool. If -1 is provided, or no value provided,
        /// this is <see cref="DefaultInitialBufferCount"/></param>
        /// <param name="maximumCapacity">The maximum number of buffers the pool can hold at once.  If -1 is provided, or no value provided,
        /// this is <see cref="DefaultMaximumCapacity"/></param>
        public FixedLengthArrayPool(int arraySize, int initialBufferCount = -1, int maximumCapacity = -1)
        {
            _tracer = ArrayPoolTracer<T>.Create(this);

            if (arraySize < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(arraySize), arraySize);
            }

            if (maximumCapacity == -1)
            {
                maximumCapacity = DefaultMaximumCapacity;
            }

            Guard.Positive(maximumCapacity);

            if (initialBufferCount == -1)
            {
                initialBufferCount = DefaultInitialBufferCount;
            }

            Guard.InRangeInclusive(initialBufferCount, 0, maximumCapacity);

            for (int i = 0; i < initialBufferCount; i++)
            {
                AddBuffer();
            }

            ArraySize = arraySize;
            MaximumCapacity = maximumCapacity;
        }

        private void AddBuffer()
        {
            var buff = new T[ArraySize];
            PushNewBuffer(buff);
        }

        /// <summary>
        /// Rent a new array. The array is guaranteed to be <see cref="ArraySize"/> elements of <typeparamref name="T"/>
        /// </summary>
        /// <param name="minimumLength">This parameter is ignored</param>
        /// <returns>An array of <see cref="ArraySize"/> elements of <typeparamref name="T"/></returns>
        // ReSharper disable once OptionalParameterHierarchyMismatch
        public override T[] Rent(int minimumLength = -1)
        {
            if (TryPop(out T[] value))
            {
                return value;
            }

            var newBuff = new T[ArraySize];
            _tracer.MarkNewBuffer(newBuff, isInPool: false);
            return newBuff;
        }

        /// <summary>
        /// Return an array rented with <see cref="Rent"/>
        /// </summary>
        /// <param name="array"></param>
        /// <param name="clearArray"></param>
        public override void Return(T[] array, bool clearArray = false)
        {
            Debug.Assert(array.Length == ArraySize);

            if (clearArray)
            {
                array.AsSpan().Clear();
            }

            if (_arrays.Count < MaximumCapacity)
            {
                Push(array);
            }
        }

        [MethodImpl(Wrapper)]
        private void Push(T[] value)
        {
            bool taken = EnterLock();
            _arrays.Push(value);
            _tracer.MarkReturned(value);
            ExitLock(taken);
        }

        [MethodImpl(Wrapper)]
        private void PushNewBuffer(T[] value)
        {
            bool taken = EnterLock();
            _tracer.MarkNewBuffer(value, isInPool: true);
            _arrays.Push(value);
            ExitLock(taken);
        }

        [MethodImpl(Wrapper)]
        private bool TryPop(out T[] value)
        {
            bool taken = EnterLock();
            bool b = _arrays.TryPop(out value!);
            _tracer.MarkRented(value);
            ExitLock(taken);

            return b;
        }

        [MethodImpl(Wrapper)]
        private bool EnterLock()
        {
            bool taken = false;
            _lock.Enter(ref taken);
            return taken;
        }

        [MethodImpl(Wrapper)]
        private void ExitLock(bool taken)
        {
            if (taken)
            {
                _lock.Exit();
            }
        }
    }
}
