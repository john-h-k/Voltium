using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Memory;
using Voltium.Core.Infrastructure;
using Voltium.Core.Devices;
using Voltium.Core.Pipeline;
using ZLogger;
using Voltium.Core.Devices.Shaders;
using Voltium.Core.Pool;

using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12_FEATURE;

using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core.Devices
{
    /// <summary>
    /// 
    /// </summary>
    public struct TextureFootprint
    {
        /// <summary>
        /// 
        /// </summary>
        public ulong RowSize;
        /// <summary>
        /// 
        /// </summary>
        public uint NumRows;
    }

    /// <summary>
    /// 
    /// </summary>
    public unsafe partial class ComputeDevice : IDisposable
    {

        /// <summary>
        /// The default allocator for the device
        /// </summary>
        public GpuAllocator Allocator { get; private protected set; } = null!;

        /// <summary>
        /// The default pipeline manager for the device
        /// </summary>
        public PipelineManager PipelineManager { get; private protected set; } = null!;

        private protected ComPtr<ID3D12Device> Device;
        internal SupportedDevice DeviceLevel;
        internal enum SupportedDevice { Unknown, Device, Device1, Device2, Device3, Device4, Device5, Device6, Device7, Device8 }
        private protected static HashSet<ulong> PreexistingDevices = new(1);
        private protected DebugLayer? Debug;
        private protected ContextPool ContextPool;

        private protected SynchronizedCommandQueue CopyQueue;
        private protected SynchronizedCommandQueue ComputeQueue;

        private protected SynchronizedCommandQueue GraphicsQueue;
        private protected ulong CpuFrequency;

        private ref SynchronizedCommandQueue GetQueueForContext(ExecutionContext context)
        {
            switch (context)
            {
                case ExecutionContext.Copy:
                    return ref CopyQueue;
                case ExecutionContext.Compute:
                    return ref ComputeQueue;
                case ExecutionContext.Graphics:
                    return ref GraphicsQueue;
            }

            return ref Helpers.NullRef<SynchronizedCommandQueue>();
        }

        /// <summary>
        /// The <see cref="Adapter"/> this device uses
        /// </summary>
        public Adapter Adapter { get; private set; }

        internal enum SupportedGraphicsCommandList { Unknown, GraphicsCommandList, GraphicsCommandList1, GraphicsCommandList2, GraphicsCommandList3, GraphicsCommandList4, GraphicsCommandList5 }
        private SupportedGraphicsCommandList SupportedList;

        internal ID3D12Device* DevicePointer => Device.Get();
        internal TDevice* DevicePointerAs<TDevice>() where TDevice : unmanaged => Device.AsBase<TDevice>().Get();

        /// <summary>
        /// The number of physical adapters, referred to as nodes, that the device uses
        /// </summary>
        public uint NodeCount => DevicePointer->GetNodeCount();


        private static readonly Adapter DefaultAdapter = GetDefaultAdapter();

        /// <summary>
        /// The highest <see cref="ShaderModel"/> supported by this device
        /// </summary>
        public ShaderModel HighestSupportedShaderModel { get; private set; }

        /// <summary>
        /// Whether DXIL is supported, rather than the old DXBC bytecode form.
        /// This is equivalent to cheking if <see cref="HighestSupportedShaderModel"/> supports shader model 6
        /// </summary>
        public bool IsDxilSupported => HighestSupportedShaderModel.IsDxil;

        private static Adapter GetDefaultAdapter()
        {
            using var factory = new DxgiDeviceFactory().GetEnumerator();
            _ = factory.MoveNext();
            return factory.Current;
        }

        private static readonly object StateLock = new object();

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
        /// Create a new <see cref="ComputeDevice"/>
        /// </summary>
        /// <param name="adapter">The <see cref="Adapter"/> to create the device on, or <see langword="null"/> to use the default adapter</param>
        /// <param name="config">The <see cref="DeviceConfiguration"/> to create the device with</param>
        public ComputeDevice(DeviceConfiguration config, Adapter? adapter)
        {
            Debug = new DebugLayer(config.DebugLayerConfiguration);

            {
                // Prevent another device creation messing with our settings
                lock (StateLock)
                {
                    Debug.SetGlobalStateForConfig();

                    CreateNewDevice(adapter, config.RequiredFeatureLevel);

                    Debug.ResetGlobalState();
                }

                if (!Device.Exists)
                {
                    ThrowHelper.ThrowPlatformNotSupportedException($"FATAL: Creation of ID3D12Device with feature level '{config.RequiredFeatureLevel}' failed");
                }

                Debug.SetDeviceStateForConfig(this);
            }

            ulong frequency;
            var res = QueryPerformanceFrequency((LARGE_INTEGER*) /* <- can we do that? */ &frequency);

            if (res == 0)
            {
                ThrowHelper.ThrowPlatformNotSupportedException("No high resolution timer found");
            }

            CpuFrequency = frequency;

            QueryFeaturesOnCreation();
            ContextPool = new ContextPool(this);
            CopyQueue = new SynchronizedCommandQueue(this, ExecutionContext.Copy);
            ComputeQueue = new SynchronizedCommandQueue(this, ExecutionContext.Compute);
        }

        private protected void CreateNewDevice(
            Adapter? adapter,
            FeatureLevel level
        )
        {
            adapter ??= GetDefaultAdapter();

            if (PreexistingDevices.Contains(adapter.GetValueOrDefault().AdapterLuid))
            {
                ThrowHelper.ThrowArgumentException("Device already exists for adapter");
            }

            {
                using ComPtr<ID3D12Device> p = default;

                var underlying = adapter.GetValueOrDefault();

                bool success = SUCCEEDED(D3D12CreateDevice(
                    // null device triggers D3D12 to select a default device
                    adapter.GetValueOrDefault().UnderlyingAdapter,
                    (D3D_FEATURE_LEVEL)level,
                    p.Iid,
                    ComPtr.GetVoidAddressOf(&p)
                ));
                Device = p.Move();

                LogHelper.LogInformation($"New D3D12 device created from adapter: \n{adapter}");
            }

            DebugHelpers.SetName(Device.Get(), "Primary Device");

            DeviceLevel = Device switch
            {
                _ when Device.HasInterface<ID3D12Device8>() => SupportedDevice.Device8,
                _ when Device.HasInterface<ID3D12Device7>() => SupportedDevice.Device7,
                _ when Device.HasInterface<ID3D12Device6>() => SupportedDevice.Device6,
                _ when Device.HasInterface<ID3D12Device5>() => SupportedDevice.Device5,
                _ when Device.HasInterface<ID3D12Device4>() => SupportedDevice.Device4,
                _ when Device.HasInterface<ID3D12Device3>() => SupportedDevice.Device3,
                _ when Device.HasInterface<ID3D12Device2>() => SupportedDevice.Device2,
                _ when Device.HasInterface<ID3D12Device1>() => SupportedDevice.Device1,
                _ => SupportedDevice.Device
            };

            LUID luid = DevicePointer->GetAdapterLuid();
            PreexistingDevices.Add(Unsafe.As<LUID, ulong>(ref luid));
        }

        internal void QueryFeatureSupport<T>(D3D12_FEATURE feature, ref T val) where T : unmanaged
        {
            fixed (T* pVal = &val)
            {
                Guard.ThrowIfFailed(DevicePointer->CheckFeatureSupport(feature, pVal, (uint)sizeof(T)));
            }
        }

        private protected virtual void QueryFeaturesOnCreation()
        {
            D3D12_FEATURE_DATA_SHADER_MODEL highest;

            // the highest shader model that the app understands
            highest.HighestShaderModel = D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_6;
            QueryFeatureSupport(D3D12_FEATURE_SHADER_MODEL, ref highest);

            GetMajorMinorForShaderModel(highest.HighestShaderModel, out var major, out var minor);
            HighestSupportedShaderModel = new ShaderModel(ShaderType.Unspecified, (byte)major, (byte)minor);
        }

        private static void GetMajorMinorForShaderModel(D3D_SHADER_MODEL model, out int major, out int minor)
            => (major, minor) = model switch
            {
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_5_1 => (5, 1),
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_0 => (6, 0),
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_1 => (6, 1),
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_2 => (6, 2),
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_3 => (6, 3),
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_4 => (6, 4),
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_5 => (6, 5),
                D3D_SHADER_MODEL.D3D_SHADER_MODEL_6_6 => (6, 6),
                _ => throw new ArgumentException()
            };

        internal bool TryQueryInterface<T>(out ComPtr<T> result) where T : unmanaged
        {
            var success = ComPtr.TryQueryInterface(DevicePointer, out T* val);
            result = new ComPtr<T>(val);
            return success;
        }

        /// <summary>
        /// Represents a scoped PIX capture that ends when the type is disposed. Use <see cref="BeginScopedCapture"/> to create one.
        /// If PIX is not attached and the debug layer is attached, a message will be emitted explaining that the capture was dropped because PIX was not attached.
        /// If PIX is not attached and the debug layer is disabled, the capture will be silently dropped
        /// </summary>
        public struct ScopedCapture : IDisposable
        {
            private DebugLayer? _layer;

            internal ScopedCapture(DebugLayer? layer)
            {
                _layer = layer;
                layer?.BeginCapture();
            }

            /// <summary>
            /// Ends the capture
            /// </summary>
            public void Dispose() => _layer?.EndCapture();
        }

        /// <summary>
        /// Begins a scoped PIX capture that ends when the <see cref="ScopedCapture"/> is disposed
        /// </summary>
        /// <returns>A new <see cref="ScopedCapture"/></returns>
        public ScopedCapture BeginScopedCapture() => new ScopedCapture(Debug);

        /// <summary>
        /// Begins a PIX capture
        /// </summary>
        public void BeginCapture() => Debug?.BeginCapture();

        /// <summary>
        /// Ends a PIX capture
        /// </summary>
        public void EndCapture() => Debug?.EndCapture();

        /// <summary>
        /// Returns a <see cref="CopyContext"/> used for recording copy commands
        /// </summary>
        public CopyContext BeginCopyContext(bool executeOnClose = false)
        {
            var context = ContextPool.Rent(ExecutionContext.Copy, null, executeOnClose: executeOnClose);
            return new CopyContext(context);
        }

        /// <summary>
        /// Returns a <see cref="ComputeContext"/> used for recording compute commands
        /// </summary>
        public ComputeContext BeginComputeContext(ComputePipelineStateObject? pso = null, bool executeOnClose = false)
        {
            var context = ContextPool.Rent(ExecutionContext.Compute, pso, executeOnClose: executeOnClose);

            SetDefaultState(ref context, pso);

            return new ComputeContext(context);
        }

        private unsafe void SetDefaultState(ref GpuContext context, ComputePipelineStateObject? pso)
        {
            if (pso is not null)
            {
                context.List->SetComputeRootSignature(pso.GetRootSig());
            }

            const int numHeaps = 1;
            var heaps = stackalloc ID3D12DescriptorHeap*[1] { ResourceDescriptors.GetHeap() };

            context.List->SetDescriptorHeaps(numHeaps, heaps);
        }

        internal ComPtr<ID3D12Heap> CreateHeap(D3D12_HEAP_DESC desc)
        {
            ComPtr<ID3D12Heap> heap = default;
            Guard.ThrowIfFailed(DevicePointer->CreateHeap(
                &desc,
                heap.Iid,
                ComPtr.GetVoidAddressOf(&heap)
            ));

            return heap.Move();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="intermediate"></param>
        /// <param name="data"></param>
        public void ReadbackIntermediateBuffer(TextureFootprint tex, Buffer intermediate, Span<byte> data)
        {
            var offset = data;
            var mapped = intermediate.Data;

            var alignedRowSize = MathHelpers.AlignUp(tex.RowSize, 256);
            for (var i = 0; i < tex.NumRows - 1; i++)
            {
                mapped.Slice(0, (int)tex.RowSize).CopyTo(offset);

                mapped = mapped.Slice((int)alignedRowSize);
                offset = offset.Slice((int)tex.RowSize);
            }
            mapped.Slice(0, (int)tex.RowSize).CopyTo(offset);
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
            ref var queue = ref GetQueueForContext(context);

            ulong gpuTick, cpuTick;

            if (!queue.TryQueryTimestamps(&gpuTick, &cpuTick))
            {
                ThrowHelper.ThrowExternalException("GPU timestamp query failed");
            }

            gpu = TimeSpan.FromSeconds(gpuTick / (double)queue.Frequency);
            cpu = TimeSpan.FromSeconds(cpuTick / (double)CpuFrequency);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="subresourceIndex"></param>
        /// <returns></returns>
        public TextureFootprint GetSubresourceFootprint(Texture tex, uint subresourceIndex)
        {
            TextureFootprint result;
            GetCopyableFootprint(tex, subresourceIndex, 1, out _, out result.NumRows, out result.RowSize, out _);
            return result;
        }

        internal void GetCopyableFootprint(
            Texture tex,
            uint firstSubresource,
            uint numSubresources,
            out D3D12_PLACED_SUBRESOURCE_FOOTPRINT layouts,
            out uint numRows,
            out ulong rowSizesInBytes,
            out ulong requiredSize
        )
        {
            var desc = tex.GetResourcePointer()->GetDesc();

            fixed (D3D12_PLACED_SUBRESOURCE_FOOTPRINT* pLayout = &layouts)
            {
                ulong rowSizes;
                uint rowCount;
                ulong size;
                DevicePointer->GetCopyableFootprints(&desc, 0, numSubresources, 0, pLayout, &rowCount, &rowSizes, &size);

                rowSizesInBytes = rowSizes;
                numRows = rowCount;
                requiredSize = size;
            }
        }

        internal void GetCopyableFootprints(
            Texture tex,
            uint firstSubresource,
            uint numSubresources,
            out Span<D3D12_PLACED_SUBRESOURCE_FOOTPRINT> layouts,
            out Span<uint> numRows,
            out Span<ulong> rowSizesInBytes,
            out ulong requiredSize
        )
        {
            var desc = tex.GetResourcePointer()->GetDesc();

            var subresources = GC.AllocateUninitializedArray<byte>((sizeof(D3D12_PLACED_SUBRESOURCE_FOOTPRINT) + sizeof(uint) + sizeof(ulong)) * (int)numSubresources, pinned: true);

            var pLayouts = (D3D12_PLACED_SUBRESOURCE_FOOTPRINT*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(subresources));
            ulong* pRowSizesInBytes = (ulong*)(pLayouts + numSubresources);
            uint* pNumRows = (uint*)(pRowSizesInBytes + numSubresources);

            ulong size;
            DevicePointer->GetCopyableFootprints(&desc, 0, numSubresources, 0, pLayouts, pNumRows, pRowSizesInBytes, &size);
            requiredSize = size;

            layouts = new Span<D3D12_PLACED_SUBRESOURCE_FOOTPRINT>(pLayouts, (int)numSubresources);
            rowSizesInBytes = new Span<ulong>(pRowSizesInBytes, (int)numSubresources);
            numRows = new Span<uint>(pNumRows, (int)numSubresources);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="numSubresources"></param>
        /// <returns></returns>
        public ulong GetRequiredSize(
            Texture tex,
            uint numSubresources
        )
        {
            return GetRequiredSize(tex.GetResourcePointer()->GetDesc(), numSubresources);
        }

        internal ulong GetRequiredSize(
            D3D12_RESOURCE_DESC desc,
            uint numSubresources
        )
        {
            ulong requiredSize;
            DevicePointer->GetCopyableFootprints(&desc, 0, numSubresources, 0, null, null, null, &requiredSize);
            return requiredSize;
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
                 resource.Iid,
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
                 resource.Iid,
                 ComPtr.GetVoidAddressOf(&resource)
            ));

            return resource.Move();

            static D3D12_HEAP_PROPERTIES GetHeapProperties(InternalAllocDesc desc)
            {
                return new D3D12_HEAP_PROPERTIES(desc.HeapType);
            }
        }

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            Device.Dispose();
            Allocator.Dispose();
            PipelineManager.Dispose();
        }

        //CheckFeatureSupport
        //CopyDescriptors
        //CopyDescriptorsSimple
        //CreateCommandAllocator
        //CreateCommandList
        //CreateCommandQueue
        //CreateCommandSignature
        //CreateCommittedResource
        //CreateComputePipelineState
        //CreateConstantBufferView
        //CreateDescriptorHeap
        //CreateFence
        //CreateHeap
        //CreatePlacedResource
        //CreateQueryHeap
        //CreateRootSignature
        //CreateShaderResourceView
        //CreateSharedHandle
        //CreateUnorderedAccessView
        //Evict
        //GetAdapterLuid
        //GetCopyableFootprints
        //GetCustomHeapProperties
        //GetDescriptorHandleIncrementSize
        //GetDeviceRemovedReason
        //GetNodeCount
        //GetResourceAllocationInfo
        //MakeResident
        //OpenSharedHandle
        //OpenSharedHandleByName
        //SetStablePowerState

        //CreatePipelineLibrary
        //SetEventOnMultipleFenceCompletion
        //SetResidencySetEventOnMultipleFenceCompletionPriority

        //CreatePipelineState

        //OpenExistingHeapFromAddress
        //OpenExistingHeapFromFileMapping
        //EnqueueMakeResident

        //GetResourceAllocationInfo1

        //CreateMetaCommand
        //CreateStateObject
        //EnumerateMetaCommandParameters
        //EnumerateMetaCommands
        //RemoveDevice
    }
}
