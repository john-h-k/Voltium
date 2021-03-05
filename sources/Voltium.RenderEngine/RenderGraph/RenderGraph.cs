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

    internal class ThreadLocalBoxCache<T> where T : struct, IEquatable<T>
    {
        private object? _box = default(T);

        public bool TryGetBox(in T data, [NotNullWhen(true)] out object? box)
        {
            if (_box is null)
            {
                box = null!;
                return false;
            }

            // this is not allowed and not ok
            Unsafe.Unbox<T>(_box) = data;
            box = _box;
            _box = null;
            return true;
        }

        public void ReturnBox(object o) => _box = o;
    }

    /// <summary>
    /// A graph used to schedule and execute frames
    /// </summary>
    public unsafe sealed partial class RenderGraph
    {
        private GraphicsDevice _device;

        private struct CachedString : IEquatable<CachedString>, IEquatable<string>
        {
            public CachedString(string s)
            {
                Value = s;
                _hashCode = null;
            }

            public readonly string Value;

            private int? _hashCode;

            public int HashCode => _hashCode ??= Value.GetHashCode();
            public bool IsHashCodeCached => _hashCode is not null;

            public bool Equals(CachedString other)
            {
                if (IsHashCodeCached && other.IsHashCodeCached && HashCode != other.HashCode)
                {
                    return false;
                }
                return Value == other.Value;
            }

            public bool Equals(string? other) => Value == other;
        }

        private DictionarySlim<CachedString, PassHeuristics> _heuristics = new();

        private struct FrameData
        {
            public ValueList<Pass> RenderPasses;

            public GraphLayer[]? RenderLayers;

            public ValueList<int> OutputPassIndices;

            public ValueList<int> InputPassIndices;
            public ValueList<TrackedResource> Resources;
            public ValueList<TrackedResource> PersistentResources;
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

        /// <summary>
        /// Provides options a render pass can decide about itself to inform the graph
        /// </summary>
        [Flags]
        public enum PassRegisterDecision
        {
            /// <summary>
            /// This pass should be executed
            /// </summary>
            ExecutePass = 1,

            /// <summary>
            /// This pass has an external output, so the graph can't cull it or any passes it depends on
            /// </summary>
            HasExternalOutputs = 2,

            /// <summary>
            /// The pass can be executed on the async compute queue
            /// </summary>
            AsyncComputeValid = 4,

            /// <summary>
            /// The pass should not be executed
            /// </summary>
            // Do this so we can recognise invalid flag combos (avoid people doing, e.g, return AsyncComputeValid)
            RemovePass = ~(ExecutePass | HasExternalOutputs | AsyncComputeValid)
        }

        /// <summary>
        /// Information about a pass execution
        /// </summary>
        public readonly struct PassExecutionInfo
        {
            /// <summary>
            /// Whether the current pass registered for and is enqueued for async compute execution 
            /// </summary>
            public bool IsOnAsyncCompute { get; init; }

            /// <summary>
            /// The <see cref="GpuContext"/> for this pass
            /// </summary>
            public GpuContext CommandBuffer { get; init; }
        }

        /// <summary>
        /// The delegate used for registering new render graph passes
        /// </summary>
        /// <typeparam name="TState">Opaque state that is passed to this delegate</typeparam>
        /// <param name="builder">The <see cref="RenderPassBuilder"/> used for deciding render graph inputs and outputs</param>
        /// <param name="resolver">The <see cref="Resolver"/> used for communicating between passes</param>
        /// <param name="state">The <typeparamref name="TState"/> passed by the consumer</param>
        /// <returns>The execution decision for the pass</returns>
        public delegate PassRegisterDecision PassRegister<TState>(
            ref RenderPassBuilder builder,
            ref Resolver resolver,
            TState state
        );


        /// <summary>
        /// The delegate used for recording new render graph passes
        /// </summary>
        /// <typeparam name="TState">Opaque state that is passed to this delegate</typeparam>
        /// <param name="resolver"></param>
        /// <param name="state">The <see cref="Resolver"/> used for communicating between passes</param>
        /// <param name="info">The <see cref="PassExecutionInfo"/> for this pass</param>
        public delegate void PassRecord<TState>(
            ref Resolver resolver,
            TState state,
            in PassExecutionInfo info
        );

        internal struct Pass
        {
            public string Name;
            public /* TState */ object? State;
            public /* PassRecord<TState> */ Delegate Record;
            public int Depth;
            public int Index;
            public ValueList<int> Dependencies;
            public ValueList<(ResourceHandle Resource, ResourceState State)> Transitions;
            public GpuContext Context;
        }

        /// <summary>
        /// Registers a pass into the graph, and calls the <paramref name="register"/> method immediately
        /// method immediately to register all dependencies
        /// </summary>
        /// <typeparam name="TState">Opaque state that is passed to this <paramref name="register"/></typeparam>
        /// <param name="name">The name of the pass</param>
        /// <param name="state">The <typeparamref name="TState"/> to pass to <paramref name="register"/></param>
        /// <param name="register">The registration method</param>
        /// <param name="record">The record method</param>
        public void AddPass<TState>(
            string name,
            TState state,
            PassRegister<TState> register,
            PassRecord<TState> record
        )
        {
            var passIndex = _frame.RenderPasses.Count;
            var builder = new RenderPassBuilder(this, passIndex);

            var decision = register(ref builder, ref _frame.Resolver, state);

            if (!decision.HasFlag(PassRegisterDecision.ExecutePass) && decision != PassRegisterDecision.RemovePass)
            {
                ThrowHelper.ThrowInvalidOperationException($"Registration returned execution flags '{decision}' but did not set 'PassRegisterDecision.ExecutePass'");
            }

            if (decision == PassRegisterDecision.RemovePass)
            {
                // Pass culled itself
                return;
            }

            // anything with no dependencies is a top level input node implicity
            if (builder.FrameDependencies.Count == 0)
            {
                // the index _renderPasses.Add results in
                _frame.InputPassIndices.Add(passIndex);
            }

            if (builder.Depth > _frame.MaxDepth)
            {
                _frame.MaxDepth = builder.Depth;
            }

            var pass = new Pass
            {
                Name = name,
                Record = record,
                State = state,
                Index = passIndex,
                Depth = builder.Depth
            };

            _frame.RenderPasses.Add(pass);
        }

        /// <summary>
        /// Executes the graph
        /// </summary>
        public void ExecuteGraph()
        {
            // false during the register passes
            _frame.Resolver.CanResolveResources = true;

            // Work out the order passes will execute in
            Schedule();

            // Realise the requested resources into something usable
            AllocateResources();

            // Create the barrier sets between layers
            BuildBarriers();

            // Everything until now has been setup, and in serial
            // Now we record the actual passes (calling the render pass record methods) in parallel (well... soon)
            Record();

            // Submit them to the device
            var task = Execute();

            // Doesn't actually deallocate them, but takes them out of the render graph's control so they will be destroyed where possible
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
                _lastFrame.Resources = default;
            }

            //_frame.Resolver = new Resolver(this);
            _frame.Resolver = _lastFrame.Resolver;
            _frame.RenderPasses = new();
            _frame.RenderLayers = null;
            _frame.OutputPassIndices = new();
            _frame.Resources = new();
            _frame.InputPassIndices = new();
            _frame.MaxDepth = default;
            _frame.NumBarrierLists = 0;
        }

        private void DeallocateResources(in GpuTask frame)
        {
            frame.RegisterDisposal(_frame.Resources.ToArray());
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

                if (pass.Transitions.Count != 0)
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

        internal ref TrackedResource GetResource(ResourceHandle handle)
        {
            if (handle.IsInvalid)
            {
                ThrowHelper.ThrowInvalidOperationException("Resource was not created");
            }
            return ref _frame.Resources.RefIndex((int)handle.Index - 1);
        }

        internal ref Pass GetRenderPass(int index) => ref _frame.RenderPasses.RefIndex(index);

        private enum ResourceBarrierType
        {
            Transition,
            WriteBarrier,
            Aliasing
        }

        private struct GraphLayer
        {
            /// <summary> The barriers executed before this layer is executed </summary>
            public List<(ResourceHandle Resource, ResourceState State)> Barriers;

            /// <summary> The indices in the GpuContext array that can be executed in any order </summary>
            public List<int> Passes;


            public int NumPreviousPasses;
        }

        private void AllocateResources()
        {
            foreach (ref var resource in _frame.Resources.AsSpan())
            {
                // TODO pooling
                resource.AllocateFrom(_device.Allocator);
                resource.CurrentTrackedState = resource.Desc.InitialState;

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


                    }
                }
            }
        }

        private void Record()
        {
            RecordWithoutHeuristics();
        }

        //private void RecordWithHeuristics()
//        {
//#pragma warning disable CS0162 // Unreachable code detected
//            _frame.RenderPasses.AsSpan().Sort(static (a, b) =>
//#pragma warning restore CS0162 // Unreachable code detected
//            {
//                Debug.Assert(a.Graph == b.Graph);
//                ref PassHeuristics heuristicsA = ref a.Graph._heuristics.GetOrAddValueRef(a.Pass);
//                ref PassHeuristics heuristicsB = ref b.Graph._heuristics.GetOrAddValueRef(b.Pass);
//                return heuristicsA.CompareTo(heuristicsB);
//            });


//            using var tasks = RentedArray<Task>.Create(Environment.ProcessorCount);

//            for (int i = 0, offset = 0; i < _frame.RenderPasses.Count + Environment.ProcessorCount - 1; i += Environment.ProcessorCount, offset++)
//            {
//                tasks.Value[offset] = Task.Run(() =>
//                {
//                    for (var j = 0; j < Environment.ProcessorCount; j++)
//                    {
//                        var index = (j * Environment.ProcessorCount) + i;
//                        if (index >= _frame.RenderPasses.Count)
//                        {
//                            return;
//                        }

//                        ref var pass = ref _frame.RenderPasses.AsSpan()[index];

//                        ref var heuristics = ref _heuristics.GetOrAddValueRef(pass.Pass);

//                        double start = Stopwatch.GetTimestamp(), end = 0;

//                        RecordPass(ref pass);

//                        end = Stopwatch.GetTimestamp();

//                        var recordLength = TimeSpan.FromMilliseconds(Math.Max((end - start), 0) / Stopwatch.Frequency);

//                        heuristics.PassExecutionCount++;
//                        heuristics.LastPassRecordTime = recordLength;
//                    }
//                });
//            }

//            Task.WhenAll(tasks.Value).RunSynchronously();
//        }

        private void RecordWithoutHeuristics()
        {
            var passes = _frame.RenderPasses.AsSpan();
            foreach (ref var pass in passes)
            {
                RecordPass(ref pass);
            }
        }

        private void RecordPass(ref Pass pass)
        {
            ref var resolver = ref _frame.Resolver;
            var info = new PassExecutionInfo
            {
                IsOnAsyncCompute = false,
                CommandBuffer = GetContext()
            };

            Unsafe.As<PassRecord<object?>>(pass.Record)(ref resolver, pass.State, info);

            pass.Context = info.CommandBuffer;
        }

        private GraphicsContext GetContext() => new GraphicsContext();
        private void ReturnContext(GpuContext context)
        { }

        private GpuTask Execute()
        {
            using var contexts = RentedArray<GpuContext>.Create(/* barrier context */ /*_frame.RenderLayers!.Length +*/ _frame.NumBarrierLists + _frame.RenderPasses.Count);

            int offset = 0;
            foreach (ref var layer in _frame.RenderLayers.AsSpan())
            {
                // TODO multithread
                var transitions = layer.Barriers.AsReadOnlySpan();

                using var barriers = RentedArray<ResourceTransition>.Create(transitions.Length);

                int i = 0;
                foreach (ref readonly var transition in transitions)
                {
                    var resource = _frame.Resources[(int)transition.Resource.Index];
                    barriers.Value[i++] = resource.Desc.Type switch
                    {
                        ResourceType.Buffer => ResourceTransition.Create(resource.Desc.Buffer, resource.CurrentTrackedState, transition.State),
                        ResourceType.Texture => ResourceTransition.Create(resource.Desc.Texture, resource.CurrentTrackedState, transition.State),
                        _ => default
                    };
                }

                // we can't (!!) record an empty barrier list, A) it is bad, B) the layer.NumPreviousPasses only accounts for this if barriers are present
                if (!transitions.IsEmpty)
                {
                    var barrierContext = GetContext();
                    barrierContext.Barrier(barriers.AsSpan());
                    contexts.Value[offset++] = barrierContext;
                }

                foreach (ref readonly var pass in layer.Passes.AsSpan())
                {
                    contexts.Value[offset++] = _frame.RenderPasses[pass].Context;
                }
            }

            var task = _device.GraphicsQueue.Execute(contexts.Value);
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

            public TimeSpan LastPassExecutionTime { get => _lastPassExecutionTime; set { _cumulativePassExecutionTime += value; _lastPassExecutionTime = value; PassExecutionCount++ } }
            public TimeSpan AveragePassExecutionTime => _cumulativePassExecutionTime / PassExecutionCount;
#endif
        }
    }
}
