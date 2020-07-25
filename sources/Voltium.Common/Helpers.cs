using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;

namespace Voltium.Common
{
    internal unsafe static class Helpers
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

        public static bool IsGuidEqual(Guid* lhs, Guid* rhs)
            => IsGuidEqual(in *lhs, in *rhs);
        public static bool IsGuidEqual(in Guid lhs, in Guid rhs)
        {
            if (Unsafe.AreSame(ref Unsafe.AsRef(in lhs), ref Unsafe.AsRef(in rhs)))
            {
                return true;
            }

            if (Sse2.IsSupported)
            {
                var comp = Sse2.CompareEqual(
                    Unsafe.As<Guid, Vector128<byte>>(ref Unsafe.AsRef(in lhs)),
                    Unsafe.As<Guid, Vector128<byte>>(ref Unsafe.AsRef(in rhs))
                );

                return Sse2.MoveMask(comp) == 0xFFFF;
            }

            return lhs.Equals(rhs);
        }

        public static bool Int32ToBool(int val) => val != 0;
        public static int BoolToInt32(bool val) => Unsafe.As<bool, byte>(ref val);
    }
}
