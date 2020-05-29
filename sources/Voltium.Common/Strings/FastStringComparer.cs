using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Common.Strings
{
    internal sealed class FastStringComparer : IEqualityComparer<string>
    {
        public bool Equals([AllowNull] string x, [AllowNull] string y)
        {
            return string.Equals(x, y);
        }

        public int GetHashCode([DisallowNull] string obj)
        {
            return StringHelper.FastHash(obj);
        }
    }
}
