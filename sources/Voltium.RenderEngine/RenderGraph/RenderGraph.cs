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
using Voltium.Core.CommandBuffer;
using Voltium.Core.Contexts;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.RenderEngine
{
    internal abstract class Box
    {
        public abstract ref byte Data { get; }
    }

    internal class Box<T> : Box
    {
        public T Value;

        public Box(T value) => Value = value;

        public override ref byte Data => ref Unsafe.As<T, byte>(ref Value);
    }

    internal static class ThreadLocalBoxCache<T> where T : struct, IEquatable<T>
    {
        [ThreadStatic]
        private static ValueQueue<Box<T>> _boxes;

        private const int CacheCount = 64;

        public static bool TryGetBox(in T data, [NotNullWhen(true)] out Box<T>? box)
        {
            if (_boxes.IsValid)
            {
                _boxes = ValueQueue<Box<T>>.Create(CacheCount);
            }

            if (_boxes.Count == 0 || _boxes.Count == CacheCount)
            {
                box = null!;
                return false;
            }

            box = _boxes.Dequeue();
            box.Value = data;
            return true;
        }

        public static void ReturnBox(Box<T> o) => _boxes.Enqueue(o);
    }

    internal struct CachedString : IEquatable<CachedString>, IEquatable<string>
    {
        public CachedString(string s)
        {
            Value = s;
            _hashCode = null;
        }

        public readonly string Value;

        private int? _hashCode;

        public void CacheHashCode() => _ = HashCode;
        public int HashCode => _hashCode ??= Value.GetHashCode();
        public bool IsHashCodeCached => _hashCode is not null;

        public bool Equals(CachedString other)
        {
            if (_hashCode == other._hashCode)
            {
                return false;
            }
            return Value == other.Value;
        }

        public bool Equals(string? other) => Value == other;
    }

    /// <summary>
    /// A graph used to schedule and execute frames
    /// </summary>
    public unsafe sealed partial class RenderGraph
    {
        private GraphicsDevice _device;

        private DictionarySlim<CachedString, PassHeuristics> _heuristics = new();

        private struct FrameData
        {
            public ValueList<Pass> RenderPasses;

            public GraphLayer[]? RenderLayers;

            public ValueList<int> OutputPassIndices;

            public ValueList<int> InputPassIndices;
            public ValueList<TrackedResource> Resources;
            public ValueList<ViewDesc> Views;
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

        /// <summary>
        /// Indicates a pass creates a new buffer
        /// </summary>
        /// <param name="desc">The <see cref="BufferDesc"/> describing the pass's buffer</param>
        /// <param name="memoryAccess">The <see cref="MemoryAccess"/> the buffer will be allocated as</param>
        /// <param name="debugName">The <see cref="string"/> to set the resource name to in debug mode</param>
        /// <returns>A new <see cref="BufferHandle"/> representing the resource that can be later resolved to a <see cref="Buffer"/></returns>
        public BufferHandle CreateBuffer(in BufferDesc desc, MemoryAccess memoryAccess, string? debugName = null)
            => AddResource(new ResourceDesc { Type = ResourceType.Buffer, BufferDesc = desc, MemoryAccess = memoryAccess, InitialState = ResourceState.Common, DebugName = debugName }, _passIndex).AsBufferHandle();

        /// <summary> 
        /// Indicates a pass creates a new texture9
        /// </summary>
        /// <param name="desc">The <see cref="TextureDesc"/> describing the pass's buffer</param>
        /// <param name="initialState">The initial <see cref="ResourceState"/> of the resource</param>
        /// <param name="debugName">The <see cref="string"/> to set the resource name to in debug mode</param>
        /// <returns>A new <see cref="TextureHandle"/> representing the resource that can be later resolved to a <see cref="Buffer"/></returns>
        public TextureHandle CreateTexture(in TextureDesc desc, string? debugName = null)
            => AddResource(new ResourceDesc { Type = ResourceType.Texture, TextureDesc = desc, InitialState = initialState, DebugName = debugName }, _passIndex).AsTextureHandle();

        public ViewHandle CreateView(BufferHandle resource, BufferViewDesc? view = null)
            => AddView(new ViewDesc { Type = ResourceType.Buffer, Handle = resource.AsResourceHandle(), BufferViewDesc = view });
        public ViewHandle CreateView(TextureHandle resource, in TextureViewDesc? view = null)
            => AddView(new ViewDesc { Type = ResourceType.Texture, Handle = resource.AsResourceHandle(), TextureViewDesc = view });

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
            // Do this so we can recognise invalid flag combos (avoid people doing, e.g, 'return AsyncComputeValid' without specifying 'ExecutePass' as well)
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
            public GraphicsContext CommandBuffer { get; init; }
        }


        /// <summary>
        /// The delegate used for recording new render graph passes
        /// </summary>
        /// <typeparam name="TState">Opaque state that is passed to this delegate</typeparam>
        /// <param name="resolver"></param>
        /// <param name="state">The <see cref="Resolver"/> used for communicating between passes</param>
        /// <param name="info">The <see cref="PassExecutionInfo"/> for this pass</param>
        public delegate void PassRecord<TState>(
            Resolver resolver,
            TState state,
            PassExecutionInfo info
        );

        internal struct Pass
        {
            public string Name;
            // Untyped because we cannot type them due to generic limitations
            public /* TState */ Box? State;
            public /* PassRecord<TState> */ Delegate Record;
            public int Depth;
            public int Index;
            public ValueList<int> Dependencies;
            public ValueList<(ResourceHandle Resource, ResourceState State)> Transitions;
            public GpuContext Context;
        }

        public struct Dependency
        {
            internal ResourceHandle Handle;
            internal ResourceState BeginState;
            internal ResourceState EndState;


            public static Dependency Create(TextureHandle tex, ResourceState state)
                => new Dependency
                {
                    Handle = tex.AsResourceHandle(),
                    BeginState = state,
                    EndState = state
                };
            public static Dependency Create(BufferHandle tex, ResourceState state)
                => new Dependency
                {
                    Handle = tex.AsResourceHandle(),
                    BeginState = state,
                    EndState = state
                };
        }
        public ref struct PassDescription
        {
            public Span<Dependency> Dependencies;
            public PassRegisterDecision Decision;
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
            PassDescription builder,
            PassRecord<TState> record
        )
        {
            var passIndex = _frame.RenderPasses.Count;

            var transitions = builder.Transitions;
            var decision = builder.Decision;

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
                State = _ state,
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

        internal ViewHandle AddView(in ViewDesc desc)
        {
            _frame.Views.Add(desc);
            return new ViewHandle((uint)_frame.Views.Count);

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

        internal ref ViewDesc GetView(ViewHandle handle)
        {
            if (handle.IsInvalid)
            {
                ThrowHelper.ThrowInvalidOperationException("Resource was not created");
            }
            return ref _frame.Views.RefIndex((int)handle.Index - 1);
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

            var viewSet = _device.CreateViewSet((uint)_frame.Views.Length);
            uint i = 0;
            foreach (ref var view in _frame.Views.AsSpan())
            {
                view.View = view.Type switch
                {
                    ResourceType.Buffer => view.BufferViewDesc is null ? _device.CreateDefaultView(viewSet, i++, GetResource(view.Handle).Desc.Buffer) : ThrowHelper.ThrowNotImplementedException<View>(),
                    ResourceType.Texture => view.TextureViewDesc is null ? _device.CreateDefaultView(viewSet, i++, GetResource(view.Handle).Desc.Texture) : ThrowHelper.ThrowNotImplementedException<View>(),
                    ResourceType.RaytracingAccelerationStructure => _device.CreateDefaultView(viewSet, i++, GetResource(view.Handle).Desc.RaytracingAccelerationStructure),
                    _ => ThrowHelper.NeverReached<View>()
                };
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
            using var contexts = RentedArray<GpuContext>.Create(_frame.NumBarrierLists + _frame.RenderPasses.Count);

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
