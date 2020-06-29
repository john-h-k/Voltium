using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.GpuResources;
using Voltium.Core.Memory.GpuResources;
using Voltium.Core.Pipeline;
using static TerraFX.Interop.Windows;

namespace Voltium.Core.Devices
{
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

        private protected ComPtr<ID3D12Device> _device;

        /// <summary>
        /// Gets the <see cref="ID3D12Device"/> used by this application
        /// </summary>
        internal ID3D12Device* DevicePointer => _device.Get();

        /// <summary>
        /// The number of physical adapters, referred to as nodes, that the device uses
        /// </summary>
        public uint NodeCount => DevicePointer->GetNodeCount();

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
                heap.Guid,
                ComPtr.GetVoidAddressOf(&heap)
            ));

            return heap.Move();
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
                 resource.Guid,
                 ComPtr.GetVoidAddressOf(&resource)
            ));

            return resource;

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
