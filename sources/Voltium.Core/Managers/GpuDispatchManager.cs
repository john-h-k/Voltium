using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Common.Debugging;
using Voltium.Common.Threading;
using Voltium.Core.D3D12;
using Voltium.Core.Pool;
using static TerraFX.Interop.D3D12_COMMAND_LIST_TYPE;
using static TerraFX.Interop.D3D12_COMMAND_QUEUE_PRIORITY;

namespace Voltium.Core.Managers
{
    internal struct GpuCommandSet : IDisposable
    {
        public ComPtr<ID3D12GraphicsCommandList> List;
        public ComPtr<ID3D12CommandAllocator> Allocator;

        public GpuCommandSet Move()
        {
            var copy = this;
            copy.List = List.Move();
            copy.Allocator = Allocator.Move();
            return copy;
        }

        public GpuCommandSet Copy()
        {
            var copy = this;
            copy.List = List.Copy();
            copy.Allocator = Allocator.Copy();
            return copy;
        }

        public void Dispose()
        {
            List.Dispose();
            Allocator.Dispose();
        }
    }

    internal struct AllocatorAndMarker : IDisposable
    {
        public ComPtr<ID3D12CommandAllocator> Allocator;
        public FenceMarker Marker;

        public AllocatorAndMarker Move()
        {
            var copy = this;
            copy.Allocator = Allocator.Move();
            return copy;
        }

        public AllocatorAndMarker Copy()
        {
            var copy = this;
            copy.Allocator = Allocator.Copy();
            return copy;
        }

        public void Dispose()
        {
            Allocator.Dispose();
        }
    }

    /// <summary>
    /// In charge of managing submission of command lists, bundled, and queries, to a GPU
    /// </summary>
    public sealed class GpuDispatchManager : IDisposable
    {
        private SynchronizedCommandQueue _graphics;
        private SynchronizedCommandQueue _compute;
        private SynchronizedCommandQueue _copy;

        private uint _maxGpuFrameCount;

        private ComPtr<ID3D12Device> _device;
        private const int DefaultListBufferSize = 4;

        private SpinLock _listAccessLock = new(EnvVars.IsDebug);

        private readonly List<GpuCommandSet> _graphicsLists = new(DefaultListBufferSize);
        private readonly List<GpuCommandSet> _computeLists = new(DefaultListBufferSize);
        private readonly List<GpuCommandSet> _copyLists = new(DefaultListBufferSize);

        private readonly List<AllocatorAndMarker> _inFlightAllocators = new(DefaultListBufferSize);

        private CommandAllocatorPool _allocatorPool = null!;
        private CommandListPool _listPool = null!;

        private static bool _initialized;
        private static readonly object Lock = new();

        /// <summary>
        /// The single instance of this type. You must call <see cref="Initialize"/> before retrieving the instance
        /// </summary>
        public static GpuDispatchManager Manager
        {
            get
            {
                Guard.Initialized(_initialized);

                return Value;
            }
        }

        private static readonly GpuDispatchManager Value = new GpuDispatchManager();

        private GpuDispatchManager() { }

        /// <summary>
        /// Initialize the single instance of this type
        /// </summary>
        /// <param name="device">The device used to initialize the type</param>
        /// <param name="config"></param>
        public static void Initialize(ComPtr<ID3D12Device> device, GraphicalConfiguration config)
        {
            // TODO could probably use CAS/System.Threading.LazyInitializer
            lock (Lock)
            {
                Debug.Assert(!_initialized);

                _initialized = true;
                Value.CoreInitialize(device, config);
            }
        }

        private unsafe void CoreInitialize(ComPtr<ID3D12Device> device, GraphicalConfiguration config)
        {
            _device = device.Move();

            _maxGpuFrameCount = config.SwapChainBufferCount;
            _frameFence = CreateFence();

            _allocatorPool = new CommandAllocatorPool(_device.Copy());
            _listPool = new CommandListPool(_device.Copy());

            _graphics = CreateSynchronizedCommandQueue(D3D12_COMMAND_LIST_TYPE_DIRECT);
            DirectXHelpers.SetObjectName(_graphics.GetQueue(), "Graphics queue");

            _compute = CreateSynchronizedCommandQueue(D3D12_COMMAND_LIST_TYPE_COMPUTE);
            DirectXHelpers.SetObjectName(_compute.GetQueue(), "Compute queue");

            _copy = CreateSynchronizedCommandQueue(D3D12_COMMAND_LIST_TYPE_COPY);
            DirectXHelpers.SetObjectName(_copy.GetQueue(), "Copy queue");
        }

        private static readonly D3D12_FENCE_FLAGS DefaultFenceFlags = D3D12_FENCE_FLAGS.D3D12_FENCE_FLAG_NONE;

        private unsafe ComPtr<ID3D12Fence> CreateFence()
        {
            ComPtr<ID3D12Fence> fence = default;

            Guard.ThrowIfFailed(_device.Get()->CreateFence(
                0,
                DefaultFenceFlags,
                fence.Guid,
                (void**)&fence
            ));

            return fence;
        }

        // used for swapchain creation. not allowed to take ownership of returned pointer etc
        internal unsafe ID3D12CommandQueue* GetGraphicsQueue() => _graphics.GetQueue();

        private unsafe SynchronizedCommandQueue CreateSynchronizedCommandQueue(D3D12_COMMAND_LIST_TYPE type)
        {
            var desc = new D3D12_COMMAND_QUEUE_DESC
            {
                Type = type,
                Flags = D3D12_COMMAND_QUEUE_FLAGS.D3D12_COMMAND_QUEUE_FLAG_NONE,
                NodeMask = 0, // TODO: MULTI-GPU
                Priority = (int)D3D12_COMMAND_QUEUE_PRIORITY_NORMAL // why are you like this D3D12
            };

            ComPtr<ID3D12CommandQueue> p = default;

            Guard.ThrowIfFailed(_device.Get()->CreateCommandQueue(
                &desc,
                p.Guid,
                ComPtr.GetVoidAddressOf(&p)
            ));

            return new SynchronizedCommandQueue((ExecutionContext)type, p, CreateFence());
        }

        /// <summary>
        /// Submit a set of recorded commands to the list
        /// </summary>
        /// <param name="graphicsCommands">The commands to submit for execution</param>
        public void End(GraphicsContext graphicsCommands)
        {
            using (_listAccessLock.EnterScoped())
            {
                InternalEndNoSync(graphicsCommands.Move());
            }
        }

        private ComPtr<ID3D12Fence> _frameFence;
        private FenceMarker _frameMarker;

        internal unsafe void MoveToNextFrame()
        {
            _graphics.Signal(_frameFence.Get(), _frameMarker);

            FenceMarker lastFrame;
            if (_frameMarker.FenceValue < _maxGpuFrameCount)
            {
                lastFrame = default;
            }
            else
            {
                lastFrame = _frameMarker - _maxGpuFrameCount;
            }

            if (_graphics.GetReachedFence().IsBefore(lastFrame))
            {
                _graphics.GetSynchronizer(lastFrame).Block();
            }

            ReturnFinishedAllocators();

            _frameMarker++;
        }

        private void ReturnFinishedAllocators()
        {
            for (var i = 0; i < _inFlightAllocators.Count; i++)
            {
                var allocator = _inFlightAllocators[i];
                if (_graphics.GetReachedFence().IsAtOrAfter(allocator.Marker))
                {
                    _allocatorPool.Return(allocator.Allocator.Move());
                    _inFlightAllocators.RemoveAt(i);
                }
            }
        }

        internal bool IsGpuIdle()
            => _graphics.IsIdle() && _compute.IsIdle() && _copy.IsIdle();

        internal void BlockForGraphicsIdle()
        {
            _graphics.GetSynchronizerForIdle().Block();
        }

        internal void BlockForGpuIdle()
        {
            _graphics.GetSynchronizerForIdle().Block();
            _compute.GetSynchronizerForIdle().Block();
            _copy.GetSynchronizerForIdle().Block();
        }

        private unsafe ComPtr<ID3D12GraphicsCommandList> GetCommandList(
            ExecutionContext context,
            ID3D12CommandAllocator* allocator,
            ID3D12PipelineState* defaultPso
        )
        {
            return _listPool.Rent(context, allocator, defaultPso).Move();
        }

        /// <summary>
        /// Submit a set of recorded commands to the queue
        /// </summary>
        /// <param name="commands">The commands to submit for execution</param>
        public void End(ReadOnlySpan<GraphicsContext> commands)
        {
            using (_listAccessLock.EnterScoped())
            {
                // TODO SoA probably beats AoS for commands
                _graphicsLists.Capacity += commands.Length;
                for (int i = 0; i < commands.Length; i++)
                {
                    InternalEndNoSync(commands[i].Move());
                }
            }
        }

        private unsafe void InternalEndNoSync(GraphicsContext graphicsCommands)
        {
            var list = graphicsCommands.GetAndReleaseList();
            var allocator = graphicsCommands.GetAndReleaseAllocator();

            Guard.ThrowIfFailed(list.Get()->Close());

            Debug.Assert(list.Exists);

            var pair = new GpuCommandSet
            {
                List = list.Move(),
                Allocator = allocator.Move()
            };

            _graphicsLists.Add(pair);
        }

        /// <summary>
        /// Executes all recorded submissions
        /// </summary>
        public void ExecuteSubmissions(bool insertFence)
        {
            // is this necessary
            using (_listAccessLock.EnterScoped())
            {
                ExecuteAndClearList(ref _graphics, _graphicsLists, insertFence);
                ExecuteAndClearList(ref _compute, _computeLists, insertFence);
                ExecuteAndClearList(ref _copy, _copyLists, insertFence);
            }
        }

        private void ExecuteAndClearList(ref SynchronizedCommandQueue queue, List<GpuCommandSet> lists, bool insertFence)
        {
            queue.Execute(CollectionsMarshal.AsSpan(lists), insertFence);
            
            for (var i = 0; i < lists.Count; i++)
            {
                _listPool.Return(lists[i].List.Move());
                _inFlightAllocators.Add(new AllocatorAndMarker { Allocator = lists[i].Allocator.Move(), Marker = _frameMarker + 1 });
            }

            lists.Clear();
        }

        /// <summary>
        /// Returns a <see cref="GraphicsContext"/> used for recording graphical commands
        /// </summary>
        /// <returns>A new <see cref="GraphicsContext"/></returns>
        public unsafe GraphicsContext BeginGraphicsContext(ComPtr<ID3D12PipelineState> defaultPso = default)
        {
            using var allocator = _allocatorPool.Rent(ExecutionContext.Graphics);
            using var list = _listPool.Rent(ExecutionContext.Graphics, allocator.Get(), defaultPso.Get());

            using var ctx = new GraphicsContext(list.Move(), allocator.Move());
            return ctx.Move();
        }

        //public unsafe void PrepareForRender()
        //{

        //}

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose()
        {
            if (!IsGpuIdle())
            {
                BlockForGpuIdle();
            }

            _device.Dispose();
            _graphics.Dispose();
            _compute.Dispose();
            _copy.Dispose();
            _frameFence.Dispose();
            _allocatorPool.Dispose();
            _listPool.Dispose();
        }

        // It is ok for singletons to end up hitting their finalizer
        /// <summary>
        /// no
        /// </summary>
        ~GpuDispatchManager() => Dispose();
    }
}
