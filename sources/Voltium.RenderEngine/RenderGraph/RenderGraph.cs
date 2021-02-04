using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Collections.Extensions;
using Microsoft.Toolkit.HighPerformance.Extensions;
using Microsoft.Toolkit.HighPerformance.Helpers;
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
    public unsafe sealed partial class RenderGraph
    {
        private GraphicsDevice _device;

        private DescriptorHeap _transientRtvs = null!;
        private DescriptorHeap _transientDsvs = null!;

        private DictionarySlim<RenderPass, PassHeuristics> _heuristics = new();

        private struct FrameData
        {
            public List<RenderPassBuilder> RenderPasses;

            public GraphLayer[]? RenderLayers;

            public List<int> OutputPassIndices;

            public OutputDesc? PrimaryOutput;

            public List<int> InputPassIndices;
            public List<TrackedResource> Resources;
            public List<TrackedResource> PersistentResources;
            public int MaxDepth;
            public int NumBarrierLists;
            public Resolver Resolver;
        }

        [FixedBufferType(typeof(GpuTask), 8)]
        private partial struct GpuTaskBuffer8 { }

        private uint _maxFrameLatency;
        private uint _frameIndex;
        private GpuTaskBuffer8 _frames;

        private FrameData _frame;
        private FrameData _lastFrame;

        private Dictionary<TextureDesc, (Texture Texture, ResourceState LastKnownState)> _cachedTextures = new();

        private List<object?> _outputs = new();

        private static bool EnablePooling => false;

        /// <summary>
        /// Creates a new <see cref="RenderGraph"/>
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/> used for rendering</param>
        /// <param name="maxFrameLatency">The maximum number of frames that can be enqueued to the </param>
        public RenderGraph(GraphicsDevice device, uint maxFrameLatency)
        {
            _device = device;
            _maxFrameLatency = maxFrameLatency;

            if (maxFrameLatency > GpuTaskBuffer8.BufferLength)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(maxFrameLatency), $"{GpuTaskBuffer8.BufferLength} is the maximum allowed maxFrameLatency");
            }

            _lastFrame.Resolver = new Resolver(this);
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

        internal void SetOutput(int passIndex, object? val) => _outputs[passIndex] = val;
        internal object? GetInput(int passIndex)
        {
            if (passIndex == 0)
            {
                ThrowHelper.ThrowInvalidOperationException("First pass doesn't have an input");
            }

            return _outputs[passIndex - 1];
        }

        internal T GetInputAs<T>(int passIndex)
        {
            var input = GetInput(passIndex);
            if (input is T t)
            {
                return t;
            }

            return ThrowHelper.ThrowInvalidOperationException<T>(GetMessage(input));

            static string GetMessage(object? o) => $"Tried to retrieve a pass input with type '{typeof(T).Name}', but pass input was {NullOrType(o)}";
            static string NullOrType(object? o) => o is null ? "null" : $"of type '{o.GetType().Name}'";
        }

        /// <summary>
        /// Registers a pass into the graph, and calls the <see cref="RenderPass.Register(ref RenderPassBuilder, ref Resolver)"/> 
        /// method immediately to register all dependencies
        /// </summary>
        /// <param name="pass">The pass</param>
        public void AddPass(RenderPass pass)
        {
            var passIndex = _frame.RenderPasses.Count;
            var builder = new RenderPassBuilder(this, passIndex, pass);
            _outputs.Add(null);

            // Register returning false means "discard pass from graph"
            if (!pass.Register(ref builder, ref _frame.Resolver))
            {
                return;
            }

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
            Record();
            var task = Execute();

            // TODO make better
            DeallocateResources(task);

            MoveToNextGraphFrame(task, preserveLastFrame: true);
        }

        private void MoveToNextGraphFrame(in GpuTask task, bool preserveLastFrame)
        {
            // won't block if frame is completed
            _frames[_frameIndex].Block();
            _frames[_frameIndex] = task;

            _frameIndex = (_frameIndex + 1) % _maxFrameLatency;

            if (preserveLastFrame)
            {
                _lastFrame = _frame;
                _lastFrame.Resources = null!;
            }

            //_frame.Resolver = new Resolver(this);
            _frame.Resolver = _lastFrame.Resolver;
            _frame.RenderPasses = new();
            _frame.RenderLayers = null;
            _frame.OutputPassIndices = new();
            _frame.Resources = new();
            _frame.InputPassIndices = new();
            _frame.PrimaryOutput = null;
            _frame.MaxDepth = default;
            _frame.NumBarrierLists = 0;
        }

        private void DeallocateResources(in GpuTask frame)
        {
            foreach (ref var resource in _frame.Resources.AsSpan())
            {
                if (EnablePooling && ShouldTryPoolResource(ref resource))
                {
                    _cachedTextures[resource.Desc.TextureDesc] = (resource.Desc.Texture, resource.CurrentTrackedState);
                }
                else
                {
                    resource.Dispose(frame);
                }
            }
        }

        private bool ShouldTryPoolResource(ref TrackedResource resource)
            => resource.Desc.Type == ResourceType.Texture && (resource.Desc.TextureDesc.ResourceFlags & (ResourceFlags.AllowDepthStencil | ResourceFlags.AllowRenderTarget)) != 0;

        private void Schedule()
        {
            _frame.RenderLayers = new GraphLayer[_frame.MaxDepth + 1];

            int i = 0;
            bool hasBarriers = false;
            foreach (ref var pass in _frame.RenderPasses.AsSpan())
            {
                ref GraphLayer layer = ref _frame.RenderLayers[pass.Depth];

                if (pass.Transitions?.Count is not (null or 0))
                {
                    hasBarriers = true;
                }

                layer.Passes ??= new();

                layer.Passes.Add(i++);
            }

            for (int j = 1; j < _frame.RenderLayers.Length; j++)
            {
                ref var layer = ref _frame.RenderLayers[j];
                ref var prevLayer = ref _frame.RenderLayers[j - 1];
                layer.NumPreviousPasses = prevLayer.NumPreviousPasses + prevLayer.Passes.Count + (hasBarriers ? 1 : 0);
            }
        }

        internal ResourceHandle AddResource(in ResourceDesc desc, int callerPassIndex)
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


        internal ResourceHandle AddPersitentResource(string name, in ResourceDesc desc, int callerPassIndex)
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
            return ref Common.ListExtensions.GetRef(_frame.Resources, (int)handle.Index - 1);
        }

        internal ref RenderPassBuilder GetRenderPass(int index) => ref Common.ListExtensions.GetRef(_frame.RenderPasses, index);

        private struct GraphLayer
        {
            /// <summary> The barriers executed before this layer is executed </summary>
            public List<ResourceBarrier> Barriers;

            /// <summary> The indices in the GpuContext array that can be executed in any order </summary>
            public List<int> Passes;


            public int NumPreviousPasses;
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

                        if (resource.Desc.Type == ResourceType.Texture && resource.Desc.Texture.Format == DataFormat.Unknown)
                        {
                            resource.Desc.TextureDesc.Format = primary.Format;
                        }
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

                    if (pass.Transitions.Count != 0)
                    {
                        _frame.NumBarrierLists++;
                    }

                    foreach (ref var transition in pass.Transitions.AsSpan())
                    {
                        ref var resource = ref GetResource(transition.Resource);

                        layer.Barriers ??= new();

                        // We try and add the state, which works if it is just another read state. Else add new barrier
                        if (layer.Barriers.Count == 0 || !layer.Barriers[^1].TryAddState(transition.State))
                        {
                            layer.Barriers.Add(resource.CreateTransition(transition.State, ResourceBarrierOptions.Full));
                        }
                        if (transition.State.HasUnorderedAccess())
                        {
                            layer.Barriers.Add(resource.CreateUav(ResourceBarrierOptions.Full));
                        }
                    }
                }
            }
        }

        private void Record()
        {
            RecordWithoutHeuristics();
        }


        private void RecordWithHeuristics()
        {
#pragma warning disable CS0162 // Unreachable code detected
            _frame.RenderPasses.AsSpan().Sort(static (a, b) =>
#pragma warning restore CS0162 // Unreachable code detected
            {
                Debug.Assert(a.Graph == b.Graph);
                ref PassHeuristics heuristicsA = ref a.Graph._heuristics.GetOrAddValueRef(a.Pass);
                ref PassHeuristics heuristicsB = ref b.Graph._heuristics.GetOrAddValueRef(b.Pass);
                return heuristicsA.CompareTo(heuristicsB);
            });


            using var tasks = RentedArray<Task>.Create(Environment.ProcessorCount);

            for (int i = 0, offset = 0; i < _frame.RenderPasses.Count + Environment.ProcessorCount - 1; i += Environment.ProcessorCount, offset++)
            {
                tasks.Value[offset] = Task.Run(() =>
                {
                    for (var j = 0; j < Environment.ProcessorCount; j++)
                    {
                        var index = (j * Environment.ProcessorCount) + i;
                        if (index >= _frame.RenderPasses.Count)
                        {
                            return;
                        }

                        ref var pass = ref _frame.RenderPasses.AsSpan()[index];

                        ref var heuristics = ref _heuristics.GetOrAddValueRef(pass.Pass);

                        double start = Stopwatch.GetTimestamp(), end = 0;

                        RecordPass(ref pass);

                        end = Stopwatch.GetTimestamp();

                        var recordLength = TimeSpan.FromMilliseconds(Math.Max((end - start), 0) / Stopwatch.Frequency);

                        heuristics.PassExecutionCount++;
                        heuristics.LastPassRecordTime = recordLength;
                    }
                });
            }

            Task.WhenAll(tasks.Value).RunSynchronously();
        }

        private struct PassExecution : IAction
        {
            public void Invoke(int i)
            {

            }
        }

        private void RecordWithoutHeuristics()
        {
            var passes = _frame.RenderPasses.AsSpan();
            foreach (ref var pass in passes)
            {
                RecordPass(ref pass);
            }
        }

        private void RecordPass(ref RenderPassBuilder pass)
        {
            if (pass.Pass is ComputeRenderPass compute)
            {
                using var ctx = _device.BeginComputeContext(compute.DefaultPipelineState);

                compute.Record(ctx, ref _frame.Resolver);
                pass.Context = ctx;
            }
            else /* must be true */ if (pass.Pass is GraphicsRenderPass graphics)
            {
                using var ctx = _device.BeginGraphicsContext(graphics.DefaultPipelineState);

                graphics.Record(ctx, ref _frame.Resolver);
                pass.Context = ctx;
            }
            else
            {
                ThrowHelper.ThrowArgumentException("what the fuck have you done");
            }
        }

        private GpuTask Execute()
        {
            using var contexts = RentedArray<GpuContext>.Create(/* barrier context */ /*_frame.RenderLayers!.Length +*/ _frame.NumBarrierLists + _frame.RenderPasses.Count);

            int offset = 0;
            foreach (ref var layer in _frame.RenderLayers.AsSpan())
            {
                // TODO multithread
                var barriers = layer.Barriers.AsReadOnlySpan();

                // we can't (!!) record an empty barrier list, A) it is bad, B) the layer.NumPreviousPasses only accounts for this if barriers are present
                if (!barriers.IsEmpty)
                {
                    using (var barrierCtx = _device.BeginGraphicsContext())
                    {
                        barrierCtx.Barrier(barriers);
                        contexts.Value[offset++] = barrierCtx;
                    }
                }

                foreach (ref var passIndex in layer.Passes.AsSpan())
                {
                    contexts.Value[offset++] = _frame.RenderPasses[passIndex].Context;
                }
            }

            var task = _device.Execute(contexts.AsSpan(), ExecutionContext.Graphics);
            return task;
        }

        private struct PassHeuristics : IComparable<PassHeuristics>
        {
            private TimeSpan _lastPassRecordTime;
            private TimeSpan _cumulativePassRecordTime;

            public int PassExecutionCount { get; set; }

            public TimeSpan LastPassRecordTime { get => _lastPassRecordTime; set { _cumulativePassRecordTime += value; _lastPassRecordTime = value; } }
            public TimeSpan AveragePassRecordTime => _cumulativePassRecordTime / PassExecutionCount;

            public int CompareTo([AllowNull] PassHeuristics other) => AveragePassRecordTime.CompareTo(other.AveragePassRecordTime);


#if DEBUG && false
            private TimeSpan _lastPassExecutionTime;
            private TimeSpan _cumulativePassExecutionTime;

            public TimeSpan LastPassExecutionTime { get => _lastPassExecutionTime; set { _cumulativePassExecutionTime += value; _lastPassExecutionTime = value; } }
            public TimeSpan AveragePassExecutionTime => _cumulativePassExecutionTime / PassExecutionCount;
#endif
        }
    }
}
