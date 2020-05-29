using System;
using System.Diagnostics;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.GpuResources;
using static TerraFX.Interop.D3D12_DESCRIPTOR_HEAP_TYPE;
using static TerraFX.Interop.D3D12_RESOURCE_FLAGS;

namespace Voltium.Core.Managers
{
    /// <summary>
    /// The type in charge of managing per-frame resources
    /// </summary>
    public sealed unsafe class ResourceManager
    {
        // TODO data-orientated design, SoA vs Aos vs AoSoA

        private DescriptorHeap _renderTargetViewHeap;
        private DescriptorHeap _depthStencilViewHeap;

        private GpuResource[] _renderTargets = null!;
        private GpuResource _depthStencil = null!;

        private GpuAllocator _allocator = null!;

        private uint _resourceIndex;
        private uint _resourceCount;

        /// <summary>
        /// Move to the next frame's set of resources
        /// </summary>
        public void MoveToNextFrame() => _resourceIndex = (_resourceIndex + 1) % _resourceCount;

        /// <summary>
        /// The render target view for the current frame
        /// </summary>
        public DescriptorHandle RenderTargetView => _renderTargetViewHeap.FirstDescriptor + (int)_resourceIndex;

        /// <summary>
        /// The depth stencil view for the current frame
        /// </summary>
        public DescriptorHandle DepthStencilView => _depthStencilViewHeap.FirstDescriptor;

        /// <summary>
        /// The <see cref="GpuResource"/> for the current render target resource
        /// </summary>
        public GpuResource RenderTarget => _renderTargets[_resourceIndex];

        /// <summary>
        /// The <see cref="GpuResource"/> for the current depth stencil resource
        /// </summary>
        public GpuResource DepthStencil => _depthStencil;

        private static bool _initialized;
        private static readonly object Lock = new object();

        /// <summary>
        /// The single instance of this type. You must call <see cref="Initialize"/> before retrieving the instance
        /// </summary>
        public static ResourceManager Manager
        {
            get
            {
                Guard.Initialized(_initialized);

                return Value;
            }
        }

        private static readonly ResourceManager Value = new ResourceManager();

        private ResourceManager() { }

        internal static void Initialize(
            DeviceManager manager,
            ID3D12Device* device,
            IDXGISwapChain3* swapChain,
            GraphicalConfiguration config,
            ScreenData screenData
        )
        {
            // TODO could probably use CAS/System.Threading.LazyInitializer
            lock (Lock)
            {
                Debug.Assert(!_initialized);

                _initialized = true;
                Value.CoreInitialize(manager, device, swapChain, config, screenData);
            }
        }

        private void CoreInitialize(
            DeviceManager manager,
            ID3D12Device* device,
            IDXGISwapChain3* swapChain,
            GraphicalConfiguration config,
            ScreenData screenData
        )
        {
            _resourceCount = config.BufferCount;
            _resourceIndex = swapChain->GetCurrentBackBufferIndex();
            _allocator = manager.Allocator;

            CreateRtvAndDsvResources(swapChain, config, screenData);
            CreateRtvAndDsvDescriptorHeaps(device, config);
            CreateRtvAndDsvViews(device, config);
        }

        private void CreateRtvAndDsvResources(
            IDXGISwapChain3* swapChain,
            GraphicalConfiguration config,
            in ScreenData screenData
        )
        {
            _renderTargets = new GpuResource[DeviceManager.Manager.FrameCount];

            for (uint i = 0; i < _renderTargets.Length; i++)
            {
                using ComPtr<ID3D12Resource> renderTarget = default;
                Guard.ThrowIfFailed(swapChain->GetBuffer(
                    i,
                    renderTarget.Guid,
                    ComPtr.GetVoidAddressOf(&renderTarget)
                ));

                DirectXHelpers.SetObjectName(renderTarget.Get(), $"_renderTargets[{i}]");

                _renderTargets[i] = GpuResource.FromRenderTarget(renderTarget.Move());
            }

            var desc = new GpuResourceDesc(
                GpuResourceFormat.DepthStencil(config.DepthStencilFormat, screenData.Width, screenData.Height),
                GpuMemoryType.GpuOnly,
                D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_DEPTH_WRITE,
                allocFlags: GpuAllocFlags.ForceAllocateComitted,
                clearValue: new D3D12_CLEAR_VALUE(config.DepthStencilFormat, 1.0f, 0)
            );

            _depthStencil = _allocator.Allocate(desc);

            DirectXHelpers.SetObjectName(_depthStencil.UnderlyingResource, nameof(_depthStencil));
        }

        private void CreateRtvAndDsvDescriptorHeaps(
            ID3D12Device* device,
            GraphicalConfiguration config
        )
        {
            _renderTargetViewHeap = DescriptorHeap.CreateRenderTargetViewHeap(device, config.BufferCount);
            _depthStencilViewHeap = DescriptorHeap.CreateDepthStencilViewHeap(device);
        }

        private void CreateRtvAndDsvViews(
            ID3D12Device* device,
            GraphicalConfiguration config
        )
        {
            Debug.Assert(device != null);

            /* TODO */ //var desc = CreateRenderTargetViewDesc();
            var handle = RenderTargetView;
            for (uint i = 0; i < _renderTargets.Length; i++)
            {
                device->CreateRenderTargetView(_renderTargets[i].UnderlyingResource, null /* TODO */, handle.CpuHandle.Value);
                handle++;
            }

            var desc = new D3D12_DEPTH_STENCIL_VIEW_DESC
            {
                Format = config.DepthStencilFormat,
                Flags = D3D12_DSV_FLAGS.D3D12_DSV_FLAG_NONE,
                ViewDimension = D3D12_DSV_DIMENSION.D3D12_DSV_DIMENSION_TEXTURE2D
            };

            Debug.Assert(_depthStencil.UnderlyingResource != null);

            device->CreateDepthStencilView(
                _depthStencil.UnderlyingResource,
                &desc,
                DepthStencilView.CpuHandle.Value
            );


            DirectXHelpers.SetObjectName(_depthStencil.UnderlyingResource, nameof(_depthStencil));
        }

        private D3D12_RENDER_TARGET_VIEW_DESC CreateRenderTargetViewDesc()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GpuDispatchManager.Manager.BlockForGpuIdle();

            _renderTargetViewHeap.Dispose();
            _depthStencilViewHeap.Dispose();

            _depthStencil.Dispose();

            for (var i = 0; i < _renderTargets.Length; i++)
            {
                _renderTargets[i].Dispose();
            }
        }
    }
}
