using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;
using Voltium.Common;
using TerraFX.Interop;
using Voltium.Core.Memory;

namespace Voltium.Core.Devices
{
    internal struct TextureSubresourceLayout
    {
        public D3D12_PLACED_SUBRESOURCE_FOOTPRINT[] Layouts;
        public uint[] NumRows;
        public ulong[] RowSizes;
        public ulong TotalSize;

    }

    public unsafe partial class ComputeDevice
    {
        // Convienience wrapper methods over ID3D12Device* 

        internal D3D12_RESOURCE_ALLOCATION_INFO GetAllocationInfo(InternalAllocDesc* desc)
            => DevicePointer->GetResourceAllocationInfo(0, 1, &desc->Desc);

        private uint RtvDescriptorSize, DsvDescriptorSize, SamplerDescriptorSize, CbvSrvUavDescriptorSize;

        internal uint GetIncrementSizeForDescriptorType(D3D12_DESCRIPTOR_HEAP_TYPE type)
        {
            if (RtvDescriptorSize == 0)
            {
                RtvDescriptorSize = DevicePointer->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_RTV);
                DsvDescriptorSize = DevicePointer->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_DSV);
                SamplerDescriptorSize = DevicePointer->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER);
                CbvSrvUavDescriptorSize = DevicePointer->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
            }

            return type switch
            {
                D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV => CbvSrvUavDescriptorSize,
                D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER => SamplerDescriptorSize,
                D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_RTV => RtvDescriptorSize,
                D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_DSV => DsvDescriptorSize,
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<uint>(nameof(type))
            };
        }

        internal void CopyDescriptors(uint numDescriptors, D3D12_CPU_DESCRIPTOR_HANDLE dest, D3D12_CPU_DESCRIPTOR_HANDLE src, D3D12_DESCRIPTOR_HEAP_TYPE type)
            => DevicePointer->CopyDescriptorsSimple(numDescriptors, dest, src, type);

        internal UniqueComPtr<ID3D12DescriptorHeap> CreateDescriptorHeap(D3D12_DESCRIPTOR_HEAP_DESC* desc)
        {
            using UniqueComPtr<ID3D12DescriptorHeap> descriptorHeap = default;
            ThrowIfFailed(DevicePointer->CreateDescriptorHeap(
                desc,
                descriptorHeap.Iid,
                (void**)&descriptorHeap
            ));

            return descriptorHeap.Move();
        }

        internal UniqueComPtr<ID3D12CommandAllocator> CreateAllocator(ExecutionContext context)
        {
            using UniqueComPtr<ID3D12CommandAllocator> allocator = default;
            ThrowIfFailed(DevicePointer->CreateCommandAllocator(
                (D3D12_COMMAND_LIST_TYPE)context,
                allocator.Iid,
                (void**)&allocator
            ));

            return allocator.Move();
        }

        internal UniqueComPtr<ID3D12GraphicsCommandList6> CreateList(ExecutionContext context, ID3D12CommandAllocator* allocator, ID3D12PipelineState* pso)
        {
            using UniqueComPtr<ID3D12GraphicsCommandList6> list = default;
            ThrowIfFailed(DevicePointer->CreateCommandList(
                0, // TODO: MULTI-GPU
                (D3D12_COMMAND_LIST_TYPE)context,
                allocator,
                pso,
                list.Iid,
                (void**)&list
            ));

            return list.Move();
        }

        internal UniqueComPtr<ID3D12QueryHeap> CreateQueryHeap(D3D12_QUERY_HEAP_DESC desc)
        {
            using UniqueComPtr<ID3D12QueryHeap> queryHeap = default;
            DevicePointer->CreateQueryHeap(&desc, queryHeap.Iid, (void**)&queryHeap);
            return queryHeap.Move();
        }

        internal UniqueComPtr<ID3D12Resource> CreatePlacedResource(ID3D12Heap* heap, ulong offset, InternalAllocDesc* desc)
        {
            using UniqueComPtr<ID3D12Resource> resource = default;

            bool hasClearVal = desc->Desc.Dimension != D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER && GpuAllocator.IsRenderTargetOrDepthStencil(desc->Desc.Flags);

            ThrowIfFailed(DevicePointer->CreatePlacedResource(
                 heap,
                 offset,
                 &desc->Desc,
                 desc->InitialState,
                 hasClearVal ? &desc->ClearValue : null,
                 resource.Iid,
                 (void**)&resource
             ));

            return resource.Move();
        }

        internal UniqueComPtr<ID3D12Resource> CreateCommittedResource(InternalAllocDesc* desc)
        {
            using UniqueComPtr<ID3D12Resource> resource = default;

            bool hasClearVal = desc->Desc.Dimension != D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER && GpuAllocator.IsRenderTargetOrDepthStencil(desc->Desc.Flags);

            ThrowIfFailed(DevicePointer->CreateCommittedResource(
                    &desc->HeapProperties,
                    D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
                    &desc->Desc,
                    desc->InitialState,
                    hasClearVal ? &desc->ClearValue : null,
                    resource.Iid,
                    (void**)&resource
            ));

            return resource.Move();
        }

        internal void GetAccelerationStructuredPrebuildInfo(D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_INPUTS* pInputs, D3D12_RAYTRACING_ACCELERATION_STRUCTURE_PREBUILD_INFO* pInfo)
        {
            DevicePointer->GetRaytracingAccelerationStructurePrebuildInfo(pInputs, pInfo);
        }

        internal UniqueComPtr<ID3D12Heap> CreateHeap(D3D12_HEAP_DESC* desc)
        {
            UniqueComPtr<ID3D12Heap> heap = default;
            ThrowIfFailed(DevicePointer->CreateHeap(
                desc,
                heap.Iid,
                (void**)&heap
            ));

            return heap.Move();
        }

        internal unsafe UniqueComPtr<ID3D12CommandQueue> CreateQueue(ExecutionContext type, D3D12_COMMAND_QUEUE_FLAGS flags = D3D12_COMMAND_QUEUE_FLAGS.D3D12_COMMAND_QUEUE_FLAG_NONE)
        {
            var desc = new D3D12_COMMAND_QUEUE_DESC
            {
                Type = (D3D12_COMMAND_LIST_TYPE)type,
                Flags = flags,
                NodeMask = 0, // TODO: MULTI-GPU
                Priority = (int)D3D12_COMMAND_QUEUE_PRIORITY.D3D12_COMMAND_QUEUE_PRIORITY_NORMAL // why are you like this D3D12
            };

            UniqueComPtr<ID3D12CommandQueue> p = default;

            ThrowIfFailed(DevicePointer->CreateCommandQueue(
                &desc,
                p.Iid,
                (void**)&p
            ));

            return p.Move();
        }

        internal UniqueComPtr<ID3D12RootSignature> CreateRootSignature(uint nodeMask, void* pSignature, uint signatureLength)
        {
            using UniqueComPtr<ID3D12RootSignature> rootSig = default;
            ThrowIfFailed(DevicePointer->CreateRootSignature(
                nodeMask,
                pSignature,
                signatureLength,
                rootSig.Iid,
                (void**)&rootSig
            ));

            return rootSig.Move();
        }

        internal void GetCopyableFootprint(
            in Texture tex,
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

        internal TextureSubresourceLayout GetCopyableFootprints(
            in Texture tex,
            uint firstSubresource,
            uint numSubresources
        )
        {
            TextureSubresourceLayout layout;
            GetCopyableFootprints(tex, firstSubresource, numSubresources, out layout.Layouts, out layout.NumRows, out layout.RowSizes, out layout.TotalSize);
            return layout;
        }

        internal void GetCopyableFootprints(
            in Texture tex,
            uint firstSubresource,
            uint numSubresources,
            out D3D12_PLACED_SUBRESOURCE_FOOTPRINT[] layouts,
            out uint[] numRows,
            out ulong[] rowSizesInBytes,
            out ulong requiredSize
        )
        {
            var desc = tex.GetResourcePointer()->GetDesc();

            layouts = new D3D12_PLACED_SUBRESOURCE_FOOTPRINT[numSubresources];
            numRows = new uint[numSubresources];
            rowSizesInBytes = new ulong[numSubresources];

            fixed (D3D12_PLACED_SUBRESOURCE_FOOTPRINT* pLayouts = layouts)
            fixed (uint* pNumRows = numRows)
            fixed (ulong* pRowSizesInBytes = rowSizesInBytes)
            {
                ulong size;
                DevicePointer->GetCopyableFootprints(&desc, 0, numSubresources, 0, pLayouts, pNumRows, pRowSizesInBytes, &size);
                requiredSize = size;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="numSubresources"></param>
        /// <returns></returns>
        public ulong GetRequiredSize(
            in Texture tex,
            uint numSubresources
        )
        {
            var desc = tex.GetResourcePointer()->GetDesc();
            return GetRequiredSize(&desc, numSubresources);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="numSubresources"></param>
        /// <returns></returns>
        public ulong GetRequiredSize(
            in TextureDesc tex,
            uint numSubresources
        )
        {
            GpuAllocator.CreateDesc(tex, out var desc);
            return GetRequiredSize(&desc, numSubresources);
        }

        internal ulong GetRequiredSize(
            D3D12_RESOURCE_DESC* desc,
            uint numSubresources
        )
        {
            ulong requiredSize;
            DevicePointer->GetCopyableFootprints(desc, 0, numSubresources, 0, null, null, null, &requiredSize);
            return requiredSize;
        }
    }
}
