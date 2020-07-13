using System.Collections.Generic;
using Microsoft.Toolkit.HighPerformance.Extensions;
using Voltium.Core;
using Voltium.Core.Contexts;
using Voltium.Core.Memory;

namespace Voltium.RenderEngine
{
    internal struct TrackedResource
    {
        public const int NoWritePass = -1;

        public ResourceDesc Desc;

        public bool HasWritePass => LastWritePassIndex != NoWritePass;
        public bool HasReadPass => (LastReadPassIndices?.Count ?? 0) != 0;

        public int LastWritePassIndex;
        public List<int> LastReadPassIndices;

        public ResourceState CurrentTrackedState;

        public ResourceBarrier CreateTransition(ResourceState state, ResourceBarrierOptions options)
        {
            ResourceBarrier barrier;
            if (Desc.Type == ResourceType.Buffer)
            {
                barrier = ResourceBarrier.Transition(Desc.Buffer, CurrentTrackedState, state, options);
            }
            else
            {
                barrier = ResourceBarrier.Transition(Desc.Texture, CurrentTrackedState, state, options);
            }

            CurrentTrackedState = state;
            return barrier;
        }

        public ResourceBarrier CreateUav(ResourceBarrierOptions options)
        {
            if (Desc.Type == ResourceType.Buffer)
            {
                return ResourceBarrier.Uav(Desc.Buffer, options);
            }
            else
            {
                return ResourceBarrier.Uav(Desc.Texture, options);
            }
        }

        public void Allocate(GpuAllocator allocator)
        {
            if (Desc.Type == ResourceType.Buffer)
            {
                Desc.Buffer = allocator.AllocateBuffer(Desc.BufferDesc, Desc.MemoryAccess, Desc.InitialState);
            }
            else
            {
                Desc.Texture = allocator.AllocateTexture(Desc.TextureDesc, Desc.InitialState);
            }
        }

        internal struct ResourceDependency
        {
            public ResourceState RequiredState;
            public uint ResourceHandle;
        }
    }
}
