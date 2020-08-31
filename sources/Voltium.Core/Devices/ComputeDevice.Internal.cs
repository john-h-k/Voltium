using System;
using System.Collections.Generic;

using System.Text;
using System.Threading.Tasks;
using Voltium.Common;
using TerraFX.Interop;
using Voltium.Core.Memory;

namespace Voltium.Core.Devices
{
    public unsafe partial class ComputeDevice
    {
        // Convienience wrapper methods over ID3D12Device* 

        internal D3D12_RESOURCE_ALLOCATION_INFO GetAllocationInfo(InternalAllocDesc* desc)
            => DevicePointer->GetResourceAllocationInfo(0, 1, &desc->Desc);

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

        internal UniqueComPtr<ID3D12GraphicsCommandList> CreateList(ExecutionContext context, ID3D12CommandAllocator* allocator, ID3D12PipelineState* pso)
        {
            using UniqueComPtr<ID3D12GraphicsCommandList> list = default;
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
            var clearVal = desc->ClearValue.GetValueOrDefault();

            using UniqueComPtr<ID3D12Resource> resource = default;

            ThrowIfFailed(DevicePointer->CreatePlacedResource(
                 heap,
                 offset,
                 &desc->Desc,
                 desc->InitialState,
                 desc->ClearValue is null ? null : &clearVal,
                 resource.Iid,
                 (void**)&resource
             ));

            return resource.Move();
        }

        internal UniqueComPtr<ID3D12Resource> CreateCommittedResource(InternalAllocDesc* desc)
        {
            var heapProperties = GetHeapProperties(desc);
            var clearVal = desc->ClearValue.GetValueOrDefault();

            using UniqueComPtr<ID3D12Resource> resource = default;

            ThrowIfFailed(DevicePointer->CreateCommittedResource(
                    &heapProperties,
                    D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
                    &desc->Desc,
                    desc->InitialState,
                    desc->ClearValue is null ? null : &clearVal,
                    resource.Iid,
                    (void**)&resource
            ));

            return resource.Move();

            static D3D12_HEAP_PROPERTIES GetHeapProperties(InternalAllocDesc* desc)
            {
                return new D3D12_HEAP_PROPERTIES(desc->HeapType);
            }
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

        internal TextureLayout GetCopyableFootprints(
            in Texture tex,
            uint firstSubresource,
            uint numSubresources
        )
        {
            TextureLayout layout;
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
