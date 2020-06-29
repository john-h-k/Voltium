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
    public unsafe partial class ComputeDevice
    {
        // Prevent external types inheriting from this (we rely on expected internal behaviour in a few places)
        internal ComputeDevice() { }

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
