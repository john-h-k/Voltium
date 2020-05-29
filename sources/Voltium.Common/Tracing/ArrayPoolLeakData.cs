using System.Buffers;
using static Voltium.Common.Tracing.ArrayPoolLeakData;

namespace Voltium.Common.Tracing
{
    internal sealed class ArrayPoolLeakData
    {
        public enum LikelyLeakReason
        {
            DoubleReturn,
            InvalidReturn,
            ThreadCorruption
        }
    }
    internal sealed class ArrayPoolLeakData<T>
    {
        public T[] LeakedArray { get; }
        public ArrayPool<T> Pool { get; }
        public LikelyLeakReason Reason { get; }

        public ArrayPoolLeakData(T[] leakedArray, ArrayPool<T> pool, LikelyLeakReason reason)
        {
            LeakedArray = leakedArray;
            Pool = pool;
            Reason = reason;
        }
    }
}
