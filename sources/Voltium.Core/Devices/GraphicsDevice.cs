using System;
using System.Diagnostics;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.DXGI;
using Voltium.Core.GpuResources;
using Voltium.Core.Pipeline;
using Voltium.Core.Memory.GpuResources;

using static TerraFX.Interop.D3D12_DESCRIPTOR_HEAP_TYPE;
using static TerraFX.Interop.D3D12_DRED_ENABLEMENT;
using static TerraFX.Interop.DXGI_DEBUG_RLO_FLAGS;
using static Voltium.Common.DirectXHelpers;
using static TerraFX.Interop.Windows;
using Voltium.Core.Devices;

namespace Voltium.Core.Managers
{
    /// <summary>
    /// The top-level manager for application resources
    /// </summary>
    public unsafe partial class GraphicsDevice : ComputeDevice
    {
        private SwapChain _swapChain;
        private ComPtr<IDXGIDebug1> _debugLayer;
        private Texture[] _backBuffer = null!;
        private GraphicalConfiguration _config = null!;
        private SynchronizedCommandQueue _graphicsQueue;

        private Adapter _adapter;
        private uint _syncInterval;
        internal ulong TotalFramesRendered = 0;
        internal uint BackBufferIndex;
        //private GpuDispatchManager _dispatch = null!;

        /// <summary>
        /// The <see cref="ScreenData"/> for the output
        /// </summary>
        public ScreenData ScreenData { get; private set; }

        private GraphicsDevice() { }

        /// <summary>
        /// Queries the GPU and timestamp
        /// </summary>
        /// <param name="context">The <see cref="ExecutionContext"/> indicating which queue should be queried</param>
        /// <param name="gpu">The <see cref="TimeSpan"/> representing the GPU clock</param>
        public void QueryTimestamp(ExecutionContext context, out TimeSpan gpu)
            => QueryTimestamps(context, out gpu, out _);

        /// <summary>
        /// Queries the GPU and CPU timestamps in close succession
        /// </summary>
        /// <param name="context">The <see cref="ExecutionContext"/> indicating which queue should be queried</param>
        /// <param name="gpu">The <see cref="TimeSpan"/> representing the GPU clock</param>
        /// <param name="cpu">The <see cref="TimeSpan"/> representing the CPU clock</param>
        public void QueryTimestamps(ExecutionContext context, out TimeSpan gpu, out TimeSpan cpu)
        {
            if (context != ExecutionContext.Graphics)
            {
                ThrowHelper.ThrowNotImplementedException("TODO");
            }

            ulong gpuTick, cpuTick;

            var queue = _graphicsQueue;

            if (!queue.TryQueryTimestamps(&gpuTick, &cpuTick))
            {
                ThrowHelper.ThrowExternalException("GPU timestamp query failed");
            }

            gpu = TimeSpan.FromSeconds(gpuTick / (double)queue.Frequency);
            cpu = TimeSpan.FromSeconds(cpuTick / (double)_cpuFrequency);
        }

        private ulong _cpuFrequency;

        /// <summary>
        /// Initialize the single instance of this type
        /// </summary>
        public static GraphicsDevice Create(GraphicalConfiguration config, in ScreenData screenData, HWND output)
        {
            var device = new GraphicsDevice();
            device._hwnd = output;
            device.InternalCreate(config, in screenData);
            return device;
        }

        /// <summary>
        /// Initialize the single instance of this type
        /// </summary>
        public static GraphicsDevice Create(GraphicalConfiguration config, in ScreenData screenData, IHwndOwner output)
            => Create(config, screenData, output.GetHwnd());

        /// <summary>
        /// Initialize the single instance of this type
        /// </summary>
        public static GraphicsDevice Create(GraphicalConfiguration config, in ScreenData screenData, void* unknownOutput)
        {
            var device = new GraphicsDevice();
            device._output = (IUnknown*)unknownOutput;
            device.InternalCreate(config, in screenData);
            return device;
        }

        /// <summary>
        /// Initialize the single instance of this type
        /// </summary>
        public static GraphicsDevice Create(GraphicalConfiguration config, in ScreenData screenData, ICoreWindowsOwner output)
            => Create(config, screenData, output.GetIUnknownForWindow());

        private HWND _hwnd;
        private IUnknown* _output;

        private void InternalCreate(GraphicalConfiguration config, in ScreenData screenData)
        {
            Guard.NotNull(config);

            _config = config;
            ScreenData = screenData;

            _config = config;
            _syncInterval = _config.VSyncCount;
            ScreenData = screenData;

            ulong frequency;
            QueryPerformanceFrequency((LARGE_INTEGER*) /* <- can we do that? */ &frequency);
            _cpuFrequency = frequency;

            EnableDebugLayer();

            foreach (Adapter adapter in AdapterFactory.Create())
            {
                if (adapter.IsSoftware)
                {
                    continue;
                }

                if (TryCreateNewDevice(adapter, (D3D_FEATURE_LEVEL)config.RequiredFeatureLevel, out _device))
                {
                    _adapter = adapter;
                    Logger.LogInformation($"New ID3D12Device created: \n{adapter}\n");
                    break;
                }
            }

            if (!_device.Exists)
            {
                Logger.LogError("Failed creation of ID3D12Device");
                ThrowHelper.ThrowPlatformNotSupportedException(
                    $"FATAL: Creation of ID3D12Device with feature level {config.RequiredFeatureLevel} failed");
            }

#if DEBUG || EXTENDED_ERROR_INFORMATION
            if (!_device.TryQueryInterface<ID3D12InfoQueue>(out var infoQueue))
            {
                Logger.LogError("Failed creation of ID3D12InfoQueue");
                ThrowHelper.ThrowPlatformNotSupportedException(
                    "FATAL: Creation of ID3D12InfoQueue failed");
            }

            D3D12DebugShim.Initialize(infoQueue);
#endif
            EnableDeviceRemovedExtendedDataLayer();

            // TODO WARP support

            Allocator = new GpuAllocator(this);

            _graphicsQueue = new SynchronizedCommandQueue(this, ExecutionContext.Graphics);

            CreateSwapChain();
            CreateDescriptorHeaps();
            Resize(ScreenData);

            Viewport = new Viewport(0.0f, 0.0f, ScreenData.Width, ScreenData.Height, 0.0f, 1.0f);
            Scissor = new Rectangle(0, 0, (int)ScreenData.Width, (int)ScreenData.Height);

            BackBufferIndex = _swapChain.BackBufferIndex;
        }

        /// <summary>
        /// The default allocator for the device
        /// </summary>
        public GpuAllocator Allocator { get; private set; } = null!;


        internal ComPtr<ID3D12Heap> CreateHeap(D3D12_HEAP_DESC desc)
        {
            ComPtr<ID3D12Heap> heap = default;
            Guard.ThrowIfFailed(DevicePointer->CreateHeap(
                &desc,
                heap.Guid,
                ComPtr.GetVoidAddressOf(&heap)
            ));

            return heap.Move();
        }

        internal ComPtr<ID3D12RootSignature> CreateRootSignature(uint nodeMask, void* pSignature, uint signatureLength)
        {
            using ComPtr<ID3D12RootSignature> rootSig = default;
            Guard.ThrowIfFailed(DevicePointer->CreateRootSignature(
                nodeMask,
                pSignature,
                signatureLength,
                rootSig.Guid,
                ComPtr.GetVoidAddressOf(&rootSig)
            ));

            return rootSig.Move();
        }

        internal D3D12_RESOURCE_ALLOCATION_INFO GetAllocationInfo(InternalAllocDesc desc)
            => DevicePointer->GetResourceAllocationInfo(0, 1, &desc.Desc);

        internal ComPtr<ID3D12Resource> CreatePlacedResource(ID3D12Heap* heap, ulong offset, InternalAllocDesc desc)
        {
            var clearVal = desc.ClearValue.GetValueOrDefault();

            using ComPtr<ID3D12Resource> resource = default;

            Guard.ThrowIfFailed(DevicePointer->CreatePlacedResource(
                 heap,
                 offset,
                 &desc.Desc,
                 desc.InitialState,
                 desc.ClearValue is null ? null : &clearVal,
                 resource.Guid,
                 ComPtr.GetVoidAddressOf(&resource)
             ));

            return resource.Move();
        }

        internal ComPtr<ID3D12Resource> CreateCommittedResource(InternalAllocDesc desc)
        {
            var heapProperties = GetHeapProperties(desc);
            var clearVal = desc.ClearValue.GetValueOrDefault();

            using ComPtr<ID3D12Resource> resource = default;

            Guard.ThrowIfFailed(DevicePointer->CreateCommittedResource(
                 &heapProperties,
                 D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
                 &desc.Desc,
                 desc.InitialState,
                 desc.ClearValue is null ? null : &clearVal,
                 resource.Guid,
                 ComPtr.GetVoidAddressOf(&resource)
            ));

            return resource;

            static D3D12_HEAP_PROPERTIES GetHeapProperties(InternalAllocDesc desc)
            {
                return new D3D12_HEAP_PROPERTIES(desc.HeapType);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public new CopyContext BeginCopyContext()
        {
            var ctx = BeginGraphicsContext();
            return ctx.AsCopyContext();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pso"></param>
        /// <returns></returns>
        public new ComputeContext BeginComputeContext(ComputePso? pso = null)
        {
            var ctx = BeginGraphicsContext(pso);
            return ctx.AsComputeContext();
        }

        /// <summary>
        /// Returns a <see cref="GraphicsContext"/> used for recording graphical commands
        /// </summary>
        /// <returns>A new <see cref="GraphicsContext"/></returns>
        public GraphicsContext BeginGraphicsContext(PipelineStateObject? pso = null)
        {
            if (_list == null || _allocator == null)
            {
                ID3D12GraphicsCommandList* list;
                ID3D12CommandAllocator* allocator;

                var iid0 = IID_ID3D12CommandAllocator;
                var iid1 = IID_ID3D12GraphicsCommandList;
                Guard.ThrowIfFailed(DevicePointer->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE.D3D12_COMMAND_LIST_TYPE_DIRECT, &iid0, (void**)&allocator));
                Guard.ThrowIfFailed(DevicePointer->CreateCommandList(0, D3D12_COMMAND_LIST_TYPE.D3D12_COMMAND_LIST_TYPE_DIRECT, allocator, pso is null ? null : pso.GetPso(), &iid1, (void**)&list));

                Guard.ThrowIfFailed(list->Close());

                _list = list;
                _allocator = allocator;
            }

            Guard.ThrowIfFailed(_allocator->Reset());
            Guard.ThrowIfFailed(_list->Reset(_allocator, pso is null ? null : pso.GetPso()));

            var rootSig = pso is null ? null : pso.GetRootSig();
            if (pso is ComputePso)
            {
                _list->SetComputeRootSignature(rootSig);
            }
            else if (pso is GraphicsPso)
            {
                _list->SetGraphicsRootSignature(rootSig);
            }

            SetDefaultDescriptorHeaps(_list);

            return new GraphicsContext(new(this, _list, _allocator));

            //return _dispatch.BeginGraphicsContext(pso);
        }

        private void SetDefaultDescriptorHeaps(ID3D12GraphicsCommandList* list)
        {
            const int numHeaps = 2;
            var heaps = stackalloc ID3D12DescriptorHeap*[numHeaps] { ResourceDescriptors.GetHeap(), _samplers.GetHeap() };

            list->SetDescriptorHeaps(numHeaps, heaps);
        }

        [ThreadStatic]
        private ID3D12GraphicsCommandList* _list;

        [ThreadStatic]
        private ID3D12CommandAllocator* _allocator;

        /// <summary>
        /// Submit a set of recorded commands to the list
        /// </summary>
        /// <param name="context">The commands to submit for execution</param>
        internal void End(ref GpuContext context)
        {
            ComPtr<ID3D12GraphicsCommandList> list = context.List;
            list.Get()->Close();
            _graphicsQueue.GetQueue()->ExecuteCommandLists(1, (ID3D12CommandList**)&list);
            _graphicsQueue.GetSynchronizerForIdle().Block();
            //return _dispatch.End(context);
        }

        /// <summary>
        /// Move to the next frame's set of resources
        /// </summary>
        public void MoveToNextFrame()
        {
            //_graphicsQueue.MoveToNextFrame();
            _graphicsQueue.GetSynchronizerForIdle().Block();
            BackBufferIndex = (BackBufferIndex + 1) % BackBufferCount;
        }

        /// <summary>
        /// The viewport for the entire screen
        /// </summary>
        public Viewport Viewport;


        /// <summary>
        /// The scissor for the entire screen
        /// </summary>
        public Rectangle Scissor;

        private DescriptorHandle[] _backBufferViews = null!;
        private DescriptorHandle _dsv;

        /// <summary>
        /// The render target view for the current frame
        /// </summary>
        public DescriptorHandle RenderTargetView => _backBufferViews[(int)BackBufferIndex];

        /// <summary>
        /// The depth stencil view for the current frame
        /// </summary>
        public DescriptorHandle DepthStencilView => _dsv;

        /// <summary>
        /// The <see cref="GpuResource"/> for the current render target resource
        /// </summary>
        public Texture BackBuffer => _backBuffer[BackBufferIndex];

        /// <summary>
        /// The number of CPU buffered resources
        /// </summary>
        public uint BackBufferCount => _config.SwapChainBufferCount;

        internal void ReleaseResourceAtFrameEnd(GpuResource resource)
        {

        }

        private void CreateSwapChain()
        {
            var desc = new DXGI_SWAP_CHAIN_DESC1
            {
                AlphaMode = DXGI_ALPHA_MODE.DXGI_ALPHA_MODE_IGNORE, // todo document
                BufferCount = _config.SwapChainBufferCount,
                BufferUsage = (int)DXGI_USAGE_RENDER_TARGET_OUTPUT, // this is the output chain
                Flags = 0,
                Format = (DXGI_FORMAT)_config.BackBufferFormat,
                Height = ScreenData.Height,
                Width = ScreenData.Width,
                SampleDesc = new DXGI_SAMPLE_DESC(_config.MultiSamplingStrategy.SampleCount, _config.MultiSamplingStrategy.QualityLevel),
                Scaling = DXGI_SCALING.DXGI_SCALING_NONE,
                Stereo = FALSE, // stereoscopic rendering, 2 images, e.g VR or 3D holo
                SwapEffect = DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_FLIP_DISCARD
            };

            using ComPtr<IDXGIFactory2> factory = default;

            int hr;
            if (ComPtr.TryQueryInterface(_adapter.UnderlyingAdapter, out IDXGIAdapter* dxgiAdapter))
            {
                hr = dxgiAdapter->GetParent(factory.Guid, ComPtr.GetVoidAddressOf(&factory));
                dxgiAdapter->Release();
            }
            else
            {
                hr = CreateDXGIFactory1(factory.Guid, ComPtr.GetVoidAddressOf(&factory));
            }

            if (hr == E_NOINTERFACE)
            {
                // we don't actually *need* IDXGIFactory2, we just need to do CreateSwapChain (rather than CreateSwapChainForHwnd etc) without it which is currently not implemented
                ThrowHelper.ThrowPlatformNotSupportedException("Platform does not support IDXGIFactory2, which is required");
            }

            Guard.ThrowIfFailed(hr, "GraphicsDevice.CreateSwapChain -- at IDXGIFactory2 creation");

            using ComPtr<IDXGISwapChain1> swapChain = default;

            // we handle the platform differences. UWP uses CoreWindow through IUnknown, Win32 uses HWND
            if (_output is not null)
            {
                Debug.Assert(_hwnd == HWND.NULL);

                Guard.ThrowIfFailed(factory.Get()->CreateSwapChainForCoreWindow(
                    (IUnknown*)_graphicsQueue.GetQueue(),
                    _output,
                    &desc,
                    null, // TODO maybe implement
                    ComPtr.GetAddressOf(&swapChain)
                ));
            }
            else
            {
                Debug.Assert(_output == null);

                Guard.ThrowIfFailed(factory.Get()->CreateSwapChainForHwnd(
                    (IUnknown*)_graphicsQueue.GetQueue(),
                    _hwnd,
                    &desc,
                    null, //&fullscreenDesc,
                    null, // TODO maybe implement
                    ComPtr.GetAddressOf(&swapChain)
                ));
            }

            if (!swapChain.TryQueryInterface(out ComPtr<IDXGISwapChain3> swapChain3))
            {
                ThrowHelper.ThrowPlatformNotSupportedException("Couldn't create IDXGISwapChain3, which is required for DX12");
            }

            _swapChain = new(swapChain3.Move());

            // TODO rotation
        }

        private void ResizeSwapChain()
        {
            for (var i = 0; i < (_backBuffer?.Length ?? 0); i++)
            {
                _backBuffer![i].Dispose();
            }

            _swapChain.ResizeBuffers(ScreenData.Width, ScreenData.Height);

            BackBufferIndex = _swapChain.BackBufferIndex;
        }

        /// <summary>
        /// Resize the render resources
        /// </summary>
        /// <param name="newScreenData">The <see cref="ScreenData"/> indicating the size to resize to</param>
        public void Resize(ScreenData newScreenData)
        {
            ScreenData = newScreenData;

            _rtvs.ResetHeap();
            _dsvs.ResetHeap();

            _graphicsQueue.GetSynchronizerForIdle().Block();

            ResizeSwapChain();
            InitializeBackBuffers();
        }

        internal void ReportLiveObjects(bool internalObjects = false)
        {
            if (_debugLayer.Exists)
            {
                Guard.ThrowIfFailed(_debugLayer.Get()->ReportLiveObjects(
                    DXGI_DEBUG_ALL,
                    DXGI_DEBUG_RLO_DETAIL | (internalObjects ? DXGI_DEBUG_RLO_ALL : DXGI_DEBUG_RLO_IGNORE_INTERNAL)
                ));
            }
        }

        /// <summary>
        /// Present the next frame
        /// </summary>
        public void Present()
        {
            //_dispatch.ExecuteSubmissions();

            var hr = _swapChain.Present(_syncInterval, 0);

            TotalFramesRendered++;

            if (hr == DXGI_ERROR_DEVICE_REMOVED || hr == DXGI_ERROR_DEVICE_RESET)
            {
                OnDeviceRemoved();
            }
            else
            {
                Guard.ThrowIfFailed(hr, "_swapChain.Get()->Present(_syncInterval, 0)");

                MoveToNextFrame();
            }

        }

        private void OnDeviceRemoved()
        {
            // we don't cache DRED state. we could, as this is the only class that should
            // change DRED state, but this isn't a fast path so there is no point
            if (_device.TryQueryInterface<ID3D12DeviceRemovedExtendedData>(out var dred))
            {
                using (dred)
                {
                    OnDeviceRemovedWithDred(dred.Get());
                }
            }

            Logger.LogError(
                "Device removed, no DRED present. Enable DEBUG or D3D12_DRED for enhanced device removed information");
        }

        private void OnDeviceRemovedWithDred(ID3D12DeviceRemovedExtendedData* dred)
        {
            Debug.Assert(dred != null);

            D3D12_DRED_AUTO_BREADCRUMBS_OUTPUT breadcrumbs;
            D3D12_DRED_PAGE_FAULT_OUTPUT pageFault;

            Guard.ThrowIfFailed(dred->GetAutoBreadcrumbsOutput(&breadcrumbs));
            Guard.ThrowIfFailed(dred->GetPageFaultAllocationOutput(&pageFault));

            // TODO dred logging
        }


        [Conditional("DEBUG")]
        [Conditional("D3D12_DEBUG_LAYER")]
        [Conditional("DXGI_DEBUG_LAYER")]
        private void EnableDebugLayer()
        {
            using ComPtr<ID3D12Debug> debugLayer = default;

            Guard.ThrowIfFailed(D3D12GetDebugInterface(
                debugLayer.Guid,
                ComPtr.GetVoidAddressOf(&debugLayer)
            ));

            debugLayer.Get()->EnableDebugLayer();

            if (debugLayer.TryQueryInterface<ID3D12Debug1>(out var debugLayer1))
            {
                using (debugLayer1)
                {
                    debugLayer1.Get()->SetEnableGPUBasedValidation(TRUE);
                }
            }

            using ComPtr<IDXGIDebug1> dxgiDebugLayer = default;

            Guard.ThrowIfFailed(DXGIGetDebugInterface1(
                0,
                dxgiDebugLayer.Guid,
                ComPtr.GetVoidAddressOf(&dxgiDebugLayer)
            ));

            Guard.ThrowIfFailed(dxgiDebugLayer.Get()->ReportLiveObjects(
                DXGI_DEBUG_ALL,
                DXGI_DEBUG_RLO_SUMMARY | DXGI_DEBUG_RLO_IGNORE_INTERNAL
            ));

            _debugLayer = dxgiDebugLayer.Move();
        }

        [Conditional("DEBUG")]
        [Conditional("D3D12_DRED")]
        private void EnableDeviceRemovedExtendedDataLayer()
        {
            using ComPtr<ID3D12DeviceRemovedExtendedDataSettings> dredLayer = default;

            int hr = D3D12GetDebugInterface(
                dredLayer.Guid,
                ComPtr.GetVoidAddressOf(&dredLayer)
            );

            if (FAILED(hr) || !dredLayer.Exists)
            {
                Logger.LogWarning("DRED could not be initialized");
                return;
            }

            dredLayer.Get()->SetAutoBreadcrumbsEnablement(D3D12_DRED_ENABLEMENT_FORCED_ON);
            dredLayer.Get()->SetPageFaultEnablement(D3D12_DRED_ENABLEMENT_FORCED_ON);
        }

        private bool TryCreateNewDevice(
            Adapter adapter,
            D3D_FEATURE_LEVEL requiredLevel,
            out ComPtr<ID3D12Device> device
        )
        {
            using ComPtr<ID3D12Device> p = default;
            bool success = SUCCEEDED(D3D12CreateDevice(
                adapter.UnderlyingAdapter,
                requiredLevel,
                p.Guid,
                ComPtr.GetVoidAddressOf(&p)
            ));

            device = p.Move();

            if (success)
            {
                SetObjectName(device.Get(), nameof(device));
            }

            return success;
        }

        private void InitializeBackBuffers()
        {
            _backBuffer = new Texture[BackBufferCount];
            _backBufferViews = new DescriptorHandle[_backBuffer.Length];

            for (uint i = 0; i < _backBuffer.Length; i++)
            {
                // Dispose if it is still alive, else nop
                _backBuffer[i].Dispose();
                _backBuffer[i] = _swapChain.GetBackBuffer(i);
                _backBufferViews[i] = CreateRenderTargetView(_backBuffer[i]);
            }
        }

        private D3D12_RENDER_TARGET_VIEW_DESC CreateRenderTargetViewDesc()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose()
        {
            ReportLiveObjects();

            _swapChain.Dispose();
            _debugLayer.Dispose();
            _graphicsQueue.Dispose();
            _device.Dispose();
        }
    }
}
