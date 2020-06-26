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
using Voltium.Core.Pipeline;
using Voltium.Core.Memory.GpuResources;
using Voltium.Core.D3D12;
using Buffer = Voltium.Core.Memory.GpuResources.Buffer;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Voltium.Core.Managers
{
    /// <summary>
    /// The top-level manager for application resources
    /// </summary>
    public unsafe class GraphicsDevice
    {
        private ComPtr<ID3D12Device> _device;
        private Adapter _adapter;
        private ComPtr<IDXGIDebug1> _debugLayer;
        private ComPtr<IDXGIFactory2> _factory;
        private ComPtr<IDXGISwapChain3> _swapChain;
        private uint _syncInterval;
        internal ulong TotalFramesRendered = 0;
        private Texture[] _renderTargets = null!;
        private Texture _depthStencil;
        internal uint BackBufferIndex;
        private GraphicalConfiguration _config = null!;
        private SynchronizedCommandQueue _graphicsQueue;
        //private GpuDispatchManager _dispatch = null!;

        /// <summary>
        /// The <see cref="ScreenData"/> for the output
        /// </summary>
        public ScreenData ScreenData { get; private set; }

        private GraphicsDevice() { }

        /// <summary>
        /// Initialize the single instance of this type
        /// </summary>
        public static GraphicsDevice Create(GraphicalConfiguration config, in ScreenData screenData)
        {
            var device = new GraphicsDevice();
            device.InternalCreate(config, in screenData);
            return device;
        }

        private void InternalCreate(GraphicalConfiguration config, in ScreenData screenData)
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

                if (TryCreateNewDevice(adapter, (D3D_FEATURE_LEVEL)config.RequiredFeatureLevel, out _device))
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
                    $"FATAL: Creation of ID3D12Device with feature level {config.RequiredFeatureLevel} failed");
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
            EnableDeviceRemovedExtendedDataLayer();

            // TODO WARP support

            Allocator = new GpuAllocator(this);

            InitializeDescriptorSizes();
            _graphicsQueue = new SynchronizedCommandQueue(this, ExecutionContext.Graphics);

            CreateSwapChain();
            CreateDescriptorHeaps();
            Resize(ScreenData);

            Viewport = new Viewport(0.0f, 0.0f, ScreenData.Width, ScreenData.Height, 0.0f, 1.0f);
            Scissor = new Rectangle(0, 0, (int)ScreenData.Width, (int)ScreenData.Height);

            BackBufferIndex = _swapChain.Get()->GetCurrentBackBufferIndex();
        }

        /// <summary>
        /// The default allocator for the device
        /// </summary>
        public GpuAllocator Allocator { get; private set; } = null!;

        private void InitializeDescriptorSizes()
        {
            ConstantBufferOrShaderResourceOrUnorderedAccessViewDescriptorSize =
                (int)DevicePointer->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);

            RenderTargetViewDescriptorSize =
                (int)DevicePointer->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);

            DepthStencilViewDescriptorSize =
                (int)DevicePointer->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_DSV);

            SamplerDescriptorSize = (int)DevicePointer->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER);
        }

        /// <summary>
        /// Gets the <see cref="ID3D12Device"/> used by this application
        /// </summary>
        internal ID3D12Device* DevicePointer => _device.Get();

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

        internal ComPtr<ID3D12Resource> CreateComittedResource(InternalAllocDesc desc)
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

        private DescriptorHeap _samplers;
        private DescriptorHeap _rtvs;
        private DescriptorHeap _dsvs;
        private DescriptorHeap _cbvSrvUav;

        private void CreateDescriptorHeaps()
        {
            _rtvs = DescriptorHeap.CreateRenderTargetViewHeap(this, _config.SwapChainBufferCount * 5);
            _dsvs = DescriptorHeap.CreateDepthStencilViewHeap(this, 50);
            _samplers = DescriptorHeap.CreateSamplerHeap(this, 1);
            _cbvSrvUav = DescriptorHeap.CreateConstantBufferShaderResourceUnorderedAccessViewHeap(this, 10);
        }


        /// <summary>
        /// Creates a shader resource view to a <see cref="Texture"/>
        /// </summary>
        /// <param name="resource">The <see cref="Texture"/> resource to create the view for</param>
        public DescriptorHandle CreateShaderResourceView(Texture resource)
        {
            var handle = _cbvSrvUav.GetNextHandle();

            DevicePointer->CreateShaderResourceView(resource.Resource.UnderlyingResource, null, handle.CpuHandle);

            return handle;
        }

        /// <summary>
        /// Creates a shader resource view to a <see cref="Texture"/>
        /// </summary>
        /// <param name="resource">The <see cref="Texture"/> resource to create the view for</param>
        /// <param name="desc">The <see cref="TextureShaderResourceViewDesc"/> describing the metadata used to create the view</param>
        public DescriptorHandle CreateShaderResourceView(Texture resource, in TextureShaderResourceViewDesc desc)
        {
            D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc;

            if (desc.IsMultiSampled)
            {
                ThrowHelper.ThrowNotImplementedException("TODO");
            }

            switch (resource.Dimension)
            {
                case TextureDimension.Tex1D:
                    srvDesc.Anonymous.Texture1D.MipLevels = desc.MipLevels;
                    srvDesc.Anonymous.Texture1D.MostDetailedMip = desc.MostDetailedMip;
                    srvDesc.Anonymous.Texture1D.ResourceMinLODClamp = desc.ResourceMinLODClamp;
                    srvDesc.ViewDimension = D3D12_SRV_DIMENSION.D3D12_SRV_DIMENSION_TEXTURE1D;
                    break;
                case TextureDimension.Tex2D:
                    srvDesc.Anonymous.Texture2D.MipLevels = desc.MipLevels;
                    srvDesc.Anonymous.Texture2D.MostDetailedMip = desc.MostDetailedMip;
                    srvDesc.Anonymous.Texture2D.ResourceMinLODClamp = desc.ResourceMinLODClamp;
                    srvDesc.Anonymous.Texture2D.PlaneSlice = desc.PlaneSlice;
                    srvDesc.ViewDimension = D3D12_SRV_DIMENSION.D3D12_SRV_DIMENSION_TEXTURE2D;
                    break;
                case TextureDimension.Tex3D:

                    srvDesc.Anonymous.Texture3D.MipLevels = desc.MipLevels;
                    srvDesc.Anonymous.Texture3D.MostDetailedMip = desc.MostDetailedMip;
                    srvDesc.Anonymous.Texture3D.ResourceMinLODClamp = desc.ResourceMinLODClamp;
                    srvDesc.ViewDimension = D3D12_SRV_DIMENSION.D3D12_SRV_DIMENSION_TEXTURE3D;
                    break;
            }

            srvDesc.Format = (DXGI_FORMAT)desc.Format;
            srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING; // TODO

            var handle = _cbvSrvUav.GetNextHandle();

            DevicePointer->CreateShaderResourceView(resource.Resource.UnderlyingResource, &srvDesc, handle.CpuHandle);

            return handle;
        }

        /// <summary>
        /// Creates a shader resource view to a <see cref="Buffer"/>
        /// </summary>
        /// <param name="resource">The <see cref="Buffer"/> resource to create the view for</param>
        /// <param name="desc">The <see cref="BufferShaderResourceViewDesc"/> describing the metadata used to create the view</param>
        public DescriptorHandle CreateShaderResourceView(Buffer resource, in BufferShaderResourceViewDesc desc)
        {
            Unsafe.SkipInit(out D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc);
            srvDesc.Format = (DXGI_FORMAT)desc.Format;
            srvDesc.ViewDimension = D3D12_SRV_DIMENSION.D3D12_SRV_DIMENSION_BUFFER;
            srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING; // TODO
            srvDesc.Anonymous.Buffer.FirstElement = desc.Offset;
            srvDesc.Anonymous.Buffer.Flags = desc.Raw ? D3D12_BUFFER_SRV_FLAGS.D3D12_BUFFER_SRV_FLAG_RAW : D3D12_BUFFER_SRV_FLAGS.D3D12_BUFFER_SRV_FLAG_NONE;
            srvDesc.Anonymous.Buffer.NumElements = desc.ElementCount;
            srvDesc.Anonymous.Buffer.StructureByteStride = desc.ElementStride;

            var handle = _cbvSrvUav.GetNextHandle();

            DevicePointer->CreateShaderResourceView(resource.Resource.UnderlyingResource, &srvDesc, _cbvSrvUav.GetNextHandle().CpuHandle);
            
            return handle;
        }

        /// <summary>
        /// Creates a shader resource view to a <see cref="Buffer"/>
        /// </summary>
        /// <param name="resource">The <see cref="Buffer"/> resource to create the view for</param>
        public DescriptorHandle CreateShaderResourceView(Buffer resource)
        {
            var handle = _cbvSrvUav.GetNextHandle();

            DevicePointer->CreateShaderResourceView(resource.Resource.UnderlyingResource, null, handle.CpuHandle);

            return handle;
        }

        /// <summary>
        /// Creates a render target view to a <see cref="Texture"/>
        /// </summary>
        /// <param name="resource">The <see cref="Texture"/> resource to create the view for</param>
        /// <param name="desc">The <see cref="TextureShaderResourceViewDesc"/> describing the metadata used to create the view</param>
        public DescriptorHandle CreateRenderTargetView(Texture resource, in TextureRenderTargetViewDesc desc)
        {
            D3D12_RENDER_TARGET_VIEW_DESC rtvDesc;

            if (desc.IsMultiSampled)
            {
                switch (resource.Dimension)
                {
                    case TextureDimension.Tex1D:
                        ThrowHelper.ThrowArgumentException("Cannot multisample 1D render target view");
                        break;
                    case TextureDimension.Tex2D:
                        rtvDesc.ViewDimension = D3D12_RTV_DIMENSION.D3D12_RTV_DIMENSION_TEXTURE2DMS;
                        break;
                    case TextureDimension.Tex3D:
                        ThrowHelper.ThrowArgumentException("Cannot multisample 3D render target view");
                        break;
                }
            }
            else
            {
                switch (resource.Dimension)
                {
                    case TextureDimension.Tex1D:
                        rtvDesc.Anonymous.Texture1D.MipSlice = desc.MipIndex;
                        rtvDesc.ViewDimension = D3D12_RTV_DIMENSION.D3D12_RTV_DIMENSION_TEXTURE1D;
                        break;
                    case TextureDimension.Tex2D:
                        rtvDesc.Anonymous.Texture2D.MipSlice = desc.MipIndex;
                        rtvDesc.Anonymous.Texture2D.PlaneSlice = desc.PlaneSlice;
                        rtvDesc.ViewDimension = D3D12_RTV_DIMENSION.D3D12_RTV_DIMENSION_TEXTURE2D;
                        break;
                    case TextureDimension.Tex3D:
                        rtvDesc.Anonymous.Texture3D.MipSlice = desc.MipIndex;
                        rtvDesc.ViewDimension = D3D12_RTV_DIMENSION.D3D12_RTV_DIMENSION_TEXTURE3D;
                        break;
                }
            }

            rtvDesc.Format = (DXGI_FORMAT)desc.Format;

            var handle = _rtvs.GetNextHandle();

            DevicePointer->CreateRenderTargetView(resource.Resource.UnderlyingResource, &rtvDesc, handle.CpuHandle);

            return handle;
        }

        /// <summary>
        /// Creates a render target view to a <see cref="Texture"/>
        /// </summary>
        /// <param name="resource">The <see cref="Texture"/> resource to create the view for</param>
        public DescriptorHandle CreateRenderTargetView(Texture resource)
        {
            var handle = _rtvs.GetNextHandle();

            DevicePointer->CreateRenderTargetView(resource.Resource.UnderlyingResource, null, handle.CpuHandle);

            return handle;
        }

        /// <summary>
        /// Creates a depth stencil view to a <see cref="Texture"/>
        /// </summary>
        /// <param name="resource">The <see cref="Texture"/> resource to create the view for</param>
        /// <param name="desc">The <see cref="TextureShaderResourceViewDesc"/> describing the metadata used to create the view</param>
        public DescriptorHandle CreateDepthStencilView(Texture resource, in TextureDepthStencilViewDesc desc)
        {
            D3D12_DEPTH_STENCIL_VIEW_DESC dsvDesc;

            if (desc.IsMultiSampled)
            {
                switch (resource.Dimension)
                {
                    case TextureDimension.Tex1D:
                        ThrowHelper.ThrowArgumentException("Cannot multisample 1D depth stencil view");
                        break;
                    case TextureDimension.Tex2D:
                        dsvDesc.ViewDimension = D3D12_DSV_DIMENSION.D3D12_DSV_DIMENSION_TEXTURE2DMS;
                        break;
                    case TextureDimension.Tex3D:
                        ThrowHelper.ThrowArgumentException("Cannot have 3D depth stencil view");
                        break;
                }
            }
            else
            {
                switch (resource.Dimension)
                {
                    case TextureDimension.Tex1D:
                        dsvDesc.Anonymous.Texture1D.MipSlice = desc.MipIndex;
                        dsvDesc.ViewDimension = D3D12_DSV_DIMENSION.D3D12_DSV_DIMENSION_TEXTURE1D;
                        break;
                    case TextureDimension.Tex2D:
                        dsvDesc.Anonymous.Texture2D.MipSlice = desc.MipIndex;
                        dsvDesc.ViewDimension = D3D12_DSV_DIMENSION.D3D12_DSV_DIMENSION_TEXTURE2D;
                        break;
                    case TextureDimension.Tex3D:
                        ThrowHelper.ThrowArgumentException("Cannot have 3D depth stencil view");
                        break;
                }
            }

            dsvDesc.Format = (DXGI_FORMAT)desc.Format;

            var handle = _dsvs.GetNextHandle();

            DevicePointer->CreateDepthStencilView(resource.GetResourcePointer(), &dsvDesc, handle.CpuHandle);

            return handle;
        }

        /// <summary>
        /// Creates a depth stencil view to a <see cref="Texture"/>
        /// </summary>
        /// <param name="resource">The <see cref="Texture"/> resource to create the view for</param>
        public DescriptorHandle CreateDepthStencilView(Texture resource)
        {
            var handle = _dsvs.GetNextHandle();

            DevicePointer->CreateDepthStencilView(resource.GetResourcePointer(), null, handle.CpuHandle);

            return handle;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public CopyContext BeginCopyContext()
        {
            var ctx = BeginGraphicsContext();
            return ctx.AsCopyContext();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pso"></param>
        /// <returns></returns>
        public ComputeContext BeginComputeContext(PipelineStateObject? pso = null)
        {
            Debug.Assert(pso is not GraphicsPso);
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

            if (pso is ComputePso)
            {
                _list->SetComputeRootSignature(pso.GetRootSig());
            }
            else if (pso is GraphicsPso)
            {
                _list->SetGraphicsRootSignature(pso is null ? null : pso.GetRootSig());
            }

            SetDescriptorHeaps(_list);

            return new GraphicsContext(this, _list, _allocator);

            //return _dispatch.BeginGraphicsContext(pso);
        }


        private void SetDescriptorHeaps(ID3D12GraphicsCommandList* list)
        {
            const int numHeaps = 2;
            var heaps = stackalloc ID3D12DescriptorHeap*[numHeaps] { _cbvSrvUav.GetHeap(), _samplers.GetHeap() };

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
            var list = context.List.Move();
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

        private DescriptorHandle[] _rtv = null!;
        private DescriptorHandle _dsv;

        /// <summary>
        /// The render target view for the current frame
        /// </summary>
        public DescriptorHandle RenderTargetView => _rtv[(int)BackBufferIndex];

        /// <summary>
        /// The depth stencil view for the current frame
        /// </summary>
        public DescriptorHandle DepthStencilView => _dsv;

        /// <summary>
        /// The <see cref="GpuResource"/> for the current render target resource
        /// </summary>
        public Texture BackBuffer => _renderTargets[BackBufferIndex];

        /// <summary>
        /// The <see cref="GpuResource"/> for the current depth stencil resource
        /// </summary>
        public Texture DepthStencil => _depthStencil;

        /// <summary>
        /// The number of CPU buffered resources
        /// </summary>
        public uint BackBufferCount => _config.SwapChainBufferCount;

        /// <summary>
        /// The size of a descriptor of type <see cref="D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV"/>
        /// </summary>
        // why the fuck did i call it this
        public int ConstantBufferOrShaderResourceOrUnorderedAccessViewDescriptorSize { get; private set; }

        /// <summary>
        /// The size of a descriptor of type <see cref="D3D12_DESCRIPTOR_HEAP_TYPE_RTV"/>
        /// </summary>
        public int RenderTargetViewDescriptorSize { get; private set; }

        /// <summary>
        /// The size of a descriptor of type <see cref="D3D12_DESCRIPTOR_HEAP_TYPE_DSV"/>
        /// </summary>
        public int DepthStencilViewDescriptorSize { get; private set; }

        /// <summary>
        /// The size of a descriptor of type <see cref="D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER"/>
        /// </summary>
        public int SamplerDescriptorSize { get; private set; }

        /// <summary>
        /// Gets the size of a descriptor for a given <see cref="D3D12_DESCRIPTOR_HEAP_TYPE"/>
        /// </summary>
        /// <param name="type">The type of the descriptor</param>
        /// <returns>The size of the descriptor, in bytes</returns>
        public uint GetDescriptorSizeForType(D3D12_DESCRIPTOR_HEAP_TYPE type)
        {
            Debug.Assert(type >= 0 && type < D3D12_DESCRIPTOR_HEAP_TYPE_NUM_TYPES);

            return (uint)(type switch
            {
                D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV => ConstantBufferOrShaderResourceOrUnorderedAccessViewDescriptorSize,
                D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER => SamplerDescriptorSize,
                D3D12_DESCRIPTOR_HEAP_TYPE_RTV => RenderTargetViewDescriptorSize,
                D3D12_DESCRIPTOR_HEAP_TYPE_DSV => DepthStencilViewDescriptorSize,
                _ => 0
            });
        }

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
                Flags = (uint)DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH,
                Format = (DXGI_FORMAT)_config.BackBufferFormat,
                Height = ScreenData.Height,
                Width = ScreenData.Width,
                SampleDesc = new DXGI_SAMPLE_DESC(_config.MultiSamplingStrategy.SampleCount, _config.MultiSamplingStrategy.QualityLevel),
                Scaling = DXGI_SCALING.DXGI_SCALING_NONE,
                Stereo = FALSE, // stereoscopic rendering, 2 images, e.g VR or 3D holo
                SwapEffect = DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_FLIP_DISCARD
            };

            using ComPtr<IDXGISwapChain1> swapChain = default;
            Guard.ThrowIfFailed(_factory.Get()->CreateSwapChainForHwnd(
                (IUnknown*)_graphicsQueue.GetQueue(),
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

        private void ResizeSwapChain()
        {
            for (var i = 0; i < (_renderTargets?.Length ?? 0); i++)
            {
                _renderTargets![i].Dispose();
            }

            Guard.ThrowIfFailed(_swapChain.Get()->ResizeBuffers(
                _config.SwapChainBufferCount,
                ScreenData.Width,
                ScreenData.Height,
                (DXGI_FORMAT)_config.BackBufferFormat,
                (uint)DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH
            ));

            BackBufferIndex = _swapChain.Get()->GetCurrentBackBufferIndex();
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
            CreateRenderTargets();
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

        private void CreateRenderTargets()
        {
            CreateRtvAndDsvResources();
            CreateRtvAndDsvViews();
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
                (IUnknown*)adapter.UnderlyingAdapter,
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

        private void CreateRtvAndDsvResources()
        {
            _renderTargets = new Texture[BackBufferCount];

            for (uint i = 0; i < _renderTargets.Length; i++)
            {
                _renderTargets[i].Dispose();
                _renderTargets[i] = Texture.FromBackBuffer((IDXGISwapChain*)_swapChain.Get(), i);
                SetObjectName(_renderTargets[i].Resource.UnderlyingResource, $"Render target #{i}");
            }

            _depthStencil.Dispose();

            var texDesc = new TextureDesc
            {
                Format = _config.DepthStencilFormat,
                Dimension = TextureDimension.Tex2D,
                Width = ScreenData.Width,
                Height = ScreenData.Height,
                DepthOrArraySize = 1,
                ClearValue = TextureClearValue.CreateForDepthStencil(1.0f, 0),
                ResourceFlags = ResourceFlags.AllowDepthStencil
            };

            _depthStencil = Allocator.AllocateTexture(texDesc, ResourceState.DepthWrite);

            SetObjectName(_depthStencil.Resource.UnderlyingResource, nameof(_depthStencil));
        }

        private void CreateRtvAndDsvViews()
        {
            /* TODO */ //var desc = CreateRenderTargetViewDesc();
            _rtv = new DescriptorHandle[_renderTargets.Length];

            for (uint i = 0; i < _renderTargets.Length; i++)
            {
                _rtv[i] = CreateRenderTargetView(_renderTargets[i]);
            }

            //var dsv = new TextureDepthStencilViewDesc
            //{
            //    Format = _config.DepthStencilFormat,
            //    IsMultiSampled = false,
            //    MipIndex = 0,
            //    PlaneSlice = 0
            //};

            //_dsv = CreateDepthStencilView(_depthStencil, dsv);

            //SetObjectName(_depthStencil.Resource.UnderlyingResource, nameof(_depthStencil));
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
