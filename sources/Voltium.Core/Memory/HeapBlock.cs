using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Voltium.Common;

namespace Voltium.Core.Memory
{
    [GenerateEquality]
    internal partial struct HeapBlock : IEquatable<HeapBlock>, IComparable<HeapBlock>
    {
        public ulong Offset;
        public ulong Size;

        public int CompareTo(HeapBlock other)
        {
            // SortedSet<T> considers elements that CompareTo 0 to be the same (why??!?!) so we have to avoid that
            var sizeComp = Size.CompareTo(other.Size);
            return sizeComp == 0 ? (Equals(other) ? 0 : 1) : sizeComp;
        }
    }

}
