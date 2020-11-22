using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Voltium.Common
{
    internal static class ListExtensions
    {
        public static ref T GetRef<T>(this List<T> list, int index) => ref CollectionsMarshal.AsSpan(list)[index];
        public static ref T GetRefUnsafe<T>(this List<T> list, int index)
        {
#if DEBUG
            return ref GetRef(list, index);
#else
            return ref Unsafe.Add(ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(list)), index);
#endif
        }

        public static ref T GetPinnableReference<T>(this List<T>? list) => ref list.AsSpan().GetPinnableReference();

        public static Span<T> AsSpan<T>(this List<T>? list) => CollectionsMarshal.AsSpan(list);
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(this List<T>? list) => CollectionsMarshal.AsSpan(list);
    }
}
