using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Common
{
    internal static class ArrayExtensions
    {
        public static ref T UnsafeIndex<T>(this T[] arr, int index)
            => ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(arr), index);
    }
}
