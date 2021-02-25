using System.Diagnostics;
using System.Numerics;

#pragma warning disable

namespace Voltium.Common
{
    public unsafe static class MathHelpers
    {
        public static T* AlignUp<T>(T* ptr, nuint alignment) where T : unmanaged
               => (T*)AlignUp((nuint)ptr, alignment);

        public static T* AlignUp<T>(T* ptr, nint alignment) where T : unmanaged
               => (T*)AlignUp((nuint)ptr, (nuint)alignment);

        public static ulong AlignUp(ulong ptr, ulong alignment)
        {
            Debug.Assert(BitOperations.PopCount(alignment) == 1);
            var mask = alignment - 1;
            return (ptr + mask) & ~mask;
        }

        public static nuint AlignUp(nuint ptr, nuint alignment)
        {
            Debug.Assert(BitOperations.PopCount(alignment) == 1);
            var mask = alignment - 1;
            return (ptr + mask) & ~mask;
        }



        public static int AlignUp(int ptr, int alignment)
        {
            Debug.Assert(BitOperations.PopCount((ulong)alignment) == 1);
            var mask = alignment - 1;
            return (ptr + mask) & ~mask;
        }

        public static nint AlignUp(nint ptr, nint alignment)
        {
            Debug.Assert(BitOperations.PopCount((ulong)alignment) == 1);
            var mask = alignment - 1;
            return (ptr + mask) & ~mask;
        }

        public static T* AlignDown<T>(T* ptr, nuint alignment) where T : unmanaged
               => (T*)AlignDown((nuint)ptr, alignment);
        public static nuint AlignDown(nuint ptr, nuint alignment)
        {
            Debug.Assert(BitOperations.PopCount(alignment) == 1);
            return ptr & ~alignment;
        }

        public static bool IsAligned(ulong ptr, ulong alignment)
        {
            Debug.Assert(BitOperations.PopCount(alignment) == 1);
            return (ptr & (alignment - 1)) == 0;
        }

        public static bool IsAligned<T>(T* ptr, T* alignment) where T : unmanaged => IsAligned((nuint)ptr, (nuint)alignment);
        public static bool IsAligned(nuint ptr, nuint alignment)
        {
            Debug.Assert(BitOperations.PopCount(alignment) == 1);
            return (ptr & (alignment - 1)) == 0;
        }
    }
}
