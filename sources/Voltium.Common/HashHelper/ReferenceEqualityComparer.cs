using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Common.HashHelper
{
    internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals([AllowNull] object x, [AllowNull] object y) => ReferenceEquals(x, y);

        public int GetHashCode([DisallowNull] object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
