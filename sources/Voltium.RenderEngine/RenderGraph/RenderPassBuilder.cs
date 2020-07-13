using System;
using System.Collections.Generic;
using Microsoft.Toolkit.HighPerformance.Extensions;
using Voltium.Common;
using Voltium.Core;
using Voltium.Core.Contexts;
using Voltium.Core.Memory;
using Voltium.RenderEngine;
using Voltium.RenderEngine.Passes;

using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.RenderEngine
{
    /// <summary>
    /// The type used in a <see cref="RenderGraph"/> to register pass dependencies and resources
    /// </summary>
    public struct RenderPassBuilder
    {
        private RenderGraph _graph;

        private int _passIndex;

        internal RenderPass Pass;
        internal int Depth;


        internal RenderPassBuilder(RenderGraph graph, int passIndex, RenderPass pass) : this()
        {
            _graph = graph;
            _passIndex = passIndex;
            FrameDependencies = new();
            Transitions = new();
            Pass = pass;
        }

        internal List<int> FrameDependencies;
        internal List<(ResourceHandle Resource, ResourceState State, ResourceBarrierOptions Options)> Transitions;

        /// <summary>
        /// Indicates a pass uses a resource in a certain manner
        /// </summary>
        /// <param name="buffer">The resource handle</param>
        /// <param name="flags">The <see cref="ResourceState"/> the pass uses it as</param>
        public void MarkUsage(BufferHandle buffer, ResourceState flags)
            => MarkUsage(buffer.AsResourceHandle(), flags);

        /// <summary>
        /// Indicates a pass uses a resource in a certain manner
        /// </summary>
        /// <param name="texture">The resource handle</param>
        /// <param name="flags">The <see cref="ResourceState"/> the pass uses it as</param>
        public void MarkUsage(TextureHandle texture, ResourceState flags)
            => MarkUsage(texture.AsResourceHandle(), flags);

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

        /// <summary>
        /// Indicates a pass creates a new buffer
        /// </summary>
        /// <param name="desc">The <see cref="BufferDesc"/> describing the pass's buffer</param>
        /// <param name="memoryAccess">The <see cref="MemoryAccess"/> the buffer will be allocated as</param>
        /// <param name="initialState">The initial <see cref="ResourceState"/> of the resource</param>
        /// <returns>A new <see cref="BufferHandle"/> representing the resource that can be later resolved to a <see cref="Buffer"/></returns>
        public BufferHandle CreateBuffer(in BufferDesc desc, MemoryAccess memoryAccess, ResourceState initialState = ResourceState.CopyDestination)
            => _graph.AddResource(new ResourceDesc { Type = ResourceType.Buffer, BufferDesc = desc, MemoryAccess = memoryAccess, InitialState = initialState }, _passIndex).AsBufferHandle();

        /// <summary>
        /// Indicates a pass creates a new texture
        /// </summary>
        /// <param name="desc">The <see cref="TextureDesc"/> describing the pass's buffer</param>
        /// <param name="initialState">The initial <see cref="ResourceState"/> of the resource</param>
        /// <returns>A new <see cref="TextureHandle"/> representing the resource that can be later resolved to a <see cref="Buffer"/></returns>
        public TextureHandle CreateTexture(in TextureDesc desc, ResourceState initialState = ResourceState.CopyDestination)
            => _graph.AddResource(new ResourceDesc { Type = ResourceType.Texture, TextureDesc = desc, InitialState = initialState }, _passIndex).AsTextureHandle();

        /// <summary>
        /// Indicates a pass creates a new buffer that is relative to the size of the primary output. This is only valid
        /// if the primary output is a buffer
        /// </summary>
        /// <param name="desc">The <see cref="BufferDesc"/> describing the pass's buffer</param>
        /// <param name="memoryAccess">The <see cref="MemoryAccess"/> the buffer will be allocated as</param>
        /// <param name="initialState">The initial <see cref="ResourceState"/> of the resource</param>
        /// <param name="outputRelativeSize">Optionally, the multiplier that specifies how to multiply the size of this resource relative to the primary output.
        /// By default, this is 1 - the resource is the same size as the primary output</param>
        /// <returns>A new <see cref="BufferHandle"/> representing the resource that can be later resolved to a <see cref="Buffer"/></returns>

        public BufferHandle CreatePrimaryOutputRelativeBuffer(in BufferDesc desc, MemoryAccess memoryAccess, ResourceState initialState = ResourceState.CopyDestination, double outputRelativeSize = 1)
            => _graph.AddResource(new ResourceDesc { Type = ResourceType.Buffer, OutputRelativeSize = outputRelativeSize, BufferDesc = desc, MemoryAccess = memoryAccess, InitialState = initialState }, _passIndex).AsBufferHandle();


        /// <summary>
        /// Indicates a pass creates a new texture that is relative to the size of the primary output. This is only valid
        /// if the primary output is a texture
        /// </summary>
        /// <param name="desc">The <see cref="TextureDesc"/> describing the pass's buffer</param>
        /// <param name="initialState">The initial <see cref="ResourceState"/> of the resource</param>
        /// <param name="outputRelativeSize">Optionally, the multiplier that specifies how to multiply the size of this resource relative to the primary output.
        /// By default, this is 1 - the resource is the same size as the primary output</param>
        /// <returns>A new <see cref="TextureHandle"/> representing the resource that can be later resolved to a <see cref="Buffer"/></returns>

        public TextureHandle CreatePrimaryOutputRelativeTexture(in TextureDesc desc, ResourceState initialState = ResourceState.CopyDestination, double outputRelativeSize = 1)
            => _graph.AddResource(new ResourceDesc { Type = ResourceType.Texture, OutputRelativeSize = outputRelativeSize, TextureDesc = desc, InitialState = initialState }, _passIndex).AsTextureHandle();
    }
}
