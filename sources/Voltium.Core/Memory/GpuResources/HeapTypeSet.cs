using System.Runtime.CompilerServices;

namespace Voltium.Core.GpuResources
{
    internal unsafe struct HeapTypeSet<T>
    {
        public T Default;
        public T Upload;
        public T Readback;

        public T this[GpuMemoryType context]
        {
            get => Unsafe.Add(ref Default, (int)context - 1);

            set => Unsafe.Add(ref Default, (int)context - 1) = value;
        }
    }

}
