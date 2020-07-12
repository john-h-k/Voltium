#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using Microsoft.Toolkit.HighPerformance.Extensions;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Contexts;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.RenderEngine;
using Voltium.RenderEngine.Passes;
using Voltium.RenderEngine.RenderGraph;

namespace Voltium.Core
{
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

        public RenderGraph(GraphicsDevice device)
        {
            _device = device;
        }

        public void AddPass<T>(T pass) where T : RenderPass
        {
            var passIndex = _renderPasses.Count;
            var builder = new RenderPassBuilder(this, passIndex, pass);

            pass.Register(ref builder);

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

        public void Schedule()
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
            return new ResourceHandle((uint)_resources.Count - 1);
        }

        internal ref TrackedResource GetResource(ResourceHandle handle) => ref ListExtensions.GetRef(_resources, (int)handle.Index);
        internal ref RenderPassBuilder GetRenderPass(int index) => ref ListExtensions.GetRef(_renderPasses, index);

        private struct GraphLayer
        {
            /// <summary> The barriers executed before this layer is executed </summary>
            public List<ResourceBarrier> Barriers;

            /// <summary> The indices in the GpuContext array that can be executed in any order </summary>
            public List<int> Passes;
        }

        public void ExecuteGraph()
        {
            Schedule();
            AllocateResources();
            BuildBarriers();
            RecordAndExecuteLayers();
        }

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
                    }
                }
                resource.Allocate(_device.Allocator);
            }
        }

        private void BuildBarriers()
        {
            foreach (ref var layer in _renderLayers.AsSpan())
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

        private void RecordAndExecuteLayers()
        {
            var resolver = new ComponentResolver(this);

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

                        compute.Record(ref ctx.AsMutable(), ref resolver);
                    }
                    else /* must be true */ if (pass is GraphicsRenderPass graphics)
                    {
                        using var ctx = _device.BeginGraphicsContext(graphics.DefaultPipelineState);

                        graphics.Record(ref ctx.AsMutable(), ref resolver);
                    }
                }
            }
        }
    }
}
