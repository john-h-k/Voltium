using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.GpuResources;
using Voltium.Core.Infrastructure;
using Voltium.Core.Managers;
using Voltium.Core.Memory.GpuResources;
using Voltium.Core.Pipeline;
using static TerraFX.Interop.Windows;
using Buffer = Voltium.Core.Memory.GpuResources.Buffer;

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
        // Prevent external types inheriting from this (we rely on expected internal behaviour in a few places)
        internal ComputeDevice() { }

        /// <summary>
        /// The default allocator for the device
        /// </summary>
        public GpuAllocator Allocator { get; private protected set; } = null!;
        internal ILogger Logger { get; } = NullLogger.Instance;

        private protected ComPtr<ID3D12Device> _device;
        private protected SupportedDevice _supportedDevice;
        private protected enum SupportedDevice { Device, Device1, Device2, Device3, Device4, Device5, Device6, Device7, Device8 }

        private protected static HashSet<ulong?> _preexistingDevices = new(1);

        /// <summary>
        /// Gets the <see cref="ID3D12Device"/> used by this application
        /// </summary>
        internal ID3D12Device* DevicePointer => _device.Get();

        /// <summary>
        /// The number of physical adapters, referred to as nodes, that the device uses
        /// </summary>
        public uint NodeCount => DevicePointer->GetNodeCount();

        private protected void CreateNewDevice(
            Adapter? adapter,
            FeatureLevel level
        )
        {
            // null adapter has 2 LUIDs in the hashset, one is 'null', the other is the real adapter LUID
            // this handles the case where the default adapter is used and *then* the default adapter is used explicitly
            var adapterLuid = adapter?.AdapterLuid;
            if (_preexistingDevices.Contains(adapterLuid))
            {
                ThrowHelper.ThrowArgumentException("Device already exists for adapter");
            }

            {
                using ComPtr<ID3D12Device> p = default;

                var underlying = adapter.GetValueOrDefault();

                bool success = SUCCEEDED(D3D12CreateDevice(
                    // null device triggers D3D12 to select a default device
                    adapter is Adapter ? underlying.UnderlyingAdapter : null,
                    (D3D_FEATURE_LEVEL)level,
                    p.Iid,
                    ComPtr.GetVoidAddressOf(&p)
                ));
                _device = p.Move();
            }

            DebugHelpers.SetName(_device.Get(), "Primary Device");

            _supportedDevice = _device switch
            {
                _ when _device.HasInterface<ID3D12Device8>() => SupportedDevice.Device8,
                _ when _device.HasInterface<ID3D12Device7>() => SupportedDevice.Device7,
                _ when _device.HasInterface<ID3D12Device6>() => SupportedDevice.Device6,
                _ when _device.HasInterface<ID3D12Device5>() => SupportedDevice.Device5,
                _ when _device.HasInterface<ID3D12Device4>() => SupportedDevice.Device4,
                _ when _device.HasInterface<ID3D12Device3>() => SupportedDevice.Device3,
                _ when _device.HasInterface<ID3D12Device2>() => SupportedDevice.Device2,
                _ when _device.HasInterface<ID3D12Device1>() => SupportedDevice.Device1,
                _ => SupportedDevice.Device
            };

            LUID luid = DevicePointer->GetAdapterLuid();
            _preexistingDevices.Add(Unsafe.As<LUID, ulong>(ref luid));
            if (adapter is null)
            {
                _preexistingDevices.Add(null);
            }
        }

        internal bool TryQueryInterface<T>(out ComPtr<T> result) where T : unmanaged
        {
            var success = ComPtr.TryQueryInterface(DevicePointer, out T* val);
            result = new ComPtr<T>(val);
            return success;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public CopyContext BeginCopyContext()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ComputeContext BeginComputeContext(ComputePso? pso = null)
        {
            throw new NotImplementedException();
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
            _device.Dispose();
            Allocator.Dispose();
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
