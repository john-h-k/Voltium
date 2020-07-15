using System;
using System.Collections.Generic;
using Microsoft.Toolkit.HighPerformance.Extensions;
using Voltium.Common;
using Voltium.Core;
using Voltium.Core.Contexts;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.RenderEngine
{
    /// <summary>
    /// A graph used to schedule and execute frames
    /// </summary>
    public unsafe sealed class RenderGraph
    {
        private GraphicsDevice _device;

        private List<RenderPassBuilder> _renderPasses = new();

        private GraphLayer[]? _renderLayers;

        private List<int> _outputPassIndices = new();

        private OutputDesc? _primaryOutput = null;

        private List<int> _inputPassIndices = new();
        private List<TrackedResource> _resources = new();
        private int _maxDepth = 0;
        private Resolver _resolver;

        /// <summary>
        /// Creates a new <see cref="RenderGraph"/>
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> used for rendering</param>
        public RenderGraph(GraphicsDevice device)
        {
            _device = device;
            _resolver = new Resolver(this);

            _index++;
        }

        // convience methods

        /// <summary>
        /// Creates a new component and adds it to the graph
        /// </summary>
        /// <typeparam name="T0">The type of the component</typeparam>
        /// <param name="component0">The value of the component</param>
        public void CreateComponent<T0>(T0 component0)
            => _resolver.CreateComponent(component0);

        /// <summary>
        /// Creates new components and adds them to the graph
        /// </summary>
        public void CreateComponents<T0, T1>(T0 component0, T1 component1)
        {
            _resolver.CreateComponent(component0);
            _resolver.CreateComponent(component1);
        }

        /// <summary>
        /// Creates new components and adds them to the graph
        /// </summary>
        public void CreateComponents<T0, T1, T2>(T0 component0, T1 component1, T2 component2)
        {
            _resolver.CreateComponent(component0);
            _resolver.CreateComponent(component1);
            _resolver.CreateComponent(component2);
        }

        /// <summary>
        /// Creates new components and adds them to the graph
        /// </summary>
        public void CreateComponents<T0, T1, T2, T3>(T0 component0, T1 component1, T2 component2, T3 component3)
        {
            _resolver.CreateComponent(component0);
            _resolver.CreateComponent(component1);
            _resolver.CreateComponent(component2);
            _resolver.CreateComponent(component3);
        }

        /// <summary>
        /// Registers a pass into the graph, and calls the <see cref="RenderPass.Register(ref RenderPassBuilder, ref Resolver)"/> 
        /// method immediately to register all dependencies
        /// </summary>
        /// <typeparam name="T">The type of the pass</typeparam>
        /// <param name="pass">The pass</param>
        public void AddPass<T>(T pass) where T : RenderPass
        {
            var passIndex = _renderPasses.Count;
            var builder = new RenderPassBuilder(this, passIndex, pass);

            pass.Register(ref builder, ref _resolver);

            // anything with no dependencies is a top level input node implicity
            if ((builder.FrameDependencies?.Count ?? 0) == 0)
            {
                // the index _renderPasses.Add results in
                _inputPassIndices.Add(passIndex);
            }

            if (builder.Depth > _maxDepth)
            {
                _maxDepth = builder.Depth;
            }

            // outputs are explicit
            if (pass.Output.Type != OutputClass.None)
            {
                if (pass.Output.Type == OutputClass.Primary)
                {
                    // can only have one primary output, but many secondaries
                    if (_primaryOutput is not null)
                    {
                        ThrowHelper.ThrowInvalidOperationException("Cannot register a primary output pass as one has already been registered");
                    }
                    _primaryOutput = pass.Output;
                }
                // the index _renderPasses.Add results in
                _outputPassIndices.Add(passIndex);
            }

            _renderPasses.Add(builder);
        }

        /// <summary>
        /// Executes the graph
        /// </summary>
        public void ExecuteGraph()
        {
            // false during the register passes
            _resolver.CanResolveResources = true;
            Schedule();
            AllocateResources();
            BuildBarriers();
            RecordAndExecuteLayers();

            // TODO make better
            DeallocateResources();
        }

        private void DeallocateResources()
        {
            foreach (ref var resource in _resources.AsSpan())
            {
                resource.Dispose();
            }
        }

        private void Schedule()
        {
            _renderLayers = new GraphLayer[_maxDepth + 1];

            int i = 0;
            foreach (ref var pass in _renderPasses.AsSpan())
            {
                ref GraphLayer layer = ref _renderLayers[pass.Depth];

                layer.Passes ??= new();

                layer.Passes.Add(i++);
            }
        }

        internal ResourceHandle AddResource(ResourceDesc desc, int callerPassIndex)
        {
            var resource = new TrackedResource
            {
                Desc = desc,
                LastReadPassIndices = new(),
                LastWritePassIndex = callerPassIndex
            };

            _resources.Add(resource);
            return new ResourceHandle((uint)_resources.Count);
        }

        internal ref TrackedResource GetResource(ResourceHandle handle)
        {
            if (handle.IsInvalid)
            {
                ThrowHelper.ThrowInvalidOperationException("Resource was not created");
            }
            return ref ListExtensions.GetRef(_resources, (int)handle.Index - 1);
        }

        internal ref RenderPassBuilder GetRenderPass(int index) => ref ListExtensions.GetRef(_renderPasses, index);

        private struct GraphLayer
        {
            /// <summary> The barriers executed before this layer is executed </summary>
            public List<ResourceBarrier> Barriers;

            /// <summary> The indices in the GpuContext array that can be executed in any order </summary>
            public List<int> Passes;
        }

        private static Dictionary<BufferDesc, Buffer> _buffers = new();
        private static Dictionary<TextureDesc, Texture> _textures = new();


        private static int _index = 0;

        private void AllocateResources()
        {
            foreach (ref var resource in _resources.AsSpan())
            {
                // handle relative sizes
                if (resource.Desc.OutputRelativeSize is double relative)
                {
                    if (_primaryOutput is not OutputDesc primary)
                    {
                        ThrowHelper.ThrowInvalidOperationException("Cannot use a primary output relative resource as no primary output was registered");
                        return;
                    }
;
                    if (resource.Desc.Type == ResourceType.Buffer)
                    {
                        resource.Desc.BufferDesc.Length = (long)(primary.BufferLength * relative);
                    }
                    else
                    {
                        switch (resource.Desc.TextureDesc.Dimension)
                        {
                            case TextureDimension.Tex3D:
                                resource.Desc.TextureDesc.DepthOrArraySize = (ushort)(primary.TextureDepthOrArraySize * relative);
                                goto case TextureDimension.Tex2D;

                            case TextureDimension.Tex2D:
                                resource.Desc.TextureDesc.Height = (uint)(primary.TextureHeight * relative);
                                goto case TextureDimension.Tex1D;

                            case TextureDimension.Tex1D:
                                resource.Desc.TextureDesc.Width = (ulong)(primary.TextureWidth * relative);
                                break;
                        }

                        // make sure no 0 height/depth
                        resource.Desc.TextureDesc.DepthOrArraySize = Math.Max((ushort)1U, resource.Desc.TextureDesc.DepthOrArraySize);
                        resource.Desc.TextureDesc.Height = Math.Max(1U, resource.Desc.TextureDesc.Height);
                    }
                }

                if (!(resource.Desc.Type == ResourceType.Buffer && _buffers.TryGetValue(resource.Desc.BufferDesc, out resource.Desc.Buffer))
                    && !(resource.Desc.Type == ResourceType.Texture && _textures.TryGetValue(resource.Desc.TextureDesc, out resource.Desc.Texture)))
                {
                    resource.AllocateFrom(_device.Allocator);

                    // TODO this isn't properly synced, need to only add them after they have finished being used
                }
            }
        }

        private void BuildBarriers()
        {
            foreach (ref var layer in _renderLayers.AsSpan())
            {
                foreach (var passIndex in layer.Passes.AsSpan())
                {
                    ref var pass = ref GetRenderPass(passIndex);

                    foreach   (ref var transition in pass.Transitions.AsSpan())
                    {
                        ref var resource = ref GetResource(transition.Resource);

                        layer.Barriers ??= new();

                        layer.Barriers.Add(resource.CreateTransition(transition.State, ResourceBarrierOptions.Full));

                        if (transition.State.HasUnorderedAccess())
                        {
                            layer.Barriers.Add(resource.CreateUav(ResourceBarrierOptions.Full));
                        }
                    }
                }
            }
        }

        private void RecordAndExecuteLayers()
        {
            foreach (ref var layer in _renderLayers.AsSpan())
            {
                // TODO multithread

                var barriers = layer.Barriers.AsROSpan();

                using (var barrierCtx = _device.BeginCopyContext())
                {
                    barrierCtx.ResourceBarrier(barriers);
                }

                foreach (var passIndex in layer.Passes)
                {
                    ref var pass = ref GetRenderPass(passIndex).Pass;

                    if (pass is ComputeRenderPass compute)
                    {
                        using var ctx = _device.BeginComputeContext(compute.DefaultPipelineState);
                        
                        compute.Record(ref ctx.AsMutable(), ref _resolver);
                    }
                    else /* must be true */ if (pass is GraphicsRenderPass graphics)
                    {
                        using var ctx = _device.BeginGraphicsContext(graphics.DefaultPipelineState);

                        graphics.Record(ref ctx.AsMutable(), ref _resolver);
                    }
                }
            }
        }
    }
}
