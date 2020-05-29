using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Loader;
using TerraFX.Interop;
using static TerraFX.Interop.D3D12_RESOURCE_STATES;
using Voltium.Common;

namespace Voltium.Core.GpuResources
{
    /// <summary>
    /// An allocator used for allocating temporary and long
    /// </summary>
    public unsafe sealed class GpuAllocator : IDisposable
    {
        private static readonly ulong InitialHeapSize = 1024 * 1024 * 256; // 256mb
        private HeapTypeSet<List<AllocatorHeap>> _heaps;
        private ComPtr<ID3D12Device> _device;
        private const ulong HighestRequiredAlign = 1024 * 1024 * 4; // 4mb

        /// <summary>
        /// Creates a new allocator
        /// </summary>
        /// <param name="device">The <see cref="ID3D12Device"/> to allocate on</param>
        public GpuAllocator(ComPtr<ID3D12Device> device)
        {
            Debug.Assert(device.Exists);
            _device = device.Move();

            _heaps[GpuMemoryType.CpuReadOptimized] = new();
            _heaps[GpuMemoryType.CpuWriteOptimized] = new();
            _heaps[GpuMemoryType.GpuOnly] = new();

            CreateNewHeap(GpuMemoryType.CpuReadOptimized);
            CreateNewHeap(GpuMemoryType.CpuWriteOptimized);
            CreateNewHeap(GpuMemoryType.GpuOnly);
        }

        private AllocatorHeap CreateNewHeap(GpuMemoryType type)
        {
            D3D12_HEAP_PROPERTIES props = new((D3D12_HEAP_TYPE)type);
            D3D12_HEAP_DESC desc = new(InitialHeapSize, props);

            ComPtr<ID3D12Heap> heap = default;
            _device.Get()->CreateHeap(&desc, heap.Guid, ComPtr.GetVoidAddressOf(&heap));

            var allocatorHeap = new AllocatorHeap { Heap = heap.Move(), FreeBlocks = new() };
            allocatorHeap.FreeBlocks.Add(new HeapBlock { Offset = 0, Size = desc.SizeInBytes });
            _heaps[type].Add(allocatorHeap);

            return allocatorHeap;
        }

        internal void Return(GpuResource gpuAllocation)
        {
            var heap = gpuAllocation.GetAllocatorHeap();
            if (!heap.Heap.Exists)
            {
                // resource is comitted (implicit heap)
                gpuAllocation.UnderlyingResource->Release();
            }
            else
            {
                ReturnPlacedAllocation(gpuAllocation);
            }    
        }

        private void ReturnPlacedAllocation(GpuResource gpuAllocation)
        {
            var block = new HeapBlock { Offset = gpuAllocation.GetOffsetFromUnderlyingResource(), Size = gpuAllocation.Size };
            var heap = gpuAllocation.GetAllocatorHeap();

            heap.FreeBlocks.Add(block);

            // TODO defrag
        }

        private D3D12_RESOURCE_ALLOCATION_INFO GetAllocationInfo(GpuResourceDesc desc)
        {
            // TODO use ID3D12Device4::GetResourceAllocationInfo1 for doing all our computations in one go
            return _device.Get()->GetResourceAllocationInfo(0 /* TODO: MULTI-GPU */, 1, &desc.ResourceFormat.D3D12ResourceDesc);
        }

        /// <summary>
        /// Allocates a new <see cref="VertexBuffer{TVertex}"/>
        /// </summary>
        /// <typeparam name="TVertex">The type of each vertex</typeparam>
        /// <param name="vertexCount">The number of vertices</param>
        /// <param name="type">The type of GPU memory to allocate in</param>
        /// <param name="flags">Any additional allocation flags passed to the allocator</param>
        /// <returns>A new <see cref="VertexBuffer{TVertex}"/></returns>
        public VertexBuffer<TVertex> AllocateVertexBuffer<TVertex>(
            uint vertexCount,
            GpuMemoryType type,
            GpuAllocFlags flags = GpuAllocFlags.None
        ) where TVertex : unmanaged
        {
            Debug.Assert(type != GpuMemoryType.CpuReadOptimized);

            var desc = new GpuResourceDesc(
                GpuResourceFormat.Buffer((uint)sizeof(TVertex) * vertexCount),
                type,
                type == GpuMemoryType.CpuWriteOptimized ? D3D12_RESOURCE_STATE_GENERIC_READ : D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER,
                flags
            );

            return new VertexBuffer<TVertex>(Allocate(desc));
        }

        /// <summary>
        /// Allocates a new <see cref="IndexBuffer{TIndex}"/>
        /// </summary>
        /// <typeparam name="TIndex">The type of each index</typeparam>
        /// <param name="indexCount">The number of indices</param>
        /// <param name="type">The type of GPU memory to allocate in</param>
        /// <param name="flags">Any additional allocation flags passed to the allocator</param>
        /// <returns>A new <see cref="IndexBuffer{TIndex}"/></returns>
        public IndexBuffer<TIndex> AllocateIndexBuffer<TIndex>(
            uint indexCount,
            GpuMemoryType type,
            GpuAllocFlags flags = GpuAllocFlags.None
        ) where TIndex : unmanaged
        {
            Debug.Assert(type != GpuMemoryType.CpuReadOptimized);

            var desc = new GpuResourceDesc(
                GpuResourceFormat.Buffer((uint)sizeof(TIndex) * indexCount),
                type,
                type == GpuMemoryType.CpuWriteOptimized ? D3D12_RESOURCE_STATE_GENERIC_READ : D3D12_RESOURCE_STATE_INDEX_BUFFER,
                flags
            );

            return new IndexBuffer<TIndex>(Allocate(desc));
        }

        /// <summary>
        /// Allocates a new region of GPU memory
        /// </summary>
        /// <param name="desc">The description for the resource to allocate</param>
        /// <returns>A new <see cref="GpuResource"/> which encapsulates the allocated region</returns>
        public GpuResource Allocate(
            GpuResourceDesc desc
        )
        {
            VerifyDesc(desc);

            var info = GetAllocationInfo(desc);

            if (desc.AllocFlags.HasFlag(GpuAllocFlags.ForceAllocateComitted))
            {
                return AllocateCommitted(desc, info);
            }

            return AllocatePlacedFromHeap(desc, info);
        }

        private void VerifyDesc(GpuResourceDesc desc)
        {
            var flags = desc.AllocFlags;
            if (flags.HasFlag(GpuAllocFlags.ForceAllocateComitted))
            {
                Debug.Assert(!flags.HasFlag(GpuAllocFlags.ForceAllocateNotComitted));
            }
            else if (flags.HasFlag(GpuAllocFlags.ForceAllocateNotComitted))
            {
                Debug.Assert(!flags.HasFlag(GpuAllocFlags.ForceAllocateComitted));
            }
        }

        private GpuResource AllocateCommitted(GpuResourceDesc desc, D3D12_RESOURCE_ALLOCATION_INFO allocInfo)
        {
            using var device = _device.Copy();

            var heapProperties = GetHeapProperties(desc);

            var clearVal = desc.ClearValue.GetValueOrDefault();

            using ComPtr<ID3D12Resource> resource = default;

            Guard.ThrowIfFailed(device.Get()->CreateCommittedResource(
                 &heapProperties,
                 desc.HeapFlags,
                 &desc.ResourceFormat.D3D12ResourceDesc,
                 desc.InitialState,
                 desc.ClearValue is null ? null : &clearVal,
                 resource.Guid,
                 ComPtr.GetVoidAddressOf(&resource)
            ));

            return new GpuResource(
                resource.Move(),
                desc,
                allocInfo.SizeInBytes,
                0,
                default,
                this
            );
        }

        private D3D12_HEAP_PROPERTIES GetHeapProperties(GpuResourceDesc desc)
        {
            return new D3D12_HEAP_PROPERTIES((D3D12_HEAP_TYPE)desc.GpuMemoryType);
        }

        private bool TryAllocateFromHeap(GpuResourceDesc desc, D3D12_RESOURCE_ALLOCATION_INFO info, AllocatorHeap heap, out GpuResource allocation)
        {
            if (TryGetFreeBlock(heap, info, out HeapBlock freeBlock))
            {
                allocation = CreatePlaced(desc, info, heap, freeBlock);
                return true;
            }

            allocation = default!;
            return false;
        }

        private GpuResource AllocatePlacedFromHeap(GpuResourceDesc desc, D3D12_RESOURCE_ALLOCATION_INFO info)
        {
            GpuResource allocation;
            var heapList = _heaps[desc.GpuMemoryType];
            for (var i = 0; i < heapList.Count; i++)
            {
                if (TryAllocateFromHeap(desc, info, heapList[i], out allocation))
                {
                    return allocation;
                }
            }

            // No free blocks available anywhere. Create a new heap
            var newHeap = CreateNewHeap(desc.GpuMemoryType);
            var result = TryAllocateFromHeap(desc, info, newHeap, out allocation);
            Debug.Assert(result);
            return allocation;
        }

        private GpuResource CreatePlaced(GpuResourceDesc desc, D3D12_RESOURCE_ALLOCATION_INFO allocInfo, AllocatorHeap heap, HeapBlock block)
        {
            using var device = _device.Copy();

            var clearVal = desc.ClearValue.GetValueOrDefault();

            using ComPtr<ID3D12Resource> resource = default;

            Guard.ThrowIfFailed(device.Get()->CreatePlacedResource(
                 heap.Heap.Get(),
                 block.Offset,
                 &desc.ResourceFormat.D3D12ResourceDesc,
                 desc.InitialState,
                 desc.ClearValue is null ? null : &clearVal,
                 resource.Guid,
                 ComPtr.GetVoidAddressOf(&resource)
             ));

            return new GpuResource(
                resource.Move(),
                desc,
                allocInfo.SizeInBytes,
                block.Offset,
                heap,
                this
            );
        }

        private void MarkFreeBlockUsed(AllocatorHeap heap, D3D12_RESOURCE_ALLOCATION_INFO info, HeapBlock freeBlock, HeapBlock allocBlock)
        {
            heap.FreeBlocks.Remove(freeBlock);

            var alignOffset = allocBlock.Offset - freeBlock.Offset;

            if (alignOffset != 0)
            {
                var loBlock = new HeapBlock { Offset = freeBlock.Offset, Size = allocBlock.Offset - freeBlock.Offset };
                heap.FreeBlocks.Add(loBlock);
            }
            if (allocBlock.Size != freeBlock.Size)
            {
                var hiBlock = new HeapBlock { Offset = allocBlock.Offset + info.SizeInBytes, Size = freeBlock.Size - allocBlock.Size - alignOffset };
                heap.FreeBlocks.Add(hiBlock);
            }
        }

        private bool TryGetFreeBlock(AllocatorHeap heap, D3D12_RESOURCE_ALLOCATION_INFO info, out HeapBlock freeBlock)
        {
            Debug.Assert(info.Alignment <= HighestRequiredAlign);

            // try and get a block that is already aligned
            foreach (var block in heap.FreeBlocks)
            {
                if (block.Size >= info.SizeInBytes
                    // because we assume the heap start is aligned, just check if the offset is too
                    && MathHelpers.IsAligned(block.Offset, info.Alignment))
                {
                    freeBlock = block;
                    MarkFreeBlockUsed(heap, info, freeBlock, freeBlock);
                    return true;
                }
            }

            // if that doesn't work, try and find a block big enough and manually align
            foreach (var block in heap.FreeBlocks)
            {
                var alignedOffset = MathHelpers.AlignUp(block.Offset, info.Alignment);
                var alignedSize = block.Size - (alignedOffset - block.Offset);

                if (alignedSize >= info.SizeInBytes)
                {
                    freeBlock = new HeapBlock { Offset = alignedOffset, Size = alignedSize };
                    MarkFreeBlockUsed(heap, info, block, freeBlock);
                    return true;
                }
            }

            freeBlock = default;
            return false;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _device.Dispose();
        }
    }
}
