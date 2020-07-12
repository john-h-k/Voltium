#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using Microsoft.Toolkit.HighPerformance.Extensions;
using Voltium.Common;
using Voltium.Core.Contexts;
using Voltium.Core.Memory;
using Voltium.RenderEngine;
using Voltium.RenderEngine.Passes;

namespace Voltium.Core
{
    public struct RenderPassBuilder
    {
        private RenderGraph _graph;

        private int _passIndex;

        internal RenderPass Pass;
        internal int Depth;

        public RenderPassBuilder(RenderGraph graph, int passIndex, RenderPass pass) : this()
        {
            _graph = graph;
            _passIndex = passIndex;
            FrameDependencies = new();
            Transitions = new();
            Pass = pass;
        }

        internal List<int> FrameDependencies;
        internal List<(ResourceHandle Resource, ResourceState State, ResourceBarrierOptions Options)> Transitions;

        public void MarkUsage(BufferHandle buffer, ResourceState flags)
            => MarkUsage(buffer.AsResourceHandle(), flags);

        public void MarkUsage(TexHandle tex, ResourceState flags)
            => MarkUsage(tex.AsResourceHandle(), flags);

        private void AddDependencies(ReadOnlySpan<int> passIndices)
        {
            foreach (var passIndex in passIndices)
            {
                AddDependency(passIndex);
            }
        }
        private void AddDependency(int passIndex)
        {
            ref var pass = ref _graph.GetRenderPass(passIndex);

            if (pass.Depth >= Depth)
            {
                Depth = pass.Depth + 1;
            }

            FrameDependencies.Add(passIndex);
        }

        private void MarkUsage(ResourceHandle resource, ResourceState flags)
        {
            if (flags.IsInvalid())
            {
                ThrowHelper.ThrowArgumentException(nameof(flags), InvalidResourceStateFlags);
            }

            ref TrackedResource res = ref _graph.GetResource(resource);

            if (flags.HasWriteFlag())
            {
                // If we write to it, and it is read from earlier up, we need to depend on all the reading passes and the write pass
                if (res.HasReadPass)
                {
                    AddDependencies(res.LastReadPassIndices.AsSpan());
                }
                if (res.HasWritePass)
                {
                    AddDependency(res.LastWritePassIndex);
                }

                // If we write to resource, we need to mark that to the resource
                res.LastWritePassIndex = _passIndex;
                Transitions.Add((resource, flags, ResourceBarrierOptions.Full));
            }
            else if (flags.HasReadOnlyFlags())
            {
                // We also need to depend on any prior write passes but *not* any prior read passes
                if (res.HasWritePass)
                {
                    AddDependency(res.LastWritePassIndex);
                }

                // If we read from it, we need to mark that we do
                res.LastReadPassIndices.Add(_passIndex);

                Transitions.Add((resource, flags, ResourceBarrierOptions.Full));
            }
        }

        private const string InvalidResourceStateFlags = "ResourceStateFlags is invalid, which means it contains both write and read states, or multiple write states";

        public BufferHandle CreateBuffer(in BufferDesc desc, MemoryAccess memoryAccess, ResourceState initialState = ResourceState.CopyDestination)
            => _graph.AddResource(new ResourceDesc { Type = ResourceType.Buffer, BufferDesc = desc, MemoryAccess = memoryAccess, InitialState = initialState }, _passIndex).AsBufferHandle();

        public TexHandle CreateTexture(in TextureDesc desc, ResourceState initialState = ResourceState.CopyDestination)
            => _graph.AddResource(new ResourceDesc { Type = ResourceType.Texture, TextureDesc = desc, InitialState = initialState }, _passIndex).AsTextureHandle();


        public BufferHandle CreatePrimaryOutputRelativeBuffer(in BufferDesc desc, MemoryAccess memoryAccess, ResourceState initialState = ResourceState.CopyDestination, double outputRelativeSize = 1)
            => _graph.AddResource(new ResourceDesc { Type = ResourceType.Buffer, OutputRelativeSize = outputRelativeSize, BufferDesc = desc, MemoryAccess = memoryAccess, InitialState = initialState }, _passIndex).AsBufferHandle();


        public TexHandle CreatePrimaryOutputRelativeTexture(in TextureDesc desc, ResourceState initialState = ResourceState.CopyDestination, double outputRelativeSize = 1)
            => _graph.AddResource(new ResourceDesc { Type = ResourceType.Texture, OutputRelativeSize = outputRelativeSize, TextureDesc = desc, InitialState = initialState }, _passIndex).AsTextureHandle();
    }
}
