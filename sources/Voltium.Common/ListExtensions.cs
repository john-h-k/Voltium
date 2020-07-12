using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Voltium.Common
{
    internal static class ListExtensions
    {
        public static ref T GetRef<T>(this List<T> list, int index) => ref CollectionsMarshal.AsSpan(list)[index];
        public static Span<T> AsSpan<T>(this List<T> list) => CollectionsMarshal.AsSpan(list);
        public static ReadOnlySpan<T> AsROSpan<T>(this List<T> list) => CollectionsMarshal.AsSpan(list);
    }
}
