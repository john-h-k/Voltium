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
using Voltium.Common.Debugging;

namespace Voltium.Core.Managers
{
    /// <summary>
    /// The top-level manager for application resources
    /// </summary>
    public sealed unsafe class DeviceManager : IDisposable
    {
        private ComPtr<ID3D12Device> _device;
        private ComPtr<IDXGIDebug1> _debugLayer;
        private ComPtr<IDXGISwapChain3> _swapChain;

        /// <summary>
        /// The viewport for the entire screen
        /// </summary>
        public Viewport Viewport;


        /// <summary>
        /// The scissor for the entire screen
        /// </summary>
        public Rectangle Scissor;

        private GraphicalConfiguration _config = null!;
        private ScreenData _screenData;
        private uint _syncInterval; // cache

        internal ulong TotalFramesRendered = 0;

        /// <summary>
        /// The number of CPU buffered resources
        /// </summary>
        public uint FrameCount => _config.BufferCount;

        private static bool _initialized;
        private static readonly object Lock = new object();

        /// <summary>
        /// The single instance of this type. You must call <see cref="Initialize"/> before retrieving the instance
        /// </summary>
        public static DeviceManager Manager
        {
            get
            {
                Guard.Initialized(_initialized);

                return Value;
            }
        }

        private static readonly DeviceManager Value = new DeviceManager();

        private DeviceManager()
        {
        }

        /// <summary>
        /// Initialize the single instance of this type
        /// </summary>
        public static void Initialize(GraphicalConfiguration config, in ScreenData screenData)
        {
            lock (Lock)
            {
                _initialized = true;

                Value.CoreInitialize(config, in screenData);
            }
        }

        private void CoreInitialize(GraphicalConfiguration config, in ScreenData screenData)
        {
            Guard.NotNull(config);

            _config = config;
            _screenData = screenData;

            _config = config;
            _syncInterval = _config.VSyncCount;
            _screenData = screenData;

            EnableDebugLayer();
            //EnableDeviceRemovedExtendedDataLayer();

            using ComPtr<IDXGIFactory2> factory = default;

            Guard.ThrowIfFailed(CreateDXGIFactory(
                factory.Guid,
                ComPtr.GetVoidAddressOf(&factory)
            ));

            foreach (Adapter adapter in Adapter.EnumerateAdapters(factory))
            {
                if (adapter.IsSoftware)
                {
                    continue;
                }

                if (TryCreateNewDevice(adapter, config.RequiredDirect3DLevel, out _device))
                {
                    Logger.LogInformation("New ID3D12Device created: {}", adapter);
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

            // TODO WARP support

            Allocator = new GpuAllocator(_device.Copy());

            InitializeDescriptorSizes();
            GpuDispatchManager.Initialize(_device, _config);
            CreateSwapChain(factory.Get());
            InitializerGlobalManagers();

            Viewport = new Viewport(0.0f, 0.0f, _screenData.Width, _screenData.Height, 0.0f, 1.0f);
            Scissor = new Rectangle(0, 0, (int)_screenData.Width, (int)_screenData.Height);
        }

        /// <summary>
        /// The default allocator for the device
        /// </summary>
        public GpuAllocator Allocator { get; private set; } = null!;

        private void InitializeDescriptorSizes()
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
        public ID3D12Device* Device => _device.Get();

        //private uint _resourceBufferCount = 3;
        private uint _currentFrame = 0;

        /// <summary>
        /// The size of a descriptor of type <see cref="D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV"/>
        /// </summary>
        // why the fuck did i call it this
        public int ConstantBufferOrShaderResourceOrUnorderedAccessViewDescriptorSize { get; private set; }

        /// <summary>
        /// The size of a descriptor of type <see cref="D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_RTV"/>
        /// </summary>
        public int RenderTargetViewDescriptorSize { get; private set; }

        /// <summary>
        /// The size of a descriptor of type <see cref="D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_DSV"/>
        /// </summary>
        public int DepthStencilViewDescriptorSize { get; private set; }

        /// <summary>
        /// The size of a descriptor of type <see cref="D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER"/>
        /// </summary>
        public int SamplerDescriptorSize { get; private set; }

        /// <summary>
        /// Gets the size of a descriptor for a given <see cref="D3D12_DESCRIPTOR_HEAP_TYPE"/>
        /// </summary>
        /// <param name="type">The type of the descriptor</param>
        /// <returns>The size of the descriptor, in bytes</returns>
        public int GetDescriptorSizeForType(D3D12_DESCRIPTOR_HEAP_TYPE type)
        {
            Debug.Assert(type > 0 && type < D3D12_DESCRIPTOR_HEAP_TYPE_NUM_TYPES);

            return type switch
            {
                D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV =>
                ConstantBufferOrShaderResourceOrUnorderedAccessViewDescriptorSize,
                D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER => SamplerDescriptorSize,
                D3D12_DESCRIPTOR_HEAP_TYPE_RTV => RenderTargetViewDescriptorSize,
                D3D12_DESCRIPTOR_HEAP_TYPE_DSV => DepthStencilViewDescriptorSize,
                _ => 0
            };
        }

        private void CreateSwapChain(IDXGIFactory2* factory)
        {
            var desc = new DXGI_SWAP_CHAIN_DESC1
            {
                AlphaMode = DXGI_ALPHA_MODE.DXGI_ALPHA_MODE_IGNORE, // todo document
                BufferCount = _config.BufferCount,
                BufferUsage = (int)DXGI_USAGE_RENDER_TARGET_OUTPUT, // this is the output chain
                Flags = (uint)DXGI_SWAP_CHAIN_FLAG.DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH,
                Format = _config.BackBufferFormat,
                Height = _screenData.Height,
                Width = _screenData.Width,
                SampleDesc = _config.MultiSamplingStrategy,
                Scaling = _config.ScalingStrategy,
                Stereo = Windows.FALSE, // stereoscopic rendering, 2 images used to make it look 3D
                SwapEffect = _config.SwapEffect
            };

            var fullscreenDesc = new DXGI_SWAP_CHAIN_FULLSCREEN_DESC
            {
                RefreshRate = new DXGI_RATIONAL(0, 0),
                Scaling = _config.FullscreenScalingStrategy,
                ScanlineOrdering = _config.ScanlineOrdering,
                Windowed = _config.ForceFullscreenAsWindowed ? Windows.TRUE : Windows.FALSE
            };

            using ComPtr<IDXGISwapChain1> swapChain = default;
            Guard.ThrowIfFailed(factory->CreateSwapChainForHwnd(
                (IUnknown*)GpuDispatchManager.Manager.GetGraphicsQueue(),
                _screenData.Handle,
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

        internal bool ReportLiveObjects(bool internalObjects = false)
        {
            Guard.ThrowIfFailed(_debugLayer.Get()->ReportLiveObjects(
                Windows.DXGI_DEBUG_ALL,
                DXGI_DEBUG_RLO_DETAIL | (internalObjects ? DXGI_DEBUG_RLO_ALL : DXGI_DEBUG_RLO_IGNORE_INTERNAL)
            ));

            return true;
        }

        /// <summary>
        /// Present the next frame
        /// </summary>
        public void Present()
        {
            GpuDispatchManager.Manager.ExecuteSubmissions(insertFence: true);

            var hr = _swapChain.Get()->Present(_syncInterval, 0);
            TotalFramesRendered++;

            if (hr == Windows.DXGI_ERROR_DEVICE_REMOVED || hr == Windows.DXGI_ERROR_DEVICE_RESET)
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
            // i know some people dislike this using (var) pattern but personally i find it clear -↓↓↓↓↓
            if (_device.TryQueryInterface(out ComPtr<ID3D12DeviceRemovedExtendedData> dred))
                using (dred)
                {
                    OnDeviceRemovedWithDred(dred.Get());
                }

            Logger.LogError(
                "Device removed, no DRED present. Enable DEBUG or D3D12_DRED for enhanced device removed information");
        }

        private void OnDeviceRemovedWithDred(ID3D12DeviceRemovedExtendedData* dred)
        {
            Debug.Assert(dred != null);

            D3D12_DRED_AUTO_BREADCRUMBS_OUTPUT breadcrumbs;
            D3D12_DRED_PAGE_FAULT_OUTPUT pageFault;

            dred->GetAutoBreadcrumbsOutput(&breadcrumbs);
            dred->GetPageFaultAllocationOutput(&pageFault);

            // TODO dred logging
        }

        private void MoveToNextFrame()
        {
            _currentFrame++;
            GpuDispatchManager.Manager.MoveToNextFrame();
            ResourceManager.Manager.MoveToNextFrame();
        }

        private void InitializerGlobalManagers()
        {
            ResourceManager.Initialize(this, _device.Get(), _swapChain.Get(), _config, _screenData);
        }

        [Conditional("DEBUG")]
        [Conditional("D3D12_DEBUG_LAYER")]
        private void EnableDebugLayer()
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
        private void EnableDeviceRemovedExtendedDataLayer()
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

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose()
        {
            ReportLiveObjects();

            _device.Dispose();
            _swapChain.Dispose();
            _debugLayer.Dispose();
            ResourceManager.Manager.Dispose();
            GpuDispatchManager.Manager.Dispose();
        }

        /// <summary>
        /// no
        /// </summary>
        ~DeviceManager()
        {
            Dispose();
        }
    }
}
