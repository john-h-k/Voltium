using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Common
{
    internal static class CollectionExtensions
    {
        public static ref T GetRef<T>(this List<T> list, int index) => ref CollectionsMarshal.AsSpan(list)[index];
    }
}
