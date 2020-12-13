using System.Buffers;
using System.Diagnostics;

namespace Voltium.Common.Tracing
{
    internal struct ArrayPoolTracer<T>
    {
        private enum ArrayState
        {
            InPool,
            OutOfPool
        }

#if TRACE_MEMORY
        private ConcurrentDictionary<T[], ArrayState> _arrays;
        private ArrayPool<T> _owner;
#endif

        public static ArrayPoolTracer<T> Create(ArrayPool<T> owner)
        {
#if TRACE_MEMORY
            var tracer = new ArrayPoolTracer<T> {_arrays = new ConcurrentDictionary<T[], ArrayState>(), _owner = owner};

            return tracer;
#else
            return default;
#endif
        }

        [Conditional("TRACE_MEMORY")]
        public void MarkNewBuffer(T[] arr, bool isInPool)
        {
#if TRACE_MEMORY
            Debug.Assert(!_arrays.ContainsKey(arr));

            _arrays[arr] = isInPool ? ArrayState.InPool : ArrayState.OutOfPool;
#endif
        }

        [Conditional("TRACE_MEMORY")]
        public void MarkBufferRetired(T[] arr)
        {
#if TRACE_MEMORY
            Debug.Assert(_arrays.ContainsKey(arr));

            _arrays.TryRemove(arr, out _);
#endif
        }

        [Conditional("TRACE_MEMORY")]
        public void MarkRented(T[] arr)
        {
#if TRACE_MEMORY
            if (!_arrays.TryGetValue(arr, out ArrayState state))
            {
                ThrowHelper.ThrowArrayPoolLeakException(
                    "Attempt to rent an array to the array pool which was not created by the pool - this means an invalid return has occured," +
                    "or a created buffer was not reported properly to the tracer", arr,
                    GetData(arr, LikelyLeakReason.InvalidReturn));
            }

            if (state != ArrayState.InPool)
            {
                ThrowHelper.ThrowArrayPoolLeakException(
                    "Attempt to rent an array that was considered part of array pool. Memory leak has occured" +
                    "It is likely an array was double returned and tracing did not catch it", arr,
                    GetData(arr, LikelyLeakReason.DoubleReturn));
            }

            _arrays[arr] = ArrayState.OutOfPool;
#endif
        }

        [Conditional("TRACE_MEMORY")]
        public void MarkReturned(T[] arr)
        {
#if TRACE_MEMORY
            if (!_arrays.TryGetValue(arr, out ArrayState state))
            {
                ThrowHelper.ThrowArrayPoolLeakException(
                    "Trying to return an array to the array pool which was not created by the pool", arr,
                    GetData(arr, LikelyLeakReason.InvalidReturn));
            }

            if (state != ArrayState.OutOfPool)
            {
                ThrowHelper.ThrowArrayPoolLeakException("Returned array was considered part of array pool." +
                                                        "Either an irrelevant array was returned, or a double return occured",
                    arr, GetData(arr, LikelyLeakReason.DoubleReturn));
            }

            _arrays[arr] = ArrayState.InPool;
#endif
        }

#if TRACE_MEMORY
        private ArrayPoolLeakData<T> GetData(T[] arr, LikelyLeakReason reason) =>
            new ArrayPoolLeakData<T>(arr, _owner, reason);
#endif
    }
}
