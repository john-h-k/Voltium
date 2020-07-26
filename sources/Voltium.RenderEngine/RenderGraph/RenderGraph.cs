using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

        private struct GpuTaskBuffer8
        {
            public GpuTask E0;
            public GpuTask E1;
            public GpuTask E2;
            public GpuTask E3;
            public GpuTask E4;
            public GpuTask E5;
            public GpuTask E6;
            public GpuTask E7;

            public ref GpuTask this[uint index] => ref Unsafe.Add(ref GetPinnableReference(), (int)index);
            public ref GpuTask GetPinnableReference() => ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref E0, 0));
        }

        private uint _maxFrameLatency;
        private uint _frameIndex;
        private GpuTaskBuffer8 _frames;

        private FrameData _frame;

        private FrameData _lastFrame;

        

        private Dictionary<TextureDesc, (Texture Texture, ResourceState LastKnownState)> _cachedTextures = new();

        private static bool EnablePooling => true;

        /// <summary>
        /// Creates a new <see cref="RenderGraph"/>
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> used for rendering</param>
        /// <param name="maxFrameLatency">The maximum number of frames that can be enqueued to the </param>
        public RenderGraph(GraphicsDevice device, uint maxFrameLatency)
        {
            _device = device;
            _maxFrameLatency = maxFrameLatency;

            if (maxFrameLatency > 8)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(maxFrameLatency), "8 is the maximum allowed maxFrameLatency");
            }

            // we don't actually have last frame so elide pointless copy
            MoveToNextGraphFrame(GpuTask.Completed, preserveLastFrame: false);
        }

        // convience methods

        /// <summary>
        /// Creates a new component and adds it to the graph
        /// </summary>
        /// <typeparam name="T0">The type of the component</typeparam>
        /// <param name="component0">The value of the component</param>
        public void CreateComponent<T0>(T0 component0)
            => _frame.Resolver.CreateComponent(component0);

        /// <summary>
        /// Creates new components and adds them to the graph
        /// </summary>
        public void CreateComponents<T0, T1>(T0 component0, T1 component1)
        {
            _frame.Resolver.CreateComponent(component0);
            _frame.Resolver.CreateComponent(component1);
        }

        /// <summary>
        /// Creates new components and adds them to the graph
        /// </summary>
        public void CreateComponents<T0, T1, T2>(T0 component0, T1 component1, T2 component2)
        {
            _frame.Resolver.CreateComponent(component0);
            _frame.Resolver.CreateComponent(component1);
            _frame.Resolver.CreateComponent(component2);
        }

        /// <summary>
        /// Creates new components and adds them to the graph
        /// </summary>
        public void CreateComponents<T0, T1, T2, T3>(T0 component0, T1 component1, T2 component2, T3 component3)
        {
            _frame.Resolver.CreateComponent(component0);
            _frame.Resolver.CreateComponent(component1);
            _frame.Resolver.CreateComponent(component2);
            _frame.Resolver.CreateComponent(component3);
        }

        /// <summary>
        /// Registers a pass into the graph, and calls the <see cref="RenderPass.Register(ref RenderPassBuilder, ref Resolver)"/> 
        /// method immediately to register all dependencies
        /// </summary>
        /// <typeparam name="T">The type of the pass</typeparam>
        /// <param name="pass">The pass</param>
        public void AddPass<T>(T pass) where T : RenderPass
        {
            var passIndex = _frame.RenderPasses.Count;
            var builder = new RenderPassBuilder(this, passIndex, pass);

            pass.Register(ref builder, ref _frame.Resolver);

            // anything with no dependencies is a top level input node implicity
            if ((builder.FrameDependencies?.Count ?? 0) == 0)
            {
                // the index _renderPasses.Add results in
                _frame.InputPassIndices.Add(passIndex);
            }

            if (builder.Depth > _frame.MaxDepth)
            {
                _frame.MaxDepth = builder.Depth;
            }

            // outputs are explicit
            if (pass.Output.Type != OutputClass.None)
            {
                if (pass.Output.Type == OutputClass.Primary)
                {
                    // can only have one primary output, but many secondaries
                    if (_frame.PrimaryOutput is not null)
                    {
                        ThrowHelper.ThrowInvalidOperationException("Cannot register a primary output pass as one has already been registered");
                    }
                    _frame.PrimaryOutput = pass.Output;
                }
                // the index _renderPasses.Add results in
                _frame.OutputPassIndices.Add(passIndex);
            }

            _frame.RenderPasses.Add(builder);
        }

        /// <summary>
        /// Executes the graph
        /// </summary>
        public void ExecuteGraph()
        {
            // false during the register passes
            _frame.Resolver.CanResolveResources = true;
            Schedule();
            AllocateResources();
            BuildBarriers();
            var task = RecordAndExecuteLayers();

            // TODO make better
            DeallocateResources();

            MoveToNextGraphFrame(task, preserveLastFrame: true);
        }

        private void MoveToNextGraphFrame(GpuTask task, bool preserveLastFrame)
        {
            // won't block if frame is completed
            _frames[_frameIndex].Block();
            _frames[_frameIndex] = task;

            if (_frameIndex == 0)
            {
                _device.ResetRenderTargetViewHeap();
                _device.ResetDepthStencilViewHeap();
            }
            _frameIndex = (_frameIndex + 1) % _maxFrameLatency;

            if (preserveLastFrame)
            {
                _lastFrame = _frame;
                _lastFrame.Resources = null!;
            }

            _frame.Resolver = new Resolver(this);
            _frame.RenderPasses = new();
            _frame.RenderLayers = null;
            _frame.OutputPassIndices = new();
            _frame.Resources = new();
            _frame.InputPassIndices = new();
            _frame.PrimaryOutput = null;
            _frame.MaxDepth = default;
        }

        private void DeallocateResources()
        {
            foreach (ref var resource in _frame.Resources.AsSpan())
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
            _frame.RenderLayers = new GraphLayer[_frame.MaxDepth + 1];

            int i = 0;
            foreach (ref var pass in _frame.RenderPasses.AsSpan())
            {
                ref GraphLayer layer = ref _frame.RenderLayers[pass.Depth];

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

            _frame.Resources.Add(resource);
            return new ResourceHandle((uint)_frame.Resources.Count);
        }

        internal ref TrackedResource GetResource(ResourceHandle handle)
        {
            if (handle.IsInvalid)
            {
                ThrowHelper.ThrowInvalidOperationException("Resource was not created");
            }
            return ref ListExtensions.GetRef(_frame.Resources, (int)handle.Index - 1);
        }

        internal ref RenderPassBuilder GetRenderPass(int index) => ref ListExtensions.GetRef(_frame.RenderPasses, index);

        private struct GraphLayer
        {
            /// <summary> The barriers executed before this layer is executed </summary>
            public List<ResourceBarrier> Barriers;

            /// <summary> The indices in the GpuContext array that can be executed in any order </summary>
            public List<int> Passes;
        }

        private void AllocateResources()
        {
            foreach (ref var resource in _frame.Resources.AsSpan())
            {
                // handle relative sizes
                if (resource.Desc.OutputRelativeSize is double relative)
                {
                    if (_frame.PrimaryOutput is not OutputDesc primary)
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
                        _frame.RenderLayers![0].Barriers ??= new();
                        _frame.RenderLayers![0].Barriers.Add(resource.CreateTransition(resource.Desc.InitialState, ResourceBarrierOptions.Full));
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
            foreach (ref var layer in _frame.RenderLayers.AsSpan())
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
        private GpuTask RecordAndExecuteLayers()
        {
            foreach (ref var layer in _frame.RenderLayers.AsSpan())
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

                        compute.Record(ref ctx.AsMutable(), ref _frame.Resolver);
                        _contexts.Add(ctx.AsMutable().AsGpuContext());
                    }
                    else /* must be true */ if (pass is GraphicsRenderPass graphics)
                    {
                        using var ctx = _device.BeginGraphicsContext(graphics.DefaultPipelineState);

                        graphics.Record(ref ctx.AsMutable(), ref _frame.Resolver);
                        _contexts.Add(ctx.AsMutable().AsGpuContext());
                    }
                    else
                    {
                        ThrowHelper.ThrowArgumentException("what the fuck have you done");
                    }
                }
            }

            var task = _device.Execute(_contexts.AsSpan());
            _contexts.Clear();
            return task;
        }
    }
}
