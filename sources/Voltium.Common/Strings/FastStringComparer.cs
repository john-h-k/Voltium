using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Voltium.Common.Strings
{
    internal sealed class FastStringComparer : IEqualityComparer<string>
    {
        public bool Equals(string? x, string? y)
        {
            return string.Equals(x, y);
        }

        public int GetHashCode(string obj)
        {
            return StringHelper.FastHash(obj);
        }
    }
}
