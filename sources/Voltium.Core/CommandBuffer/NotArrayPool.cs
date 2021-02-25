using System.Buffers;

namespace Voltium.Core.CommandBuffer
{
    internal sealed class NotArrayPool<T> : ArrayPool<T>
    {
        public override T[] Rent(int minimumLength) => new T[minimumLength];
        public override void Return(T[] array, bool clearArray = false) {  }
    }
}
