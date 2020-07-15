using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Memory;
using Voltium.Core.Infrastructure;
using Voltium.Core.Pipeline;
using ZLogger;
using static TerraFX.Interop.Windows;

namespace Voltium.Core.Devices
{
    internal struct D3D11on12
    {
        public ComPtr<ID3D11On12Device> _device;
        public ComPtr<ID3D11DeviceContext> _deviceContext;

        public ComPtr<ID2D1Device> _d2dDevice;
        public ComPtr<ID2D1DeviceContext> _d2dDeviceContext;
    }


    /// <summary>
    /// The top-level manager for application resources
    /// </summary>
    public unsafe partial class GraphicsDevice : ComputeDevice
    {
        private GraphicalConfiguration _config = null!;
        private SynchronizedCommandQueue _graphicsQueue;

        private Adapter _adapter;

        /// <summary>
        /// The <see cref="Adapter"/> this device uses
        /// </summary>
        public Adapter Adapter => _adapter;

        internal ulong TotalFramesRendered = 0;
        private GpuDispatchManager _dispatch = null!;

        private bool _disposed;

        /// <summary>
        /// 
        /// </summary>
        public void Idle()
        {
            _graphicsQueue.GetSynchronizerForIdle().Block();
        }

        private GraphicsDevice(GraphicalConfiguration config, Adapter? adapter) : base(config.RequiredFeatureLevel, config.DebugLayerConfiguration, adapter)
        {
            Guard.NotNull(config);

            _config = config;

            ulong frequency;
            QueryPerformanceFrequency((LARGE_INTEGER*) /* <- can we do that? */ &frequency);
            _cpuFrequency = frequency;


            Allocator = new GpuAllocator(this);
            PipelineManager = new PipelineManager(this);

            _graphicsQueue = new SynchronizedCommandQueue(this, ExecutionContext.Graphics);

            CreateSwapChain();
            CreateDescriptorHeaps();
        }

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

        // Output requires this for swapchain creation
        internal IUnknown* GetGraphicsQueue() => (IUnknown*)_graphicsQueue.GetQueue();

        private ulong _cpuFrequency;

        /// <summary>
        /// Create a new <see cref="GraphicsDevice"/> with an output to a WinRT ICoreWindow
        /// </summary>
        /// <param name="adapter">The <see cref="Adapter"/> to create the device on, or <see langword="null"/> to use the default adapter</param>
        /// <param name="config">The <see cref="GraphicalConfiguration"/> to create the device with</param>
        /// <returns>A new <see cref="GraphicsDevice"/></returns>
        public static GraphicsDevice Create(Adapter? adapter, GraphicalConfiguration config)
        {
            return new GraphicsDevice(config, adapter);
        }

        private protected override void QueryFeaturesOnCreation()
        {
            base.QueryFeaturesOnCreation();

            uint CheckMsaaSupport(uint sampleCount)
            {
                D3D12_FEATURE_DATA_MULTISAMPLE_QUALITY_LEVELS desc = default;
                desc.SampleCount = sampleCount;
                desc.Format = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM;

                QueryFeatureSupport(D3D12_FEATURE.D3D12_FEATURE_MULTISAMPLE_QUALITY_LEVELS, ref desc);

                return desc.NumQualityLevels;
            }

            _sampleCounts.X1 = 1;
            _sampleCounts.X2 = CheckMsaaSupport(2);
            _sampleCounts.X4 = CheckMsaaSupport(4);
            _sampleCounts.X8 = CheckMsaaSupport(8);
            _sampleCounts.X16 = CheckMsaaSupport(16);
            _sampleCounts.X32 = CheckMsaaSupport(32);
        }

        private struct SampleCounts
        {
            public uint X1;
            public uint X2;
            public uint X4;
            public uint X8;
            public uint X16;
            public uint X32;
            public uint X64;
            public uint GetQualityLevelsForSampleCount(uint sampleCount)
                => Unsafe.Add(ref X1, BitOperations.Log2(sampleCount));
        }

        private SampleCounts _sampleCounts;

        /// <summary>
        /// Checks whether multisampling for a given sample count is supported, and if so, how many quality levels are present
        /// </summary>
        /// <param name="sampleCount">The number of samples to check support for</param>
        /// <param name="highestQualityLevel">If the return value is <see langword="true"/>, the number of quality levels supported for <paramref name="sampleCount"/></param>
        /// <returns><see langword="true"/> if multisampling for <paramref name="sampleCount"/> is supported, else <see langword="false"/></returns>
        public bool IsSampleCountSupported(uint sampleCount, out MultisamplingDesc highestQualityLevel)
        {
            highestQualityLevel = new (sampleCount, _sampleCounts.GetQualityLevelsForSampleCount(sampleCount) - 1);

            // When unsupported, num quality levels = 0, which wil underflow to maxvalue in the above calc
            return highestQualityLevel.QualityLevel != 0xFFFFFFFF;
        }

        /// <summary>
        /// Returns the highest supporte multisampling count with the highest quality level supported
        /// </summary>
        /// <returns>The highest supported multisampling count with the highest quality level supported</returns>
        public MultisamplingDesc HighestSupportedMsaa()
        {
            MultisamplingDesc best;
            if (IsSampleCountSupported(16, out best))
            {
                return best;
            }
            if (IsSampleCountSupported(8, out best))
            {
                return best;
            }
            if (IsSampleCountSupported(4, out best))
            {
                return best;
            }
            if (IsSampleCountSupported(2, out best))
            {
                return best;
            }

            return MultisamplingDesc.None;
        }


        internal ComPtr<ID3D12RootSignature> CreateRootSignature(uint nodeMask, void* pSignature, uint signatureLength)
        {
            using ComPtr<ID3D12RootSignature> rootSig = default;
            Guard.ThrowIfFailed(DevicePointer->CreateRootSignature(
                nodeMask,
                pSignature,
                signatureLength,
                rootSig.Iid,
                ComPtr.GetVoidAddressOf(&rootSig)
            ));

            return rootSig.Move();
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
        public new ComputeContext BeginComputeContext(ComputePipelineStateObject? pso = null)
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
            if (pso is ComputePipelineStateObject)
            {
                _list->SetComputeRootSignature(rootSig);
            }
            else if (pso is GraphicsPipelineStateObject)
            {
                _list->SetGraphicsRootSignature(rootSig);
            }

            SetDefaultDescriptorHeaps(_list);


            return new GraphicsContext(new(this, new(_list), new(_allocator)));

            //return _dispatch.BeginGraphicsContext(pso);
        }

        private TimeSpan _start;

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

        private bool _recFrame = true;

        /// <summary>
        /// Submit a set of recorded commands to the list
        /// </summary>
        /// <param name="context">The commands to submit for execution</param>
        internal void End(ref GpuContext context)
        {
            ComPtr<ID3D12GraphicsCommandList> list = new(context.List);
            list.Get()->Close();
            _graphicsQueue.GetQueue()->ExecuteCommandLists(1, (ID3D12CommandList**)&list);

            QueryTimestamp(ExecutionContext.Graphics, out var start);
            _graphicsQueue.GetSynchronizerForIdle().Block();
            QueryTimestamp(ExecutionContext.Graphics, out var end);

            if (_recFrame)
            {
                Console.WriteLine((end - start).TotalMilliseconds);
                _recFrame = false;
            }
            //return _dispatch.End(context);
        }

        /// <summary>
        /// Execute all submitted command lists
        /// </summary>
        public void Execute()
        {

        }

        /// <summary>
        /// Move to the next frame's set of resources
        /// </summary>
        public void MoveToNextFrame()
        {
            //_graphicsQueue.MoveToNextFrame();
            _graphicsQueue.GetSynchronizerForIdle().Block();
            _rtvs.ResetHeap();
            _dsvs.ResetHeap();
        }

        internal void ReleaseResourceAtFrameEnd(GpuResource resource)
        {

        }

        private void CreateSwapChain()
        {

            // TODO rotation
        }

        private void ResizeSwapChain()
        {
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

            LogHelper.Logger.ZLogError(
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


        /// <inheritdoc cref="IDisposable"/>
        public override void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            base.Dispose();

            _debug?.ReportDeviceLiveObjects();

            _graphicsQueue.Dispose();
        }
    }
}
