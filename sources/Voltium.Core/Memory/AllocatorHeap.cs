using System.Collections.Generic;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Memory
{
    internal unsafe struct AllocatorHeap
    {
        public ComPtr<ID3D12Heap> Heap;
        public List<HeapBlock> FreeBlocks;
    }

}
