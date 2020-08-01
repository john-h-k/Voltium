using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Buffer = Voltium.Core.Memory.Buffer;
using System.Linq;
using Voltium.Extensions;

namespace Voltium.Core.Memory
{

    // This type is "semi lowered". It needs high level alloc flags because they don't necessarily have a D3D12 equivalent
    // So we just lower most of it
    internal struct InternalAllocDesc
    {
        public D3D12_RESOURCE_DESC Desc;
        public D3D12_CLEAR_VALUE? ClearValue;
        public D3D12_RESOURCE_STATES InitialState;
        public D3D12_HEAP_TYPE HeapType;

        // Can't immediately lower this. It contains other flags
        public AllocFlags AllocFlags;
    }

    /// <summary>
    /// An allocator used for allocating temporary and long
    /// </summary>
    public unsafe sealed class GpuAllocator : IDisposable
    {
        private List<AllocatorHeap> _readback = new();
        private List<AllocatorHeap> _upload = new();

        // single merged heap
        private List<AllocatorHeap> _default = null!;

        // 3 seperate heaps when merged heap isn't supported
        private List<AllocatorHeap> _buffer = null!;
        private List<AllocatorHeap> _texture = null!;
        private List<AllocatorHeap> _rtOrDs = null!;

        private ComputeDevice _device;
        private const ulong HighestRequiredAlign = 1024 * 1024 * 4; // 4mb

        private bool _hasMergedHeapSupport;

        // useful for debugging
        private static bool ForceAllAllocationsCommitted => false;


        private const D3D12_RESOURCE_FLAGS RenderTargetOrDepthStencilFlags =
            D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL | D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET;

        /// <summary>
        /// Creates a new allocator
        /// </summary>
        /// <param name="device">The <see cref="ID3D12Device"/> to allocate on</param>
        internal GpuAllocator(GraphicsDevice device)
        {
            Debug.Assert(device is not null);
            _device = device;

            D3D12_FEATURE_DATA_D3D12_OPTIONS options = default;
            _device.QueryFeatureSupport(D3D12_FEATURE.D3D12_FEATURE_D3D12_OPTIONS, &options);
            _hasMergedHeapSupport = options.ResourceHeapTier == D3D12_RESOURCE_HEAP_TIER.D3D12_RESOURCE_HEAP_TIER_2;

            if (_hasMergedHeapSupport)
            {
                _default = new();
            }
            else
            {
                _buffer = new();
                _texture = new();
                _rtOrDs = new();
            }
        }

        /// <summary>
        /// Allocates a <see cref="MemoryAccess.CpuUpload"/> buffer and copy initial data to it
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="data"/></typeparam>
        /// <param name="data">The data to copy to the buffer</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Buffer"/> with <paramref name="data"/> copied to it</returns>
        public Buffer AllocateBuffer<T>(
            ReadOnlySpan<T> data,
            AllocFlags allocFlags = AllocFlags.None
        ) where T : unmanaged
        {
            var buff = AllocateBuffer(data.ByteLength(), MemoryAccess.CpuUpload, allocFlags: allocFlags);
            buff.WriteData(data);
            return buff;
        }

        /// <summary>
        /// Allocates a buffer
        /// </summary>
        /// <param name="desc">The <see cref="BufferDesc"/> describing the buffer</param>
        /// <param name="memoryKind">The <see cref="MemoryAccess"/> to allocate the buffer in</param>
        /// <param name="initialResourceState">The initial state of the resource</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Buffer"/></returns>
        public Buffer AllocateBuffer(
            in BufferDesc desc,
            MemoryAccess memoryKind,
            ResourceState initialResourceState = ResourceState.Common,
            AllocFlags allocFlags = AllocFlags.None
        )
            => AllocateBuffer(desc.Length, memoryKind, initialResourceState, desc.ResourceFlags, allocFlags);

        /// <summary>
        /// Allocates a buffer
        /// </summary>
        /// <param name="length">The length, in bytes, to allocate</param>
        /// <param name="memoryKind">The <see cref="MemoryAccess"/> to allocate the buffer in</param>
        /// <param name="initialResourceState">The initial state of the resource</param>
        /// <param name="resourceFlags">Any additional resource flags</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Buffer"/></returns>
        public Buffer AllocateBuffer(
            long length,
            MemoryAccess memoryKind,
            ResourceState initialResourceState = ResourceState.Common,
            ResourceFlags resourceFlags = ResourceFlags.None,
            AllocFlags allocFlags = AllocFlags.None
        )
        {
            if (memoryKind == MemoryAccess.CpuUpload)
            {
                initialResourceState = ResourceState.GenericRead;
            }
            else if (memoryKind == MemoryAccess.CpuReadback)
            {
                initialResourceState = ResourceState.CopyDestination;
            }

            var desc = new D3D12_RESOURCE_DESC
            {
                Width = (ulong)length,
                Height = 1,
                DepthOrArraySize = 1,
                Dimension = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER,
                Flags = (D3D12_RESOURCE_FLAGS)resourceFlags,
                MipLevels = 1,

                // required values for a buffer
                SampleDesc = new DXGI_SAMPLE_DESC(1, 0),
                Layout = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_ROW_MAJOR
            };

            var resource = new InternalAllocDesc
            {
                Desc = desc,
                AllocFlags = allocFlags,
                HeapType = (D3D12_HEAP_TYPE)memoryKind,
                InitialState = (D3D12_RESOURCE_STATES)initialResourceState
            };

            var buffer = new Buffer((ulong)length, Allocate(&resource));
            if (memoryKind == MemoryAccess.CpuUpload)
            {
                buffer.Map(); // we persisitently map upload buffers
            }
            return buffer;
        }
        /// <summary>
        /// Allocates a texture
        /// </summary>
        /// <param name="desc">The <see cref="TextureDesc"/> describing the texture</param>
        /// <param name="initialResourceState">The state of the resource when it is allocated</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Texture"/></returns>
        public Texture AllocateTexture(
            in TextureDesc desc,
            ResourceState initialResourceState,
            AllocFlags allocFlags = AllocFlags.None
        )
        {
            DXGI_SAMPLE_DESC sample = new DXGI_SAMPLE_DESC(desc.Msaa.SampleCount, desc.Msaa.QualityLevel);

            // Normalize default
            if (desc.Msaa.SampleCount == 0)
            {
                sample.Count = 1;
            }

            var resDesc = new D3D12_RESOURCE_DESC
            {
                Dimension = (D3D12_RESOURCE_DIMENSION)desc.Dimension,
                Alignment = 0,
                Width = desc.Width,
                Height = desc.Height,
                DepthOrArraySize = desc.DepthOrArraySize,
                MipLevels = desc.MipCount,
                Format = (DXGI_FORMAT)desc.Format,
                Flags = (D3D12_RESOURCE_FLAGS)desc.ResourceFlags,
                SampleDesc = sample
            };

            D3D12_CLEAR_VALUE clearVal = new D3D12_CLEAR_VALUE { Format = resDesc.Format };

            var val = desc.ClearValue.GetValueOrDefault();
            if (desc.ResourceFlags.HasFlag(ResourceFlags.AllowRenderTarget))
            {
                Unsafe.Write(clearVal.Anonymous.Color, val.Color);
            }
            else if (desc.ResourceFlags.HasFlag(ResourceFlags.AllowDepthStencil))
            {
                clearVal.Anonymous.DepthStencil.Depth = val.Depth;
                clearVal.Anonymous.DepthStencil.Stencil = val.Stencil;
            }

            var resource = new InternalAllocDesc
            {
                Desc = resDesc,
                ClearValue = desc.ClearValue is null ? (D3D12_CLEAR_VALUE?)null : clearVal,
                InitialState = (D3D12_RESOURCE_STATES)initialResourceState,
                AllocFlags = allocFlags,
                HeapType = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_DEFAULT
            };

            return new Texture(desc, Allocate(&resource));
        }

        private const int BufferAlignment = /* 64kb */ 64 * 1024;

        /// <summary>
        /// Allocates a new region of GPU memory
        /// </summary>
        /// <param name="desc">The description for the resource to allocate</param>
        /// <returns>A new <see cref="GpuResource"/> which encapsulates the allocated region</returns>
        internal GpuResource Allocate(
            InternalAllocDesc* desc // we use a pointer here because eventually we have to pass D3D12_RESOURCE_DESC*, so avoid unnecessary pinning
        )
        {
            VerifyDesc(desc);

            if (ShouldCommitResource(desc))
            {
                return AllocateCommitted(desc);
            }

            D3D12_RESOURCE_ALLOCATION_INFO info;

            // avoid native call as we don't need to for buffers
            if (desc->Desc.Dimension == D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER)
            {
                info = new D3D12_RESOURCE_ALLOCATION_INFO(desc->Desc.Width, BufferAlignment);
            }
            else
            {
                info = _device.GetAllocationInfo(desc);
            }
            var res =  AllocatePlacedFromHeap(desc, info);
            return res;
        }

        private static bool IsRenderTargetOrDepthStencil(D3D12_RESOURCE_FLAGS flags)
            => (flags & RenderTargetOrDepthStencilFlags) != 0;

        private bool ShouldCommitResource(InternalAllocDesc* desc)
        {
            return ForceAllAllocationsCommitted || IsRenderTargetOrDepthStencil(desc->Desc.Flags) || desc->AllocFlags.HasFlag(AllocFlags.ForceAllocateComitted);
        }

        private void VerifyDesc(InternalAllocDesc* desc)
        {
            var flags = desc->AllocFlags;
            if (flags.HasFlag(AllocFlags.ForceAllocateComitted) && flags.HasFlag(AllocFlags.ForceAllocateNotComitted))
            { 
                ThrowHelper.ThrowArgumentException("Invalid to say 'ForceAllocateComitted' and 'ForceAllocateNotComitted'");
            }
        }

        private ref List<AllocatorHeap> GetHeap(GpuResource allocation)
        {
            D3D12_HEAP_PROPERTIES props;
            D3D12_HEAP_FLAGS flags;
            Guard.ThrowIfFailed(allocation.GetResourcePointer()->GetHeapProperties(&props, &flags));
            var desc = allocation.GetResourcePointer()->GetDesc();

            return ref GetHeapPool(props.Type, GetResType(desc.Dimension, desc.Flags));
        }

        private ref List<AllocatorHeap> GetHeapPool(D3D12_HEAP_TYPE mem, GpuResourceType res)
        {
            switch (mem)
            {
                case D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_READBACK:
                    return ref _readback;
                case D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_UPLOAD:
                    return ref _upload;
                case D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_DEFAULT:
                    switch (res)
                    {
                        // meaningless is the value if we have merged heap support
                        case GpuResourceType.Meaningless:
                            return ref _default;
                        case GpuResourceType.Tex:
                            return ref _texture;
                        case GpuResourceType.RenderTargetOrDepthStencilTexture:
                            return ref _rtOrDs;
                        case GpuResourceType.Buffer:
                            return ref _buffer;
                    }
                    break;
            }

            return ref Helpers.NullRef<List<AllocatorHeap>>();
        }

        private const ulong DefaultHeapAlignment = Windows.D3D12_DEFAULT_MSAA_RESOURCE_PLACEMENT_ALIGNMENT; // 4mb for MSAA textures
        private ref AllocatorHeap CreateNewHeap(D3D12_HEAP_TYPE mem, GpuResourceType res, out int index)
        {
            D3D12_HEAP_FLAGS flags = default;

            if (!_hasMergedHeapSupport)
            {
                flags = res switch
                {
                    GpuResourceType.Meaningless => 0, // shouldn't be reached
                    GpuResourceType.Tex => D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES | D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_DENY_BUFFERS,
                    GpuResourceType.RenderTargetOrDepthStencilTexture => D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES | D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_DENY_BUFFERS,
                    GpuResourceType.Buffer => D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES | D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES,
                    _ => 0, // shouldn't be reached
                };
            }

            var props = new D3D12_HEAP_PROPERTIES(mem);
            var desc = new D3D12_HEAP_DESC(GetNewHeapSize(mem, res), props, alignment: DefaultHeapAlignment, flags: flags);

            var heap = _device.CreateHeap(&desc);

            var allocatorHeap = new AllocatorHeap { Heap = heap.Move(), FreeBlocks = new() };
            AddFreeBlock(ref allocatorHeap, new HeapBlock { Offset = 0, Size = desc.SizeInBytes });

            ref var pool = ref GetHeapPool(mem, res);
            pool.Add(allocatorHeap);

            index = pool.Count - 1;

            return ref ListExtensions.GetRef(pool, pool.Count - 1);
        }

        private ulong GetNewHeapSize(D3D12_HEAP_TYPE mem, GpuResourceType res)
        {
            const ulong megabyte = 1024 * 1024;
            //const ulong kilobyte = 1024;

            var guess = res switch
            {
                GpuResourceType.Meaningless => 256 * megabyte,
                GpuResourceType.Tex => 256 * megabyte,
                // GpuResourceType.RtOrDs => 64 * megabyte,
                GpuResourceType.RenderTargetOrDepthStencilTexture => 128 * megabyte,
                GpuResourceType.Buffer => 64 * megabyte,
                _ => ulong.MaxValue, // shouldn't be reached
            };

            return guess;
        }

        private const int CommittedResourceHeapIndex = -1;
        internal void Return(GpuResource gpuAllocation)
        {
            if (gpuAllocation.HeapIndex != CommittedResourceHeapIndex)
            {
                ref var heap = ref GetHeap(gpuAllocation).AsSpan()[gpuAllocation.HeapIndex];

                Release(gpuAllocation);

                ReturnPlacedAllocation(gpuAllocation, ref heap);
            }
            else
            {
                Release(gpuAllocation);
            }

            static void Release(GpuResource alloc)
            {
                var refCount = alloc.GetResourcePointer()->Release();
                Debug.Assert(refCount == 0);
                _ = refCount;
            }
        }

        private void ReturnPlacedAllocation(GpuResource gpuAllocation, ref AllocatorHeap heap)
        {
            AddFreeBlock(ref heap, gpuAllocation.Block);
            // TODO defrag
        }

        private GpuResource AllocateCommitted(InternalAllocDesc* desc)
        {
            var resource = _device.CreateCommittedResource(desc);

            return new GpuResource(
                resource.Move(),
                *desc,
                null,
                CommittedResourceHeapIndex
            );
        }

        private bool TryAllocateFromHeap(InternalAllocDesc* desc, D3D12_RESOURCE_ALLOCATION_INFO info, ref AllocatorHeap heap, int heapIndex, out GpuResource allocation)
        {
            if (TryGetFreeBlock(ref heap, info, out HeapBlock freeBlock))
            {
                allocation = CreatePlaced(desc, ref heap, heapIndex, freeBlock);
                return true;
            }

            allocation = default!;
            return false;
        }

        private GpuResource AllocatePlacedFromHeap(InternalAllocDesc* desc, D3D12_RESOURCE_ALLOCATION_INFO info)
        {
            var resType = GetResType(desc->Desc.Dimension, desc->Desc.Flags);
            GpuResource allocation;
            ref var heapList = ref GetHeapPool(desc->HeapType, resType);
            for (var i = 0; i < heapList.Count; i++)
            {
                if (TryAllocateFromHeap(desc, info, ref ListExtensions.GetRef(heapList, i), i, out allocation))
                {
                    return allocation;
                }
            }

            // No free blocks available anywhere. Create a new heap
            ref var newHeap = ref CreateNewHeap(desc->HeapType, resType, out int index);
            var result = TryAllocateFromHeap(desc, info, ref newHeap, index, out allocation);
            if (!result) // too big to fit in heap, realloc as comitted
            {
                if (desc->AllocFlags.HasFlag(AllocFlags.ForceAllocateNotComitted))
                {
                    ThrowHelper.ThrowInsufficientMemoryException(
                        $"Could not satisfy allocation - required {info.SizeInBytes} bytes, but this " +
                        "is larget than the maximum heap size, and AllocFlags.ForceAllocateNonComitted prevented it being allocated committed"
                    );
                }
                return AllocateCommitted(desc);
            }
            return allocation;
        }

        private GpuResourceType GetResType(D3D12_RESOURCE_DIMENSION dimension, D3D12_RESOURCE_FLAGS flags)
        {
            if (_hasMergedHeapSupport)
            {
                return GpuResourceType.Meaningless;
            }

            if (dimension == D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER)
            {
                return GpuResourceType.Buffer;
            }
            if (IsRenderTargetOrDepthStencil(flags))
            {
                return GpuResourceType.RenderTargetOrDepthStencilTexture;
            }
            return GpuResourceType.Tex;
        }

        private GpuResource CreatePlaced(InternalAllocDesc* desc, ref AllocatorHeap heap, int heapIndex, HeapBlock block)
        {
            var resource = _device.CreatePlacedResource(heap.Heap.Get(), block.Offset, desc);

            return new GpuResource(
                resource.Move(),
                *desc,
                this,
                heapIndex,
                block
            );
        }

        private void MarkFreeBlockUsed(ref AllocatorHeap heap, HeapBlock wholeBlock, HeapBlock usedBlock)
        {
            ValidateFreeBlock(ref heap, usedBlock);

            var removed = heap.FreeBlocks.Remove(wholeBlock);
            Debug.Assert(removed);
            _ = removed;

            var alignOffset = usedBlock.Offset - wholeBlock.Offset;

            if (alignOffset != 0)
            {
                var loBlock = new HeapBlock { Offset = wholeBlock.Offset, Size = alignOffset };
                AddFreeBlock(ref heap, loBlock);
            }

            if (usedBlock.Size + usedBlock.Offset != wholeBlock.Size + wholeBlock.Offset)
            {
                var hiBlock = new HeapBlock { Offset = usedBlock.Offset + usedBlock.Size, Size = wholeBlock.Size - (usedBlock.Size + alignOffset) };
                AddFreeBlock(ref heap, hiBlock);
            }
        }

        private void AddFreeBlock(ref AllocatorHeap heap, HeapBlock block)
        {
            ValidateFreeBlock(ref heap, block);

            heap.FreeBlocks.Add(block);
        }

        [Conditional("HEAP_VERIFY")]
        private void ValidateFreeBlock(ref AllocatorHeap heap, HeapBlock block)
        {
            var check = heap.Heap.Get()->GetDesc().SizeInBytes >= block.Offset + block.Size;
            Debug.Assert(check, "Invalid free block added");
            _ = check;

            VerifyHeapSlow(ref heap);
        }

        private void VerifyHeapSlow(ref AllocatorHeap heap)
        {
            foreach (var block in heap.FreeBlocks)
            {
                bool dupFound = false;
                foreach (var otherBlock in heap.FreeBlocks)
                {
                    if (block == otherBlock)
                    {
                        if (dupFound)
                        {
                            // duplicate block
                            LogHeapCorruption($"Duplicate heap block found - block (Offset = {block.Offset}, Size = {block.Size}) was found twice");
                            Debugger.Break();
                        }
                        dupFound = true;
                    }

                    if ((block.Offset < otherBlock.Offset && block.Offset + block.Size > otherBlock.Offset) ||
                        (otherBlock.Offset < block.Offset && otherBlock.Offset + otherBlock.Size > block.Offset))
                    {
                        // overlapping blocks
                        LogHeapCorruption($"Overlapping heap blocks found - block (Offset = {block.Offset}, Size = {block.Size}) overlaps with " +
                            $"block (Offset = {otherBlock.Offset}, Size = {otherBlock.Size})");
                        Debugger.Break();
                    }
                }
            }    
        }

        private void LogHeapCorruption(string message)
        {
            LogHelper.LogCritical("HEAP_CORRUPTION: " + message);
        }

        private bool TryGetFreeBlock(ref AllocatorHeap heap, D3D12_RESOURCE_ALLOCATION_INFO info, out HeapBlock freeBlock)
        {
            Debug.Assert(info.Alignment <= HighestRequiredAlign);

            // try and get a block that is already aligned
            for (var i = heap.FreeBlocks.Count - 1; i >= 0; i--)
            {
                var block = heap.FreeBlocks[i];

                if (block.Size >= info.SizeInBytes
                    // because we assume the heap start is aligned, just check if the offset is too
                    && MathHelpers.IsAligned(block.Offset, info.Alignment))
                {
                    freeBlock = new HeapBlock { Offset = block.Offset, Size = info.SizeInBytes };
                    MarkFreeBlockUsed(ref heap, block, freeBlock);
                    return true;
                }
            }

            // if that doesn't work, try and find a block big enough and manually align
            foreach (var block in heap.FreeBlocks)
            {
                var alignedOffset = MathHelpers.AlignUp(block.Offset, info.Alignment);
                var alignmentPadding = alignedOffset - block.Offset;

                var alignedSize = block.Size - alignmentPadding;

                // we need to ensure we don't wrap around if alignmentPadding is greater than the block size, so we check that first

                if (alignmentPadding > block.Size && alignedSize >= info.SizeInBytes)
                {
                    freeBlock = new HeapBlock { Offset = alignedOffset, Size = alignedSize };
                    MarkFreeBlockUsed(ref heap, block, freeBlock);
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
