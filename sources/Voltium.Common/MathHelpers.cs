using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Common
{
    internal unsafe static class MathHelpers
    {
        public static T* AlignUp<T>(T* ptr, nuint alignment) where T : unmanaged
               => (T*)AlignUp((nuint)ptr, alignment);

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
