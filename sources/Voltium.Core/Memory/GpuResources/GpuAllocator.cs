using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Loader;
using TerraFX.Interop;
using static TerraFX.Interop.D3D12_RESOURCE_STATES;
using Voltium.Common;
using Voltium.Core.Memory.GpuResources.ResourceViews;
using System.Runtime.InteropServices;
using Voltium.Core.Managers;

namespace Voltium.Core.GpuResources
{
    /// <summary>
    /// An allocator used for allocating temporary and long
    /// </summary>
    public unsafe sealed class GpuAllocator : IDisposable
    {
        private List<AllocatorHeap>[] _heapPools = null!;
        private GraphicsDevice _device;
        private const ulong HighestRequiredAlign = 1024 * 1024 * 4; // 4mb

        private bool _hasMergedHeapSupport;

        /// <summary>
        /// Creates a new allocator
        /// </summary>
        /// <param name="device">The <see cref="ID3D12Device"/> to allocate on</param>
        public GpuAllocator(GraphicsDevice device)
        {
            Debug.Assert(device is object);
            _device = device;

            _hasMergedHeapSupport = CheckMergedHeapSupport(_device.Device);

            CreateOriginalHeaps();
        }

        private bool CheckMergedHeapSupport(ID3D12Device* device)
        {
            D3D12_FEATURE_DATA_D3D12_OPTIONS levels;

            Guard.ThrowIfFailed(device->CheckFeatureSupport(
                D3D12_FEATURE.D3D12_FEATURE_D3D12_OPTIONS,
                &levels,
                (uint)sizeof(D3D12_FEATURE_DATA_D3D12_OPTIONS)
            ));

            return levels.ResourceHeapTier == D3D12_RESOURCE_HEAP_TIER.D3D12_RESOURCE_HEAP_TIER_2;
        }

        private void CreateOriginalHeaps()
        {
            if (_hasMergedHeapSupport)
            {
                // one for each memory type (default, upload, readback)
                _heapPools = new List<AllocatorHeap>[3];
            }
            else
            {
                // three for each memory type (default, upload, readback),
                // - where those three are for buffers, render targets/depth stencils, and textures
                _heapPools = new List<AllocatorHeap>[9];
            }

            for (var i = GpuMemoryType.GpuOnly; i <= GpuMemoryType.CpuReadback; i++)
            {
                if (_hasMergedHeapSupport)
                {
                    ref var list = ref _heapPools[GetHeapIndex(i, GpuResourceType.Meaningless)];
                    list = new(1);
                    list.Add(CreateNewHeap(i, GpuResourceType.Meaningless));
                }
                else
                {
                    for (var j = GpuResourceType.Tex; j <= GpuResourceType.Buffer; j++)
                    {
                        ref var list = ref _heapPools[GetHeapIndex(i, j)];
                        list = new(1);
                        list.Add(CreateNewHeap(i, j));
                    }
                }
            }
        }

        private void GetHeapType(int index, out GpuMemoryType mem, out GpuResourceType res)
        {
            if (_hasMergedHeapSupport)
            {
                mem = (GpuMemoryType)index + 1;
                res = GpuResourceType.Meaningless;
                return;
            }

            mem = (GpuMemoryType)(index / 3) + 1;
            res = GpuResourceType.Meaningless;
        }

        private int GetHeapIndex(GpuMemoryType mem, GpuResourceType res)
        {
            if (_hasMergedHeapSupport)
            {
                return (int)(mem - 1);
            }

            var ind = (((int)mem - 1) * 3) + (int)(res - 1);
            return ind;
        }

        private AllocatorHeap CreateNewHeap(GpuMemoryType mem, GpuResourceType res)
        {
            D3D12_HEAP_FLAGS flags = default;

            if (!_hasMergedHeapSupport)
            {
                flags = res switch
                {
                    GpuResourceType.Meaningless => (D3D12_HEAP_FLAGS)0, // shouldn't be reached
                    GpuResourceType.Tex => D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES | D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_DENY_BUFFERS,
                    GpuResourceType.RtOrDs => D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES | D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_DENY_BUFFERS,
                    GpuResourceType.Buffer => D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES | D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES,
                    _ => (D3D12_HEAP_FLAGS)0, // shouldn't be reached
                };
            }

            D3D12_HEAP_PROPERTIES props = new((D3D12_HEAP_TYPE)mem);
            D3D12_HEAP_DESC desc = new(GetNewHeapSize(mem, res), props, flags: flags);

            ComPtr<ID3D12Heap> heap = default;
            Guard.ThrowIfFailed(_device.Device->CreateHeap(&desc, heap.Guid, ComPtr.GetVoidAddressOf(&heap)));

            var allocatorHeap = new AllocatorHeap { Heap = heap.Move(), FreeBlocks = new() };
            bool result = allocatorHeap.FreeBlocks.Add(new HeapBlock { Offset = 0, Size = desc.SizeInBytes });
            Debug.Assert(result);
            _ = result;

            return allocatorHeap;
        }

        private ulong GetNewHeapSize(GpuMemoryType mem, GpuResourceType res)
        {
            const ulong megabyte = 1024 * 1024;
            //const ulong kilobyte = 1024;

            var guess = res switch
            {
                GpuResourceType.Meaningless => 256 * megabyte , // shouldn't be reached
                GpuResourceType.Tex => 64 * megabyte,
                GpuResourceType.RtOrDs => 64 * megabyte,
                GpuResourceType.Buffer => 64 * megabyte,
                _ => ulong.MaxValue, // shouldn't be reached
            };

            return guess;
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
            return _device.Device->GetResourceAllocationInfo(0 /* TODO: MULTI-GPU */, 1, &desc.ResourceFormat.D3D12ResourceDesc);
        }

        /// <inheritdoc cref="AllocateVertexBuffer{TVertex}(ReadOnlySpan{TVertex}, GpuMemoryType, GpuAllocFlags)" />
        public VertexBuffer<TVertex> AllocateVertexBuffer<TVertex>(
            TVertex[] initialData,
            GpuMemoryType type,
            GpuAllocFlags flags = GpuAllocFlags.None
        ) where TVertex : unmanaged
            => AllocateVertexBuffer(initialData.AsSpan(), type, flags);


        /// <inheritdoc cref="AllocateVertexBuffer{TVertex}(ReadOnlySpan{TVertex}, GpuMemoryType, GpuAllocFlags)" />
        public VertexBuffer<TVertex> AllocateVertexBuffer<TVertex>(
            Span<TVertex> initialData,
            GpuMemoryType type,
            GpuAllocFlags flags = GpuAllocFlags.None
        ) where TVertex : unmanaged
            => AllocateVertexBuffer((ReadOnlySpan<TVertex>)initialData, type, flags);

        /// <summary>
        /// Allocates a new <see cref="VertexBuffer{TVertex}"/>
        /// </summary>
        /// <typeparam name="TVertex">The type of each vertex</typeparam>
        /// <param name="initialData">The initial vertices data, which will be copied to the resource</param>
        /// <param name="type">The type of GPU memory to allocate in</param>
        /// <param name="flags">Any additional allocation flags passed to the allocator</param>
        /// <returns>A new <see cref="VertexBuffer{TVertex}"/></returns>
        public VertexBuffer<TVertex> AllocateVertexBuffer<TVertex>(
            ReadOnlySpan<TVertex> initialData,
            GpuMemoryType type,
            GpuAllocFlags flags = GpuAllocFlags.None
        ) where TVertex : unmanaged
        {
            var resource = AllocateVertexBuffer<TVertex>((uint)initialData.Length, type, flags);

            resource.Map();
            initialData.CopyTo(resource.Vertices);
            resource.Unmap();

            return resource;
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
            Debug.Assert(type != GpuMemoryType.CpuReadback);

            var desc = new GpuResourceDesc(
                GpuResourceFormat.Buffer((uint)sizeof(TVertex) * vertexCount),
                type,   
                type == GpuMemoryType.CpuUpload ? ResourceState.GenericRead : ResourceState.VertexBuffer,
                flags
            );

            return new VertexBuffer<TVertex>(Allocate(desc));
        }


        /// <inheritdoc cref="AllocateIndexBuffer{TIndex}(ReadOnlySpan{TIndex}, GpuMemoryType, GpuAllocFlags)" />
        public IndexBuffer<TIndex> AllocateIndexBuffer<TIndex>(
            TIndex[] initialData,
            GpuMemoryType type,
            GpuAllocFlags flags = GpuAllocFlags.None
        ) where TIndex : unmanaged
            => AllocateIndexBuffer(initialData.AsSpan(), type, flags);


        /// <inheritdoc cref="AllocateIndexBuffer{TIndex}(ReadOnlySpan{TIndex}, GpuMemoryType, GpuAllocFlags)" />
        public IndexBuffer<TIndex> AllocateIndexBuffer<TIndex>(
            Span<TIndex> initialData,
            GpuMemoryType type,
            GpuAllocFlags flags = GpuAllocFlags.None
        ) where TIndex : unmanaged
            => AllocateIndexBuffer((ReadOnlySpan<TIndex>)initialData, type, flags);

        /// <summary>
        /// Allocates a new <see cref="IndexBuffer{TIndex}"/>
        /// </summary>
        /// <typeparam name="TIndex">The type of each index</typeparam>
        /// <param name="initialData">The initial indices data, which will be copied to the resource</param>
        /// <param name="type">The type of GPU memory to allocate in</param>
        /// <param name="flags">Any additional allocation flags passed to the allocator</param>
        /// <returns>A new <see cref="IndexBuffer{TIndex}"/></returns>
        public IndexBuffer<TIndex> AllocateIndexBuffer<TIndex>(
            ReadOnlySpan<TIndex> initialData,
            GpuMemoryType type,
            GpuAllocFlags flags = GpuAllocFlags.None
        ) where TIndex : unmanaged
        {
            var resource = AllocateIndexBuffer<TIndex>((uint)initialData.Length, type, flags);

            resource.Map();
            initialData.CopyTo(resource.Indices);
            resource.Unmap();

            return resource;
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
            Debug.Assert(type != GpuMemoryType.CpuReadback);

            var desc = new GpuResourceDesc(
                GpuResourceFormat.Buffer((uint)sizeof(TIndex) * indexCount),
                type,
                type == GpuMemoryType.CpuUpload ? ResourceState.GenericRead : ResourceState.IndexBuffer,
                flags
            );

            return new IndexBuffer<TIndex>(Allocate(desc));
        }

        /// <inheritdoc cref="AllocateConstantBuffer{TBuffer}(ReadOnlySpan{TBuffer}, GpuMemoryType, GpuAllocFlags)" />
        public ConstantBuffer<TBuffer> AllocateConstantBuffer<TBuffer>(
            TBuffer[] initialData,
            GpuMemoryType type,
            GpuAllocFlags flags = GpuAllocFlags.None
        ) where TBuffer : unmanaged
            => AllocateConstantBuffer(initialData.AsSpan(), type, flags);


        /// <inheritdoc cref="AllocateConstantBuffer{TBuffer}(ReadOnlySpan{TBuffer}, GpuMemoryType, GpuAllocFlags)" />
        public ConstantBuffer<TBuffer> AllocateConstantBuffer<TBuffer>(
            Span<TBuffer> initialData,
            GpuMemoryType type,
            GpuAllocFlags flags = GpuAllocFlags.None
        ) where TBuffer : unmanaged
            => AllocateConstantBuffer((ReadOnlySpan<TBuffer>)initialData, type, flags);

        /// <summary>
        /// Allocates a new <see cref="ConstantBuffer{TBuffer}"/>
        /// </summary>
        /// <typeparam name="TBuffer">The type of each constant buffer</typeparam>
        /// <param name="initialData">The initial buffer data, which will be copied to the resource</param>
        /// <param name="type">The type of GPU memory to allocate in</param>
        /// <param name="flags">Any additional allocation flags passed to the allocator</param>
        /// <returns>A new <see cref="ConstantBuffer{TBuffer}"/></returns>
        public ConstantBuffer<TBuffer> AllocateConstantBuffer<TBuffer>(
            ReadOnlySpan<TBuffer> initialData,
            GpuMemoryType type,
            GpuAllocFlags flags = GpuAllocFlags.None
        ) where TBuffer : unmanaged
        {
            var resource = AllocateConstantBuffer<TBuffer>((uint)initialData.Length, type, flags);

            resource.Map();

            if (sizeof(TBuffer) % 256 == 0)
            {
                // the types are blittable in memory and we can directly copy
                initialData.CopyTo(MemoryMarshal.Cast<byte, TBuffer>(resource.Buffers.GetUntypedData()));
            }
            else
            {
                var buffers = resource.Buffers;
                for (var i = 0; i < initialData.Length; i++)
                {
                    buffers[i] = initialData[i];
                }
            }    

            resource.Unmap();

            return resource;
        }

        /// <summary>
        /// Allocates a new <see cref="ConstantBuffer{TBuffer}"/>
        /// </summary>
        /// <typeparam name="TBuffer">The type of each buffer</typeparam>
        /// <param name="bufferCount">The number of buffers</param>
        /// <param name="type">The type of GPU memory to allocate in</param>
        /// <param name="flags">Any additional allocation flags passed to the allocator</param>
        /// <returns>A new <see cref="ConstantBuffer{TBuffer}"/></returns>
        public ConstantBuffer<TBuffer> AllocateConstantBuffer<TBuffer>(
            uint bufferCount,
            GpuMemoryType type,
            GpuAllocFlags flags = GpuAllocFlags.None
        ) where TBuffer : unmanaged
        {
            Debug.Assert(type != GpuMemoryType.CpuReadback);

            var desc = new GpuResourceDesc(
                GpuResourceFormat.Buffer(CalculateConstantBufferSize(sizeof(TBuffer)) * bufferCount),
                type,
                type == GpuMemoryType.CpuUpload ? ResourceState.GenericRead : ResourceState.ConstantBuffer,
                flags
            );

            return new ConstantBuffer<TBuffer>(Allocate(desc));
        }

        private static uint CalculateConstantBufferSize(int size)
            => (uint)((size + 255) & ~255);

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
            var heapProperties = GetHeapProperties(desc);

            var clearVal = desc.ClearValue.GetValueOrDefault();

            using ComPtr<ID3D12Resource> resource = default;

            Guard.ThrowIfFailed(_device.Device->CreateCommittedResource(
                 &heapProperties,
                 desc.HeapFlags,
                 &desc.ResourceFormat.D3D12ResourceDesc,
                 (D3D12_RESOURCE_STATES)desc.InitialState,
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
            var resType = GetResType(desc);
            GpuResource allocation;
            var heapList = _heapPools[GetHeapIndex(desc.GpuMemoryType, resType)];
            for (var i = 0; i < heapList.Count; i++)
            {
                if (TryAllocateFromHeap(desc, info, heapList[i], out allocation))
                {
                    return allocation;
                }
            }

            // No free blocks available anywhere. Create a new heap
            var newHeap = CreateNewHeap(desc.GpuMemoryType, resType);
            var result = TryAllocateFromHeap(desc, info, newHeap, out allocation);
            Debug.Assert(result);
            return allocation;
        }

        private GpuResourceType GetResType(GpuResourceDesc desc)
        {
            const D3D12_RESOURCE_FLAGS rtOrDsFlags =
                D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL | D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET;

            if (_hasMergedHeapSupport)
            {
                return GpuResourceType.Meaningless;
            }

            if (desc.ResourceFormat.D3D12ResourceDesc.Dimension == D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER)
            {
                return GpuResourceType.Buffer;
            }
            if ((desc.ResourceFormat.D3D12ResourceDesc.Flags & rtOrDsFlags) != 0)
            {
                return GpuResourceType.RtOrDs;
            }
            return GpuResourceType.Tex;
        }

        private GpuResource CreatePlaced(GpuResourceDesc desc, D3D12_RESOURCE_ALLOCATION_INFO allocInfo, AllocatorHeap heap, HeapBlock block)
        {
            var clearVal = desc.ClearValue.GetValueOrDefault();

            using ComPtr<ID3D12Resource> resource = default;

            Guard.ThrowIfFailed(_device.Device->CreatePlacedResource(
                 heap.Heap.Get(),
                 block.Offset,
                 &desc.ResourceFormat.D3D12ResourceDesc,
                 (D3D12_RESOURCE_STATES)desc.InitialState,
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
