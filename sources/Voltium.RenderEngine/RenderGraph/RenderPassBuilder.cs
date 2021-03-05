using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Toolkit.HighPerformance.Extensions;
using Voltium.Common;
using Voltium.Core;
using Voltium.Core.Configuration.Graphics;
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

        internal RenderGraph Graph => _graph; // we use this to avoid capturing in the Execute method where we sort passes

        private int _passIndex;

        internal int Depth;

        internal RenderPassBuilder(RenderGraph graph, int passIndex) : this()
        {
            _graph = graph;
            _passIndex = passIndex;
            FrameDependencies = new();
            Transitions = new();
        }

        internal ValueList<int> FrameDependencies;
        internal ValueList<(ResourceHandle Resource, ResourceState State)> Transitions;

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
            }

            Transitions.Add((resource, flags));
        }

        private const string InvalidResourceStateFlags = "ResourceStateFlags is invalid, which means it contains both write and read states, or multiple write states";

        // TODO: Allow named transients (currently we use null names to mark transients)

        internal const int PersistentResourceMask = int.MinValue;
        private static void ThrowForUnnamedPersistent(string name)
        {
            if (name is string { Length: > 0 })
            {
                return;
            }
            // We specifically validate this, because not doing so would cause the graph to think this resource is transient
            // And shit would explode (use-after-free on the resource likely. or weird unclear "resource dispose" errors)
            ThrowHelper.ThrowArgumentNullException(nameof(name), "Persistent resources must have a name");
        }

        /// <summary>
        /// Indicates a pass creates a new buffer
        /// </summary>
        /// <param name="desc">The <see cref="BufferDesc"/> describing the pass's buffer</param>
        /// <param name="memoryAccess">The <see cref="MemoryAccess"/> the buffer will be allocated as</param>
        /// <param name="debugName">The <see cref="string"/> to set the resource name to in debug mode</param>
        /// <returns>A new <see cref="BufferHandle"/> representing the resource that can be later resolved to a <see cref="Buffer"/></returns>
        public BufferHandle CreateBuffer(in BufferDesc desc, MemoryAccess memoryAccess, string? debugName = null)
            => _graph.AddResource(new ResourceDesc { Type = ResourceType.Buffer, BufferDesc = desc, MemoryAccess = memoryAccess, InitialState = ResourceState.Common, DebugName = debugName }, _passIndex).AsBufferHandle();

        /// <summary>
        /// Indicates a pass creates a new texture9
        /// </summary>
        /// <param name="desc">The <see cref="TextureDesc"/> describing the pass's buffer</param>
        /// <param name="initialState">The initial <see cref="ResourceState"/> of the resource</param>
        /// <param name="debugName">The <see cref="string"/> to set the resource name to in debug mode</param>
        /// <returns>A new <see cref="TextureHandle"/> representing the resource that can be later resolved to a <see cref="Buffer"/></returns>
        public TextureHandle CreateTexture(in TextureDesc desc, ResourceState initialState = ResourceState.CopyDestination, string? debugName = null)
            => _graph.AddResource(new ResourceDesc { Type = ResourceType.Texture, TextureDesc = desc, InitialState = initialState, DebugName = debugName }, _passIndex).AsTextureHandle();

    }

    enum ResourceUsage
    {
        /// <summary>
        /// The application only writes to the resource, and so its results can be discarded each frame
        /// </summary>
        Discard,

        /// <summary>
        /// The resource persists across frames, but there is a different copy of the resource for each frame index, so that
        /// the application can safely write to it with multiple latent frames
        /// </summary>
        Persistent,

        /// <summary>
        /// The resource persists across frames, and there is only a single copy of it provided to all frames. Use with caution, to prevent
        /// overwriting of data being used by the GPU in prior latent frames
        /// </summary>
        PersistentPerFrame
    }
}
