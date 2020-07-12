using System.Runtime.CompilerServices;

namespace Voltium.Core.Memory
{
    internal unsafe struct HeapTypeSet<T>
    {
        public T Default;
        public T Upload;
        public T Readback;

        public T this[MemoryAccess context]
        {
            get => Unsafe.Add(ref Default, (int)context - 1);

            set => Unsafe.Add(ref Default, (int)context - 1) = value;
        }
    }

    internal struct HeapContentSet<T>
    {
        public T Tex;
        public T RtOrDs;
        public T Buffer;

        public T this[GpuResourceType context]
        {
            get => Unsafe.Add(ref Tex, (int)context - 1);

            set => Unsafe.Add(ref Tex, (int)context - 1) = value;
        }
    }

    internal enum GpuResourceType
    {
        Meaningless = 0,
        Tex = 1,
        RtOrDs = 2,
        Buffer = 3
    }
}
