using System.Collections.Generic;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;

namespace Voltium.Core.Memory
{
    internal unsafe struct AllocatorHeap
    {
        public Heap Heap;
        public List<HeapBlock> FreeBlocks;
    }
}
