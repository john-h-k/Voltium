using System;
using System.Collections.Generic;
using Microsoft.Toolkit.HighPerformance.Extensions;
using Voltium.Common;
using Voltium.Core;
using Voltium.Core.Contexts;
using Voltium.Core.Memory;

namespace Voltium.RenderEngine
{
    internal struct TrackedResource : IDisposable
    {
        public const int NoWritePass = -1;

        public ResourceDesc Desc;

        public bool HasWritePass => LastWritePassIndex != NoWritePass;
        public bool HasReadPass => (LastReadPassIndices?.Count ?? 0) != 0;

        public int LastWritePassIndex;
        public List<int> LastReadPassIndices;

        public ResourceState CurrentTrackedState;

        public void AllocateFrom(GraphicsAllocator allocator)
        {
            if (Desc.Type == ResourceType.Buffer)
            {
                Desc.Buffer = allocator.AllocateBuffer(Desc.BufferDesc, Desc.MemoryAccess);
            }
            else
            {
                Desc.Texture = allocator.AllocateTexture(Desc.TextureDesc, Desc.InitialState);
            }
        }

        public void Dispose()
        {
            if (Desc.Type == ResourceType.Buffer)
            {
                Desc.Buffer.Dispose();
            }
            else
            {
                Desc.Texture.Dispose();
            }
        }

        internal struct ResourceDependency
        {
            public ResourceState RequiredState;
            public uint ResourceHandle;
        }

        internal void SetName()
        {
            if (Desc.DebugName is null)
            {
                return;
            }

            if (Desc.Type == ResourceType.Buffer)
            {
                //Desc.Buffer.SetName(Desc.DebugName);
            }
            else
            {
                //Desc.Texture.SetName(Desc.DebugName);
            }
        }
    }
}
