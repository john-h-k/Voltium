using System;
using System.IO;
using System.Threading.Tasks;

namespace Voltium.Core.Memory
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
