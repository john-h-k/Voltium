using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Voltium.Common
{
    internal static class ListExtensions
    {
        public static ref T GetRef<T>(this List<T> list, int index) => ref CollectionsMarshal.AsSpan(list)[index];
    }
}
