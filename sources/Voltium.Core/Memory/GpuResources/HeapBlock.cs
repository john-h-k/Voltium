using System;
using System.Diagnostics.CodeAnalysis;

namespace Voltium.Core.GpuResources
{
    internal struct HeapBlock : IComparable<HeapBlock>
    {
        public ulong Offset;
        public ulong Size;

        public int CompareTo(HeapBlock other)
        {
            return Size.CompareTo(Size);
        }
    }

}
