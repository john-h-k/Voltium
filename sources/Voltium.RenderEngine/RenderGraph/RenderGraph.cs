using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

        private struct FrameData
        {
            public List<RenderPassBuilder> RenderPasses;

            public GraphLayer[]? RenderLayers;

            public List<int> OutputPassIndices;

            public OutputDesc? PrimaryOutput;

            public List<int> InputPassIndices;
            public List<TrackedResource> Resources;
            public int MaxDepth;
            public Resolver Resolver;
        }

        private FrameData Frame;

        private FrameData LastFrame;

        

        private Dictionary<TextureDesc, (Texture Texture, ResourceState LastKnownState)> _cachedTextures = new();

        private static bool EnablePooling => true;

        /// <summary>
        /// Creates a new <see cref="RenderGraph"/>
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> used for rendering</param>
        public RenderGraph(GraphicsDevice device)
        {
            _device = device;

            // we don't actually have last frame so elide pointless copy
            MoveToNextGraphFrame(preserveLastFrame: false);
        }

        // convience methods

        /// <summary>
        /// Creates a new component and adds it to the graph
        /// </summary>
        /// <typeparam name="T0">The type of the component</typeparam>
        /// <param name="component0">The value of the component</param>
        public void CreateComponent<T0>(T0 component0)
            => Frame.Resolver.CreateComponent(component0);

        /// <summary>
        /// Creates new components and adds them to the graph
        /// </summary>
        public void CreateComponents<T0, T1>(T0 component0, T1 component1)
        {
            Frame.Resolver.CreateComponent(component0);
            Frame.Resolver.CreateComponent(component1);
        }

        /// <summary>
        /// Creates new components and adds them to the graph
        /// </summary>
        public void CreateComponents<T0, T1, T2>(T0 component0, T1 component1, T2 component2)
        {
            Frame.Resolver.CreateComponent(component0);
            Frame.Resolver.CreateComponent(component1);
            Frame.Resolver.CreateComponent(component2);
        }

        /// <summary>
        /// Creates new components and adds them to the graph
        /// </summary>
        public void CreateComponents<T0, T1, T2, T3>(T0 component0, T1 component1, T2 component2, T3 component3)
        {
            Frame.Resolver.CreateComponent(component0);
            Frame.Resolver.CreateComponent(component1);
            Frame.Resolver.CreateComponent(component2);
            Frame.Resolver.CreateComponent(component3);
        }

        /// <summary>
        /// Registers a pass into the graph, and calls the <see cref="RenderPass.Register(ref RenderPassBuilder, ref Resolver)"/> 
        /// method immediately to register all dependencies
        /// </summary>
        /// <typeparam name="T">The type of the pass</typeparam>
        /// <param name="pass">The pass</param>
        public void AddPass<T>(T pass) where T : RenderPass
        {
            var passIndex = Frame.RenderPasses.Count;
            var builder = new RenderPassBuilder(this, passIndex, pass);

            pass.Register(ref builder, ref Frame.Resolver);

            // anything with no dependencies is a top level input node implicity
            if ((builder.FrameDependencies?.Count ?? 0) == 0)
            {
                // the index _renderPasses.Add results in
                Frame.InputPassIndices.Add(passIndex);
            }

            if (builder.Depth > Frame.MaxDepth)
            {
                Frame.MaxDepth = builder.Depth;
            }

            // outputs are explicit
            if (pass.Output.Type != OutputClass.None)
            {
                if (pass.Output.Type == OutputClass.Primary)
                {
                    // can only have one primary output, but many secondaries
                    if (Frame.PrimaryOutput is not null)
                    {
                        ThrowHelper.ThrowInvalidOperationException("Cannot register a primary output pass as one has already been registered");
                    }
                    Frame.PrimaryOutput = pass.Output;
                }
                // the index _renderPasses.Add results in
                Frame.OutputPassIndices.Add(passIndex);
            }

            Frame.RenderPasses.Add(builder);
        }

        /// <summary>
        /// Executes the graph
        /// </summary>
        public void ExecuteGraph()
        {
            // false during the register passes
            Frame.Resolver.CanResolveResources = true;
            Schedule();
            AllocateResources();
            BuildBarriers();
            RecordAndExecuteLayers();

            // TODO make better
            DeallocateResources();

            MoveToNextGraphFrame(preserveLastFrame: true);
        }

        private void MoveToNextGraphFrame(bool preserveLastFrame)
        {
            if (preserveLastFrame)
            {
                LastFrame = Frame;
                LastFrame.Resources = null!;
            }

            Frame.Resolver = new Resolver(this);
            Frame.RenderPasses = new();
            Frame.RenderLayers = null;
            Frame.OutputPassIndices = new();
            Frame.Resources = new();
            Frame.InputPassIndices = new();
            Frame.PrimaryOutput = null;
            Frame.MaxDepth = default;
        }

        private void DeallocateResources()
        {
            foreach (ref var resource in Frame.Resources.AsSpan())
            {
                if (EnablePooling && ShouldTryPoolResource(ref resource))
                {
                    _cachedTextures[resource.Desc.TextureDesc] = (resource.Desc.Texture, resource.CurrentTrackedState);
                }
                else
                {
                    resource.Dispose();
                }
            }
        }

        private bool ShouldTryPoolResource(ref TrackedResource resource)
            => resource.Desc.Type == ResourceType.Texture && (resource.Desc.TextureDesc.ResourceFlags & (ResourceFlags.AllowDepthStencil | ResourceFlags.AllowRenderTarget)) != 0;

        private void Schedule()
        {
            Frame.RenderLayers = new GraphLayer[Frame.MaxDepth + 1];

            int i = 0;
            foreach (ref var pass in Frame.RenderPasses.AsSpan())
            {
                ref GraphLayer layer = ref Frame.RenderLayers[pass.Depth];

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

            Frame.Resources.Add(resource);
            return new ResourceHandle((uint)Frame.Resources.Count);
        }

        internal ref TrackedResource GetResource(ResourceHandle handle)
        {
            if (handle.IsInvalid)
            {
                ThrowHelper.ThrowInvalidOperationException("Resource was not created");
            }
            return ref ListExtensions.GetRef(Frame.Resources, (int)handle.Index - 1);
        }

        internal ref RenderPassBuilder GetRenderPass(int index) => ref ListExtensions.GetRef(Frame.RenderPasses, index);

        private struct GraphLayer
        {
            /// <summary> The barriers executed before this layer is executed </summary>
            public List<ResourceBarrier> Barriers;

            /// <summary> The indices in the GpuContext array that can be executed in any order </summary>
            public List<int> Passes;
        }

        private void AllocateResources()
        {
            foreach (ref var resource in Frame.Resources.AsSpan())
            {
                // handle relative sizes
                if (resource.Desc.OutputRelativeSize is double relative)
                {
                    if (Frame.PrimaryOutput is not OutputDesc primary)
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

                if (EnablePooling && resource.Desc.Type == ResourceType.Texture && _cachedTextures.Remove(resource.Desc.TextureDesc, out var pair))
                {
                    (resource.Desc.Texture, resource.CurrentTrackedState) = pair;

                    if (resource.CurrentTrackedState != resource.Desc.InitialState)
                    {
                        Frame.RenderLayers![0].Barriers ??= new();
                        Frame.RenderLayers![0].Barriers.Add(resource.CreateTransition(resource.Desc.InitialState, ResourceBarrierOptions.Full));
                    }
                }
                else
                {
                    resource.AllocateFrom(_device.Allocator);
                    resource.CurrentTrackedState = resource.Desc.InitialState;
                }

                resource.SetName();
            }
        }

        private void BuildBarriers()
        {
            foreach (ref var layer in Frame.RenderLayers.AsSpan())
            {
                foreach (var passIndex in layer.Passes.AsSpan())
                {
                    ref var pass = ref GetRenderPass(passIndex);

                    foreach (ref var transition in pass.Transitions.AsSpan())
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

        private List<GpuContext> _contexts = new();
        private void RecordAndExecuteLayers()
        {
            foreach (ref var layer in Frame.RenderLayers.AsSpan())
            {
                // TODO multithread

                var barriers = layer.Barriers.AsROSpan();

                using (var barrierCtx = _device.BeginGraphicsContext())
                {
                    barrierCtx.ResourceBarrier(barriers);
                    _contexts.Add(barrierCtx.AsMutable().AsGpuContext());
                }

                var numContexts = _contexts.Count + layer.Passes.Count;
                if (_contexts.Capacity < numContexts)
                {
                    _contexts.Capacity = numContexts;
                }
                foreach (var passIndex in layer.Passes)
                {
                    ref var pass = ref GetRenderPass(passIndex).Pass;

                    if (pass is ComputeRenderPass compute)
                    {
                        using var ctx = _device.BeginComputeContext(compute.DefaultPipelineState);
                        
                        compute.Record(ref ctx.AsMutable(), ref Frame.Resolver);
                        _contexts.Add(ctx.AsMutable().AsGpuContext());
                    }
                    else /* must be true */ if (pass is GraphicsRenderPass graphics)
                    {
                        using var ctx = _device.BeginGraphicsContext(graphics.DefaultPipelineState);

                        graphics.Record(ref ctx.AsMutable(), ref Frame.Resolver);
                        _contexts.Add(ctx.AsMutable().AsGpuContext());
                    }
                }
            }

            _device.Execute(_contexts.AsSpan());
            _contexts.Clear();
        }
    }
}
