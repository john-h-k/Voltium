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
        /// The default <see cref="IndirectCommand"/> for performing an indirect draw.
        /// It changes no root signature bindings and has a command size of <see langword="sizeof"/>(<see cref="IndirectDrawArguments"/>)
        /// </summary>
        public IndirectCommand DrawIndirect { get; }

        /// <summary>
        /// The default <see cref="IndirectCommand"/> for performing an indirect indexed draw.
        /// It changes no root signature bindings and has a command size of <see langword="sizeof"/>(<see cref="IndirectDrawIndexedArguments"/>)
        /// </summary>
        public IndirectCommand DrawIndexedIndirect { get; }

        /// <summary>
        /// The default <see cref="IndirectCommand"/> for performing an indirect dispatch rays.
        /// It changes no root signature bindings and has a command size of <see langword="sizeof"/>(<see cref="IndirectDispatchRaysArguments"/>)
        /// </summary>
        public IndirectCommand DispatchRaysIndirect { get; }

        /// <summary>
        /// The default <see cref="IndirectCommand"/> for performing an indirect dispatch mesh.
        /// It changes no root signature bindings and has a command size of <see langword="sizeof"/>(<see cref="IndirectDispatchMeshArguments"/>)
        /// </summary>
        public IndirectCommand DispatchMeshIndirect { get; }

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

            DrawIndirect = CreateIndirectCommand(IndirectArgument.CreateDraw());
            DrawIndexedIndirect = CreateIndirectCommand(IndirectArgument.CreateDrawIndexed());
            DispatchMeshIndirect = CreateIndirectCommand(IndirectArgument.CreateDispatchMesh());
            DispatchRaysIndirect = CreateIndirectCommand(IndirectArgument.CreateDispatchRays());
        }


        public override sealed IndirectCommand CreateIndirectCommand(RootSignature? rootSignature, ReadOnlySpan<IndirectArgument> arguments, int commandStride = -1)
        {
            if (commandStride == -1)
            {
                commandStride = CalculateCommandStride(arguments);
            }

            if (arguments.Length == 1)
            {
                var cmd = arguments[0].Desc.Type switch
                {
                    D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DRAW when commandStride == sizeof(IndirectDrawArguments) => DrawIndirect,
                    D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DRAW_INDEXED when commandStride == sizeof(IndirectDrawIndexedArguments) => DrawIndexedIndirect,
                    D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DISPATCH when commandStride == sizeof(IndirectDispatchArguments) => DispatchIndirect,
                    D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DISPATCH_RAYS when commandStride == sizeof(IndirectDispatchRaysArguments) => DispatchRaysIndirect,
                    D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DISPATCH_MESH when commandStride == sizeof(IndirectDispatchMeshArguments) => DispatchMeshIndirect,
                    _ => null
                };

                if (cmd is not null)
                {
                    return cmd;
                }
            }

            return new IndirectCommand(CreateCommandSignature(rootSignature, arguments, (uint)commandStride).Move(), rootSignature, (uint)commandStride, arguments.ToArray());
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
            var heaps = stackalloc ID3D12DescriptorHeap*[numHeaps] { Resources.GetHeap(), _samplers.GetHeap() };

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
    }
}
