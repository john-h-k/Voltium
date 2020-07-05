using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Voltium.Common.HashHelper
{
    internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals([AllowNull] object x, [AllowNull] object y) => ReferenceEquals(x, y);

        public int GetHashCode([DisallowNull] object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
