using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;

namespace Voltium.Common
{
    internal static unsafe class Helpers
    {
        public static IntPtr Heap { get; } = Windows.GetProcessHeap();

        public static void* Alloc(nuint size) => Windows.HeapAlloc(Heap, 0, size);
        public static void* Alloc(nint size) => Alloc((nuint)size);

        public static void Free(void* data) => Windows.HeapFree(Heap, 0, data);

        public static bool IsNullRef<T>(ref T val)
          => Unsafe.AsPointer(ref val) == null;

        public static bool IsNullIn<T>(in T val)
            => IsNullRef(ref Unsafe.AsRef(in val));

        public static bool IsNullOut<T>(out T val)
        {
            Unsafe.SkipInit(out val);
            return IsNullRef(ref val);
        }

        public static ref T NullRef<T>()
            => ref Unsafe.AsRef<T>(null);
    }
}
