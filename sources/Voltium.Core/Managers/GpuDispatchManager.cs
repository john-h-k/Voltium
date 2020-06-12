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
using Voltium.Core.Pipeline;
using Voltium.Core.Pool;
using static TerraFX.Interop.D3D12_COMMAND_LIST_TYPE;
using static TerraFX.Interop.D3D12_COMMAND_QUEUE_PRIORITY;

namespace Voltium.Core.Managers
{

    /// <summary>
    /// In charge of managing submission of command lists, bundled, and queries, to a GPU
    /// </summary>
    internal sealed class GpuDispatchManager : IDisposable
    {
        private SynchronizedCommandQueue _graphics;
        private SynchronizedCommandQueue _compute;
        private SynchronizedCommandQueue _copy;

        private GraphicsDevice _device = null!;
        private const int DefaultListBufferSize = 4;

        private SpinLock _listAccessLock = new(EnvVars.IsDebug);

        private readonly List<GpuCommandSet> _graphicsLists = new(DefaultListBufferSize);
        private readonly List<GpuCommandSet> _computeLists = new(DefaultListBufferSize);
        private readonly List<GpuCommandSet> _copyLists = new(DefaultListBufferSize);

        private CommandListPool _listPool = null!;

        private GpuDispatchManager() { }

        /// <summary>
        /// Initialize the single instance of this type
        /// </summary>
        /// <param name="device">The device used to initialize the type</param>
        /// <param name="config"></param>
        public static GpuDispatchManager Create(GraphicsDevice device, GraphicalConfiguration config)
        {
            var value = new GpuDispatchManager();
            value.InternalCreate(device, config);
            return value;
        }

        private unsafe void InternalCreate(GraphicsDevice device, GraphicalConfiguration config)
        {
            _device = device;

            _frameFence = CreateFence();
            _frameMarker = new FenceMarker(config.SwapChainBufferCount - 1);

            _listPool = new CommandListPool(device);

            _graphics = CreateSynchronizedCommandQueue(D3D12_COMMAND_LIST_TYPE_DIRECT);
            _compute = CreateSynchronizedCommandQueue(D3D12_COMMAND_LIST_TYPE_COMPUTE);
            _copy = CreateSynchronizedCommandQueue(D3D12_COMMAND_LIST_TYPE_COPY);
        }

        private static readonly D3D12_FENCE_FLAGS DefaultFenceFlags = D3D12_FENCE_FLAGS.D3D12_FENCE_FLAG_NONE;

        private unsafe ComPtr<ID3D12Fence> CreateFence()
        {
            ComPtr<ID3D12Fence> fence = default;

            Guard.ThrowIfFailed(_device.Device->CreateFence(
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

            Guard.ThrowIfFailed(_device.Device->CreateCommandQueue(
                &desc,
                p.Guid,
                ComPtr.GetVoidAddressOf(&p)
            ));
            var name = GetListTypeName(type);
            DirectXHelpers.SetObjectName(p.Get(), name + " Queue");

            var fence = CreateFence();
            DirectXHelpers.SetObjectName(fence.Get(), name + " Fence");

            return new SynchronizedCommandQueue(_device, (ExecutionContext)type, p, fence.Move());
        }

        private string GetListTypeName(D3D12_COMMAND_LIST_TYPE type) => type switch
        {
            D3D12_COMMAND_LIST_TYPE_DIRECT => "Graphics",
            D3D12_COMMAND_LIST_TYPE_COMPUTE => "Compute",
            D3D12_COMMAND_LIST_TYPE_COPY => "Copy",
            _ => "Unknown"
        };

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
            // currently we let IDXGISwapChain::Present implicitly sync for us

            //_frameMarker++;

            //_graphics.Signal(_frameFence.Get(), _frameMarker);

            //FenceMarker lastFrame = _frameMarker - DeviceManager.BackBufferCount;

            //if (_frameFence.Get()->GetCompletedValue() <= lastFrame.FenceValue)
            //{
            //    using var sync = new GpuDispatchSynchronizer(_frameFence.Copy(), _frameMarker);
            //    sync.Block();
            //}
        }

        internal void BlockForGraphicsIdle()
        {
            _graphics.GetSynchronizerForIdle().Block();

            _frameMarker++;
        }

        internal bool IsGpuIdle()
            => _graphics.IsIdle() && _compute.IsIdle() && _copy.IsIdle();

        internal void BlockForGpuIdle()
        {
            _graphics.GetSynchronizerForIdle().Block();
            _compute.GetSynchronizerForIdle().Block();
            _copy.GetSynchronizerForIdle().Block();

            _frameMarker++;
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
        public void ExecuteSubmissions()
        {
            // is this necessary
            using (_listAccessLock.EnterScoped())
            {
                ExecuteAndClearList(ref _graphics, _graphicsLists);
                ExecuteAndClearList(ref _compute, _computeLists);
                ExecuteAndClearList(ref _copy, _copyLists);
            }
        }

        private void ExecuteAndClearList(ref SynchronizedCommandQueue queue, List<GpuCommandSet> lists)
        {
            queue.Execute(CollectionsMarshal.AsSpan(lists), out _);

            for (var i = 0; i < lists.Count; i++)
            {
                _listPool.Return(lists[i].List.Move());
            }

            lists.Clear();
        }

        public unsafe GraphicsContext BeginGraphicsContext(PipelineStateObject? defaultPso = null)
        {
            using var allocator = _graphics.RentAllocator();
            using var list = _listPool.Rent(ExecutionContext.Graphics, allocator.Get(), defaultPso is null ? null : defaultPso.GetPso());

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

            _graphics.Dispose();
            _compute.Dispose();
            _copy.Dispose();

            _frameFence.Dispose();
            _listPool.Dispose();
        }

        // It is ok for singletons to end up hitting their finalizer
        /// <summary>
        /// no
        /// </summary>
        ~GpuDispatchManager() => Dispose();
    }
}
