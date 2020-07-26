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
using Voltium.Core.Pool;

namespace Voltium.Core.Devices
{
    /// <summary>
    /// The top-level manager for application resources
    /// </summary>
    public unsafe partial class GraphicsDevice : ComputeDevice
    {
        private DeviceConfiguration _config = null!;

        internal ulong TotalFramesRendered = 0;

        private bool _disposed;

        /// <summary>
        /// 
        /// </summary>
        public void Idle()
        {
            GraphicsQueue.GetSynchronizerForIdle().Block();
        }

        /// <summary>
        /// Create a new <see cref="GraphicsDevice"/>
        /// </summary>
        /// <param name="adapter">The <see cref="Adapter"/> to create the device on, or <see langword="null"/> to use the default adapter</param>
        /// <param name="config">The <see cref="DeviceConfiguration"/> to create the device with</param>
        public GraphicsDevice(DeviceConfiguration config, in Adapter? adapter) : base(config, adapter)
        {
            Guard.NotNull(config);

            _config = config;


            Allocator = new GpuAllocator(this);
            PipelineManager = new PipelineManager(this);

            GraphicsQueue = new SynchronizedCommandQueue(this, ExecutionContext.Graphics);

            CreateSwapChain();
            CreateDescriptorHeaps();
        }

        // Output requires this for swapchain creation
        internal IUnknown* GetGraphicsQueue() => (IUnknown*)GraphicsQueue.GetQueue();

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
            highestQualityLevel = new(sampleCount, _sampleCounts.GetQualityLevelsForSampleCount(sampleCount) - 1);

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

        /// <summary>
        /// Returns a <see cref="GraphicsContext"/> used for recording graphical commands
        /// </summary>
        /// <returns>A new <see cref="GraphicsContext"/></returns>
        public GraphicsContext BeginGraphicsContext(PipelineStateObject? pso = null, bool executeOnClose = false)
        {
            var context = ContextPool.Rent(ExecutionContext.Graphics, pso, executeOnClose: executeOnClose);

            SetDefaultState(ref context, pso);

            return new GraphicsContext(context);
        }

        private void SetDefaultState(ref GpuContext context, PipelineStateObject? pso)
        {
            var rootSig = pso is null ? null : pso.GetRootSig();
            if (pso is ComputePipelineStateObject)
            {
                context.List->SetComputeRootSignature(rootSig);
            }
            else if (pso is GraphicsPipelineStateObject)
            {
                context.List->SetGraphicsRootSignature(rootSig);
            }

            const int numHeaps = 2;
            var heaps = stackalloc ID3D12DescriptorHeap*[numHeaps] { ResourceDescriptors.GetHeap(), _samplers.GetHeap() };

            context.List->SetDescriptorHeaps(numHeaps, heaps);
        }

        /// <summary>
        /// Resets the render target view heap
        /// </summary>
        public void ResetRenderTargetViewHeap() => _rtvs.ResetHeap();

        /// <summary>
        /// Resets the depth stencil view heap
        /// </summary>
        public void ResetDepthStencilViewHeap() => _dsvs.ResetHeap();

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
            if (Device.TryQueryInterface<ID3D12DeviceRemovedExtendedData>(out var dred))
            {
                using (dred)
                {
                    OnDeviceRemovedWithDred(dred.Get());
                }
            }

            LogHelper.LogError(
                "Device removed, no DRED present. Enable DEBUG or D3D12_DRED for enhanced device removed information");
        }

        private void OnDeviceRemovedWithDred(ID3D12DeviceRemovedExtendedData* dred)
        {
            System.Diagnostics.Debug.Assert(dred != null);

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

            Debug?.ReportDeviceLiveObjects();

            GraphicsQueue.Dispose();
        }
    }
}
