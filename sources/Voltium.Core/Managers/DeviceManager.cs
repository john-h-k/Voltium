using System;
using System.Diagnostics;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.DXGI;
using Voltium.Core.GpuResources;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12_DESCRIPTOR_HEAP_TYPE;
using static Voltium.Common.DirectXHelpers;
using static TerraFX.Interop.D3D12_DRED_ENABLEMENT;
using static TerraFX.Interop.DXGI_DEBUG_RLO_FLAGS;
using static TerraFX.Interop.DXGI_SWAP_CHAIN_FLAG;
using Voltium.Common.Debugging;

namespace Voltium.Core.Managers
{
    /// <summary>
    /// The top-level manager for application resources
    /// </summary>
    public static unsafe class DeviceManager
    {
        private static ComPtr<ID3D12Device> _device;
        private static Adapter _adapter;
        private static ComPtr<IDXGIDebug1> _debugLayer;
        private static ComPtr<IDXGIFactory2> _factory;
        private static ComPtr<IDXGISwapChain3> _swapChain;
        private static uint _syncInterval;
        internal static ulong TotalFramesRendered = 0;
        private static DescriptorHeap _renderTargetViewHeap;
        private static DescriptorHeap _depthStencilViewHeap;
        private static GpuResource[] _renderTargets = null!;
        private static GpuResource _depthStencil = null!;
        internal static uint BackBufferIndex;
        private static readonly object Lock = new object();
        private static GraphicalConfiguration _config = null!;

        /// <summary>
        /// The <see cref="ScreenData"/> for the output
        /// </summary>
        public static ScreenData ScreenData { get; private set; }

        /// <summary>
        /// Initialize the single instance of this type
        /// </summary>
        public static void Initialize(GraphicalConfiguration config, in ScreenData screenData)
        {
            lock (Lock)
            {
                CoreInitialize(config, in screenData);
            }
        }

        private static void CoreInitialize(GraphicalConfiguration config, in ScreenData screenData)
        {
            Guard.NotNull(config);

            _config = config;
            ScreenData = screenData;

            _config = config;
            _syncInterval = _config.VSyncCount;
            ScreenData = screenData;

            EnableDebugLayer();

            using ComPtr<IDXGIFactory2> factory = default;

            Guard.ThrowIfFailed(CreateDXGIFactory(
                factory.Guid,
                ComPtr.GetVoidAddressOf(&factory)
            ));

            _factory = factory.Move();

            foreach (Adapter adapter in Adapter.EnumerateAdapters(_factory))
            {
                if (adapter.IsSoftware)
                {
                    continue;
                }

                if (TryCreateNewDevice(adapter, config.RequiredDirect3DLevel, out _device))
                {
                    _adapter = adapter;
                    Logger.LogInformation($"New ID3D12Device created: {adapter.Description}");
                    break;
                }
            }

            if (!_device.Exists)
            {
                Logger.LogError("Failed creation of ID3D12Device");
                ThrowHelper.ThrowPlatformNotSupportedException(
                    $"FATAL: Creation of ID3D12Device with feature level {config.RequiredDirect3DLevel} failed");
            }

#if DEBUG || EXTENDED_ERROR_INFORMATION
            if (!_device.TryQueryInterface(out ComPtr<ID3D12InfoQueue> infoQueue))
            {
                Logger.LogError("Failed creation of ID3D12InfoQueue");
                ThrowHelper.ThrowPlatformNotSupportedException(
                    "FATAL: Creation of ID3D12InfoQueue failed");
            }

            D3D12DebugShim.Initialize(infoQueue);
#endif
            //EnableDeviceRemovedExtendedDataLayer();

            // TODO WARP support

            Allocator = new GpuAllocator(_device.Copy());

            InitializeDescriptorSizes();
            GpuDispatchManager.Initialize(_device, _config);

            CreateSwapChain();
            CreateRtvAndDsvDescriptorHeaps();
            Resize(ScreenData);

            Viewport = new Viewport(0.0f, 0.0f, ScreenData.Width, ScreenData.Height, 0.0f, 1.0f);
            Scissor = new Rectangle(0, 0, (int)ScreenData.Width, (int)ScreenData.Height);
        }

        /// <summary>
        /// The default allocator for the device
        /// </summary>
        public static GpuAllocator Allocator { get; private set; } = null!;

        private static void InitializeDescriptorSizes()
        {
            ConstantBufferOrShaderResourceOrUnorderedAccessViewDescriptorSize =
                (int)Device->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);

            RenderTargetViewDescriptorSize =
                (int)Device->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);

            DepthStencilViewDescriptorSize =
                (int)Device->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_DSV);

            SamplerDescriptorSize = (int)Device->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER);
        }

        /// <summary>
        /// Gets the <see cref="ID3D12Device"/> used by this application
        /// </summary>
        public static ID3D12Device* Device => _device.Get();


        /// <summary>
        /// Move to the next frame's set of resources
        /// </summary>
        public static void MoveToNextFrame()
        {
            GpuDispatchManager.Manager.MoveToNextFrame();
            BackBufferIndex = (BackBufferIndex + 1) % BackBufferCount;
        }

        /// <summary>
        /// The viewport for the entire screen
        /// </summary>
        public static Viewport Viewport;


        /// <summary>
        /// The scissor for the entire screen
        /// </summary>
        public static Rectangle Scissor;

        /// <summary>
        /// The render target view for the current frame
        /// </summary>
        public static DescriptorHandle RenderTargetView => _renderTargetViewHeap.FirstDescriptor + (int)BackBufferIndex;

        /// <summary>
        /// The depth stencil view for the current frame
        /// </summary>
        public static DescriptorHandle DepthStencilView => _depthStencilViewHeap.FirstDescriptor;

        /// <summary>
        /// The <see cref="GpuResource"/> for the current render target resource
        /// </summary>
        public static GpuResource RenderTarget => _renderTargets[BackBufferIndex];

        /// <summary>
        /// The <see cref="GpuResource"/> for the current depth stencil resource
        /// </summary>
        public static GpuResource DepthStencil => _depthStencil;

        /// <summary>
        /// The number of CPU buffered resources
        /// </summary>
        public static uint BackBufferCount => _config.SwapChainBufferCount;

        /// <summary>
        /// The size of a descriptor of type <see cref="D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV"/>
        /// </summary>
        // why the fuck did i call it this
        public static int ConstantBufferOrShaderResourceOrUnorderedAccessViewDescriptorSize { get; private set; }

        /// <summary>
        /// The size of a descriptor of type <see cref="D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_RTV"/>
        /// </summary>
        public static int RenderTargetViewDescriptorSize { get; private set; }

        /// <summary>
        /// The size of a descriptor of type <see cref="D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_DSV"/>
        /// </summary>
        public static int DepthStencilViewDescriptorSize { get; private set; }

        /// <summary>
        /// The size of a descriptor of type <see cref="D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER"/>
        /// </summary>
        public static int SamplerDescriptorSize { get; private set; }

        /// <summary>
        /// Gets the size of a descriptor for a given <see cref="D3D12_DESCRIPTOR_HEAP_TYPE"/>
        /// </summary>
        /// <param name="type">The type of the descriptor</param>
        /// <returns>The size of the descriptor, in bytes</returns>
        public static int GetDescriptorSizeForType(D3D12_DESCRIPTOR_HEAP_TYPE type)
        {
            Debug.Assert(type > 0 && type < D3D12_DESCRIPTOR_HEAP_TYPE_NUM_TYPES);

            return type switch
            {
                D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV => ConstantBufferOrShaderResourceOrUnorderedAccessViewDescriptorSize,
                D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER => SamplerDescriptorSize,
                D3D12_DESCRIPTOR_HEAP_TYPE_RTV => RenderTargetViewDescriptorSize,
                D3D12_DESCRIPTOR_HEAP_TYPE_DSV => DepthStencilViewDescriptorSize,
                _ => 0
            };
        }

        private static void CreateSwapChain()
        {
            var desc = new DXGI_SWAP_CHAIN_DESC1
            {
                AlphaMode = DXGI_ALPHA_MODE.DXGI_ALPHA_MODE_IGNORE, // todo document
                BufferCount = _config.SwapChainBufferCount,
                BufferUsage = (int)DXGI_USAGE_RENDER_TARGET_OUTPUT, // this is the output chain
                Flags = (uint)(DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH),
                Format = _config.BackBufferFormat,
                Height = ScreenData.Height,
                Width = ScreenData.Width,
                SampleDesc = new DXGI_SAMPLE_DESC(_config.MultiSamplingStrategy.SampleCount, _config.MultiSamplingStrategy.QualityLevel),
                Scaling = _config.ScalingStrategy,
                Stereo = FALSE, // stereoscopic rendering, 2 images, e.g VR or 3D holo
                SwapEffect = _config.SwapEffect
            };

            var fullscreenDesc = new DXGI_SWAP_CHAIN_FULLSCREEN_DESC
            {
                RefreshRate = new DXGI_RATIONAL(0, 0),
                Scaling = _config.FullscreenScalingStrategy,
                ScanlineOrdering = _config.ScanlineOrdering,
                Windowed = _config.ForceFullscreenAsWindowed ? TRUE : FALSE
            };

            using ComPtr<IDXGISwapChain1> swapChain = default;
            Guard.ThrowIfFailed(_factory.Get()->CreateSwapChainForHwnd(
                (IUnknown*)GpuDispatchManager.Manager.GetGraphicsQueue(),
                ScreenData.Handle,
                &desc,
                null, //&fullscreenDesc,
                null, // TODO maybe implement
                ComPtr.GetAddressOf(&swapChain)
            ));

            if (!swapChain.TryQueryInterface(out ComPtr<IDXGISwapChain3> swapChain3))
            {
                ThrowHelper.ThrowPlatformNotSupportedException("Couldn't create swapchain3");
            }

            _swapChain = swapChain3.Move();

            // TODO rotation
        }

        private static void ResizeSwapChain()
        {
            for (var i = 0; i < (_renderTargets?.Length ?? 0); i++)
            {
                _renderTargets![i].Dispose();
            }

            Guard.ThrowIfFailed(_swapChain.Get()->ResizeBuffers(
                _config.SwapChainBufferCount,
                ScreenData.Width,
                ScreenData.Height,
                _config.BackBufferFormat,
                (uint)(DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH)
            ));

            BackBufferIndex = _swapChain.Get()->GetCurrentBackBufferIndex();
        }

        /// <summary>
        /// Resize the render resources
        /// </summary>
        /// <param name="newScreenData">The <see cref="ScreenData"/> indicating the size to resize to</param>
        public static void Resize(ScreenData newScreenData)
        {
            ScreenData = newScreenData;

            GpuDispatchManager.Manager.BlockForGraphicsIdle();

            ResizeSwapChain();
            CreateRenderTargets();
        }

        internal static bool ReportLiveObjects(bool internalObjects = false)
        {
            Guard.ThrowIfFailed(_debugLayer.Get()->ReportLiveObjects(
                DXGI_DEBUG_ALL,
                DXGI_DEBUG_RLO_DETAIL | (internalObjects ? DXGI_DEBUG_RLO_ALL : DXGI_DEBUG_RLO_IGNORE_INTERNAL)
            ));

            return true;
        }

        /// <summary>
        /// Present the next frame
        /// </summary>
        public static void Present()
        {
            GpuDispatchManager.Manager.ExecuteSubmissions(insertFence: true);

            var hr = _swapChain.Get()->Present(_syncInterval, 0);
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

        private static void OnDeviceRemoved()
        {
            // we don't cache DRED state. we could, as this is the only class that should
            // change DRED state, but this isn't a fast path so there is no point
            if (_device.TryQueryInterface(out ComPtr<ID3D12DeviceRemovedExtendedData> dred))
            {
                using (dred)
                {
                    OnDeviceRemovedWithDred(dred.Get());
                }
            }

            Logger.LogError(
                "Device removed, no DRED present. Enable DEBUG or D3D12_DRED for enhanced device removed information");
        }

        private static void OnDeviceRemovedWithDred(ID3D12DeviceRemovedExtendedData* dred)
        {
            Debug.Assert(dred != null);

            D3D12_DRED_AUTO_BREADCRUMBS_OUTPUT breadcrumbs;
            D3D12_DRED_PAGE_FAULT_OUTPUT pageFault;

            Guard.ThrowIfFailed(dred->GetAutoBreadcrumbsOutput(&breadcrumbs));
            Guard.ThrowIfFailed(dred->GetPageFaultAllocationOutput(&pageFault));

            // TODO dred logging
        }

        private static void CreateRenderTargets()
        {
            CreateRtvAndDsvResources();
            CreateRtvAndDsvViews();
        }

        [Conditional("DEBUG")]
        [Conditional("D3D12_DEBUG_LAYER")]
        private static void EnableDebugLayer()
        {
            using ComPtr<ID3D12Debug> debugLayer = default;

            Guard.ThrowIfFailed(D3D12GetDebugInterface(
                debugLayer.Guid,
                ComPtr.GetVoidAddressOf(&debugLayer)
            ));

            debugLayer.Get()->EnableDebugLayer();

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
        [Conditional("DXGI_DRED")]
        private static void EnableDeviceRemovedExtendedDataLayer()
        {
            using ComPtr<ID3D12DeviceRemovedExtendedDataSettings> dredLayer = default;

            Guard.ThrowIfFailed(D3D12GetDebugInterface(
                dredLayer.Guid,
                ComPtr.GetVoidAddressOf(&dredLayer)
            ));

            dredLayer.Get()->SetAutoBreadcrumbsEnablement(D3D12_DRED_ENABLEMENT_FORCED_ON);
            dredLayer.Get()->SetPageFaultEnablement(D3D12_DRED_ENABLEMENT_FORCED_ON);
        }

        private static bool TryCreateNewDevice(
            Adapter adapter,
            D3D_FEATURE_LEVEL requiredLevel,
            out ComPtr<ID3D12Device> device
        )
        {
            ComPtr<ID3D12Device> p = default;
            bool success = SUCCEEDED(D3D12CreateDevice(
                (IUnknown*)adapter.UnderlyingAdapter,
                requiredLevel,
                p.Guid,
                (void**)&p
            ));

            device = p.Move();

            if (success)
            {
                SetObjectName(device.Get(), nameof(device));
            }

            return success;
        }

        private static void CreateRtvAndDsvResources()
        {
            _renderTargets = new GpuResource[BackBufferCount];

            for (uint i = 0; i < _renderTargets.Length; i++)
            {
                using ComPtr<ID3D12Resource> renderTarget = default;
                Guard.ThrowIfFailed(_swapChain.Get()->GetBuffer(
                    i,
                    renderTarget.Guid,
                    ComPtr.GetVoidAddressOf(&renderTarget)
                ));

                SetObjectName(renderTarget.Get(), $"BackBufferRenderTarget[{i}]");

                _renderTargets[i] = GpuResource.FromRenderTarget(renderTarget.Move());
            }

            _depthStencil?.Dispose();

            var desc = new GpuResourceDesc(
                GpuResourceFormat.DepthStencil(_config.DepthStencilFormat, ScreenData.Width, ScreenData.Height),
                GpuMemoryType.GpuOnly,
                D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_DEPTH_WRITE,
                allocFlags: GpuAllocFlags.ForceAllocateComitted,
                clearValue: new D3D12_CLEAR_VALUE(_config.DepthStencilFormat, 1.0f, 0)
            );

            _depthStencil = Allocator.Allocate(desc);

            SetObjectName(_depthStencil.UnderlyingResource, nameof(_depthStencil));
        }

        private static void CreateRtvAndDsvDescriptorHeaps()
        {
            _renderTargetViewHeap = DescriptorHeap.CreateRenderTargetViewHeap(Device, _config.SwapChainBufferCount);
            _depthStencilViewHeap = DescriptorHeap.CreateDepthStencilViewHeap(Device);
        }

        private static void CreateRtvAndDsvViews()
        {
            /* TODO */ //var desc = CreateRenderTargetViewDesc();
            var handle = _renderTargetViewHeap.FirstDescriptor;
            for (uint i = 0; i < _renderTargets.Length; i++)
            {
                Device->CreateRenderTargetView(_renderTargets[i].UnderlyingResource, null /* TODO */, handle.CpuHandle.Value);
                handle++;
            }

            var desc = new D3D12_DEPTH_STENCIL_VIEW_DESC
            {
                Format = _config.DepthStencilFormat,
                Flags = D3D12_DSV_FLAGS.D3D12_DSV_FLAG_NONE,
                ViewDimension = D3D12_DSV_DIMENSION.D3D12_DSV_DIMENSION_TEXTURE2D
            };

            Debug.Assert(_depthStencil.UnderlyingResource != null);

            Device->CreateDepthStencilView(
                _depthStencil.UnderlyingResource,
                &desc,
                DepthStencilView.CpuHandle.Value
            );


            SetObjectName(_depthStencil.UnderlyingResource, nameof(_depthStencil));
        }

        private static D3D12_RENDER_TARGET_VIEW_DESC CreateRenderTargetViewDesc()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IDisposable"/>
        public static void Dispose()
        {
            ReportLiveObjects();

            _device.Dispose();
            _swapChain.Dispose();
            _debugLayer.Dispose();
            GpuDispatchManager.Manager.Dispose();
        }
    }
}
