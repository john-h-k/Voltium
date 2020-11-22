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
using static TerraFX.Interop.Windows;
using Voltium.Core.Pool;
using Voltium.Core.Contexts;

namespace Voltium.Core.Devices
{
    [Flags]
    enum DeviceFlags
    {
        None = 0,
        DisableGpuTimeout = 1 << 0,
    }

    /// <summary>
    /// The top-level manager for application resources
    /// </summary>
    public unsafe partial class GraphicsDevice : ComputeDevice
    {
        internal ulong TotalFramesRendered = 0;

        private bool _disposed;

        /// <summary>
        /// Block until this device has idled on all queues
        /// </summary>
        public void Idle()
        {
            var graphics = GraphicsQueue.GetSynchronizerForIdle();
            var compute = ComputeQueue.GetSynchronizerForIdle();
            var copy = CopyQueue.GetSynchronizerForIdle();

            if (DeviceLevel >= SupportedDevice.Device1)
            {
                var fences = stackalloc ID3D12Fence*[3];
                var fenceValues = stackalloc ulong[3];

                graphics.GetFenceAndMarker(out fences[0], out fenceValues[0]);
                compute.GetFenceAndMarker(out fences[1], out fenceValues[1]);
                copy.GetFenceAndMarker(out fences[2], out fenceValues[2]);

                ThrowIfFailed(As<ID3D12Device1>()->SetEventOnMultipleFenceCompletion(
                    fences,
                    fenceValues,
                    3,
                    D3D12_MULTIPLE_FENCE_WAIT_FLAGS.D3D12_MULTIPLE_FENCE_WAIT_FLAG_ALL,
                    default
                ));
            }
            else
            {
                BetterDeviceNeeded();
            }
        }

        //public BundleContext CreateBundle(PipelineStateObject pso)
        //{
        //    const ExecutionContext bundleContext = (ExecutionContext)D3D12_COMMAND_LIST_TYPE.D3D12_COMMAND_LIST_TYPE_BUNDLE;
        //    var allocator = CreateAllocator(bundleContext);

        //    _ = pso.Pointer.TryQueryInterface<ID3D12PipelineState>(out var pPipeline);
        //    var list = CreateList(bundleContext, allocator.Ptr, pPipeline.Ptr);

        //    return new BundleContext(new ContextParams(this, list, allocator, pso, bundleContext, ContextFlags.None));
        //}

        private void BetterDeviceNeeded()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the <see cref="GraphicsDevice"/> for a given <see cref="Adapter"/>
        /// </summary>
        /// <param name="requiredFeatureLevel">The required <see cref="FeatureLevel"/> for device creation</param>
        /// <param name="adapter">The <see cref="Adapter"/> to create the device from, or <see langword="null"/> to use the default adapter</param>
        /// <param name="config">The <see cref="DebugLayerConfiguration"/> for the device, or <see langword="null"/> to use the default</param>
        /// <returns>A <see cref="GraphicsDevice"/></returns>
        public static new GraphicsDevice Create(FeatureLevel requiredFeatureLevel, in Adapter? adapter, DebugLayerConfiguration? config = null)
        {
            if (TryGetDevice(requiredFeatureLevel, adapter ?? DefaultAdapter.Value, out var device))
            {
                if (device is GraphicsDevice graphics)
                {
                    return graphics;
                }

                ThrowHelper.ThrowInvalidOperationException("Cannot create a GraphicsDevice for this adapter as a ComputeDevice was created for this adapter");
            }
            return new GraphicsDevice(requiredFeatureLevel, adapter, config);
        }

        private GraphicsDevice(FeatureLevel level, in Adapter? adapter, DebugLayerConfiguration? config = null) : base(level, adapter, config)
        {
            GraphicsQueue = new CommandQueue(this, ExecutionContext.Graphics, true);
        }


        // Output requires this for swapchain creation
        internal IUnknown* GetGraphicsQueue() => (IUnknown*)GraphicsQueue.GetQueue();

        /// <summary>
        /// Returns a <see cref="GraphicsContext"/> used for recording graphical commands
        /// </summary>
        /// <returns>A new <see cref="GraphicsContext"/></returns>
        public GraphicsContext BeginGraphicsContext(PipelineStateObject? pso = null, ContextFlags flags = ContextFlags.None)
        {
            var @params = ContextPool.Rent(ExecutionContext.Graphics, pso, flags);

            var context = new GraphicsContext(@params);
            SetDefaultState(context, pso);
            return context;
        }

        private void SetDefaultState(GpuContext context, PipelineStateObject? pso)
        {
            var rootSig = pso is null ? null : pso.GetRootSig();
            if (pso is ComputePipelineStateObject or RaytracingPipelineStateObject)
            {
                context.List->SetComputeRootSignature(rootSig);
            }
            if (pso is GraphicsPipelineStateObject or MeshPipelineStateObject or RaytracingPipelineStateObject)
            {
                context.List->SetGraphicsRootSignature(rootSig);
            }

            const int numHeaps = 2;
            var heaps = stackalloc ID3D12DescriptorHeap*[numHeaps] { UavCbvSrvs.GetHeap(), _samplers.GetHeap() };

            context.List->SetDescriptorHeaps(numHeaps, heaps);
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

            GraphicsQueue.Dispose();
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public void ResetRenderTargetViewHeap() => _rtvs.ResetHeap();

        public void ResetDepthStencilViewHeap() => _dsvs.ResetHeap();
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
