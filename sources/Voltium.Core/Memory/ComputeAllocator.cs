using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12_RESOURCE_FLAGS;
using Voltium.Common;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Buffer = Voltium.Core.Memory.Buffer;
using ResourceType = Voltium.Core.Devices.ResourceType;


using Voltium.Extensions;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using Voltium.Annotations;
using System.Threading;
using Microsoft.Toolkit.HighPerformance.Extensions;

using SysDebug = System.Diagnostics.Debug;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Collections.Extensions;
using Voltium.Core.CommandBuffer;

namespace Voltium.Core.Memory
{
    internal struct InternalAllocDesc
    {
        public ResourceDesc Desc;
        public ResourceState InitialState;
        public ResourceFlags Flags;
        public ulong Size, Alignment;

        // Can't immediately lower this. Contains other flags
        public AllocFlags AllocFlags;
        // The alignment/type/access required
        public HeapInfo HeapInfo;
        public bool IsBufferSuballocationCandidate;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct ResourceDesc
    {
        [FieldOffset(0)]
        public ResourceType Type;

        [FieldOffset(sizeof(ResourceType))]
        public TextureDesc Texture;
        [FieldOffset(sizeof(ResourceType))]
        public BufferDesc Buffer;

        public ResourceDesc(BufferDesc buffer)
        {
            Unsafe.SkipInit(out this);
            Type = ResourceType.Buffer;
            Buffer = buffer;
        }

        public ResourceDesc(in TextureDesc texture)
        {
            Unsafe.SkipInit(out this);
            Type = ComputeAllocator.IsRenderTargetOrDepthStencil(texture.ResourceFlags) ? ResourceType.RenderTargetOrDepthStencilTexture : ResourceType.Texture;
            Texture = texture;
        }
    }


    /// <summary>
    /// An allocator used for allocating temporary and long
    /// </summary>
    public unsafe class ComputeAllocator : IDisposable
    {
        private ComputeDevice _device;

        // 3 seperate heaps when merged heap isn't supported
        private protected List<AllocatorHeap> _buffer;
        private protected List<AllocatorHeap>? _readback;
        private protected List<AllocatorHeap>? _upload;

        private protected List<AllocatorHeap> _4kbTextures = null!;
        private protected List<AllocatorHeap> _64kbTextures = null!;
        private protected List<AllocatorHeap> _4mbTextures = null!;

        private protected List<AllocatorHeap> _64kbRtOrDs = null!;
        private protected List<AllocatorHeap> _4mbRtOrDs = null!;

        private protected List<AllocatorHeap>? _accelerationStructureHeap;

        private protected struct AllocationInfo
        {
            public bool HasImplicitHeap => HeapIndex == uint.MaxValue;

            public uint HeapIndex;
            public HeapInfo HeapInfo;
            public ulong Alignment, Offset, Length;
        }

        private protected DictionarySlim<ResourceHandle, AllocationInfo> _allocationInfo = new();

        [MemberNotNullWhen(false, nameof(_64kbRtOrDs), nameof(_4mbRtOrDs))]
        private protected bool _hasMergedHeapSupport { get; set; }

        [MemberNotNullWhen(true, nameof(_accelerationStructureHeap))]
        private protected bool _hasRaytracingSupport { get; set; }



        [MemberNotNullWhen(false, nameof(_readback), nameof(_upload))]
        private protected bool _isCacheCoherentUma { get; set; }

        // useful for debugging
        private protected static bool ForceAllAllocationsCommitted => false;
        private protected static bool ForceNoBufferSuballocations => false;

        /// <summary>
        /// Whether <see cref="MemoryAccess.GpuOnly"/> resources are actually CPU accessible.
        /// This occurs as an optimisation to enable zero-copy resource transfer on Cache Coherent UMA architectures
        /// </summary>
        public bool GpuOnlyResourcesAreWritable => _isCacheCoherentUma;

        public ComputeAllocator(ComputeDevice device)
        {
            _device = device;

            _isCacheCoherentUma = false; // TODO

            _buffer = new();
            if (!_isCacheCoherentUma)
            {
                _readback = new();
                _upload = new();
            }
        }

        /// <summary>
        /// Allocates a <see cref="MemoryAccess.CpuUpload"/> buffer
        /// </summary>
        /// <typeparam name="T">The type of the elements of the buffer</typeparam>
        /// <param name="count">The number of <typeparamref name="T"/>s to allocate</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Buffer"/></returns>
        public Buffer AllocateDefaultBuffer<T>(
            int count = 1,
            AllocFlags allocFlags = AllocFlags.None
        ) where T : unmanaged
        {
            var buff = AllocateBuffer(sizeof(T) * count, MemoryAccess.GpuOnly, allocFlags: allocFlags);
            return buff;
        }

        /// <summary>
        /// Allocates a <see cref="MemoryAccess.CpuUpload"/> buffer
        /// </summary>
        /// <param name="count">The number of bytes to allocate</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Buffer"/></returns>
        public Buffer AllocateDefaultBuffer(
            int count = 1,
            AllocFlags allocFlags = AllocFlags.None
        )
        {
            var buff = AllocateBuffer(count, MemoryAccess.GpuOnly, allocFlags: allocFlags);
            return buff;
        }

        /// <summary>
        /// Allocates a <see cref="MemoryAccess.CpuUpload"/> buffer
        /// </summary>
        /// <typeparam name="T">The type of the elements of the buffer</typeparam>
        /// <param name="count">The number of <typeparamref name="T"/>s to allocate</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Buffer"/></returns>
        public Buffer AllocateReadbackBuffer<T>(
            int count = 1,
            AllocFlags allocFlags = AllocFlags.None
        ) where T : unmanaged
        {
            var buff = AllocateBuffer(sizeof(T) * count, MemoryAccess.CpuReadback, allocFlags: allocFlags);
            return buff;
        }

        /// <summary>
        /// Allocates a <see cref="MemoryAccess.CpuUpload"/> buffer
        /// </summary>
        /// <param name="count">The number of bytes to allocate</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Buffer"/></returns>
        public Buffer AllocateReadbackBuffer(
            int count = 1,
            AllocFlags allocFlags = AllocFlags.None
        )
            => AllocateBuffer(count, MemoryAccess.CpuReadback, allocFlags: allocFlags);


        /// <summary>
        /// Allocates a <see cref="MemoryAccess.CpuUpload"/> buffer and copy initial data to it
        /// </summary>
        /// <param name="count">The number of bytes to allocate</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Buffer"/></returns>
        public Buffer AllocateUploadBuffer<T>(
            int count = 1,
            AllocFlags allocFlags = AllocFlags.None
        )
            => AllocateUploadBuffer(Unsafe.SizeOf<T>() * count, allocFlags);

        /// <summary>
        /// Allocates a <see cref="MemoryAccess.CpuUpload"/> buffer and copy initial data to it
        /// </summary>
        /// <param name="count">The number of bytes to allocate</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Buffer"/></returns>
        public Buffer AllocateUploadBuffer(
            uint count = 1,
            AllocFlags allocFlags = AllocFlags.None
        )
            => AllocateBuffer(count, MemoryAccess.CpuUpload, allocFlags: allocFlags);

        /// <summary>
        /// Allocates a <see cref="MemoryAccess.CpuUpload"/> buffer and copy initial data to it
        /// </summary>
        /// <param name="count">The number of bytes to allocate</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Buffer"/></returns>
        public Buffer AllocateUploadBuffer(
            int count = 1,
            AllocFlags allocFlags = AllocFlags.None
        )
            => AllocateBuffer(count, MemoryAccess.CpuUpload, allocFlags: allocFlags);

        /// <inheritdoc cref="AllocateUploadBuffer{T}(ReadOnlySpan{T}, AllocFlags)"/>
        public Buffer AllocateUploadBuffer<T>(
            T[] data,
            AllocFlags allocFlags = AllocFlags.None
        ) where T : unmanaged
            => AllocateUploadBuffer((ReadOnlySpan<T>)data, allocFlags);

        /// <inheritdoc cref="AllocateUploadBuffer{T}(ReadOnlySpan{T}, AllocFlags)"/>
        public Buffer AllocateUploadBuffer<T>(
            Span<T> data,
            AllocFlags allocFlags = AllocFlags.None
        ) where T : unmanaged
            => AllocateUploadBuffer((ReadOnlySpan<T>)data, allocFlags);

        /// <summary>
        /// Allocates a <see cref="MemoryAccess.CpuUpload"/> buffer and copy initial data to it
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="data"/></typeparam>
        /// <param name="data">The data to copy to the buffer</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Buffer"/> with <paramref name="data"/> copied to it</returns>
        public Buffer AllocateUploadBuffer<T>(
            ReadOnlySpan<T> data,
            AllocFlags allocFlags = AllocFlags.None
        ) where T : unmanaged
        {
            var buff = AllocateBuffer(data.ByteLength(), MemoryAccess.CpuUpload, allocFlags: allocFlags);

            data.CopyTo(buff.AsSpan<T>());

            return buff;
        }

        /// <summary>
        /// Allocates a <see cref="MemoryAccess.CpuUpload"/> buffer and copy initial data to it
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="data"/></typeparam>
        /// <param name="data">The data to copy to the buffer</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Buffer"/> with <paramref name="data"/> copied to it</returns>
        public Buffer AllocateUploadBuffer<T>(
            ref T data,
            AllocFlags allocFlags = AllocFlags.None
        ) where T : unmanaged
        {
            var buff = AllocateBuffer(sizeof(T), MemoryAccess.CpuUpload, allocFlags: allocFlags);

            buff.AsRef<T>() = data;

            return buff;
        }

        /// <inheritdoc cref="AllocateBuffer(ulong, MemoryAccess, ResourceFlags, AllocFlags)"/>
        public Buffer AllocateBuffer(
            long length,
            MemoryAccess memoryKind,
            ResourceFlags resourceFlags = ResourceFlags.None,
            AllocFlags allocFlags = AllocFlags.None
        )
            => AllocateBuffer((ulong)length, memoryKind, resourceFlags, allocFlags);

        /// <summary>
        /// Allocates a buffer
        /// </summary>
        /// <param name="length">The length, in bytes, to allocate</param>
        /// <param name="memoryKind">The <see cref="MemoryAccess"/> to allocate the buffer in</param>
        /// <param name="resourceFlags">Any additional resource flags</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Buffer"/></returns>
        public Buffer AllocateBuffer(
            ulong length,
            MemoryAccess memoryKind,
            ResourceFlags resourceFlags = ResourceFlags.None,
            AllocFlags allocFlags = AllocFlags.None
        )
            => AllocateBuffer(new BufferDesc { Length = length, ResourceFlags = resourceFlags }, memoryKind, allocFlags);

        /// <summary>
        /// Allocates a buffer
        /// </summary>
        /// <param name="desc">The <see cref="BufferDesc"/> describing the buffer</param>
        /// <param name="memoryKind">The <see cref="MemoryAccess"/> to allocate the buffer in</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Buffer"/></returns>
        public Buffer AllocateBuffer(
            in BufferDesc desc,
            MemoryAccess memoryKind,
            AllocFlags allocFlags = AllocFlags.None
        )
        {
            InternalAllocDesc allocDesc = default;
            CreateAllocDesc(desc, &allocDesc, memoryKind, ResourceState.Common, allocFlags);

            if (TryGetFreeBlock(&allocDesc, out var heap, out var index, out var offset))
            {
                var info = new AllocationInfo
                {
                    HeapIndex = index,
                    HeapInfo = heap.Info,
                    Length = allocDesc.Size,
                    Alignment = allocDesc.Alignment,
                    Offset = offset
                };

                var buffer = _device.AllocateBuffer(desc, heap, offset, new (this, &_Return));
                SetAllocationInfo(buffer.Handle, info);
                return buffer;
            }
            else
            {
                return _device.AllocateBuffer(desc, memoryKind);
            }
        }

        /// <summary>
        /// Allocates a texture
        /// </summary>
        /// <param name="alias">The buffer to alias</param>
        /// <param name="desc">The <see cref="TextureDesc"/> describing the texture</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Texture"/></returns>
        public Buffer AllocateBufferAliasing(
            in Buffer alias,
            in BufferDesc desc,
            AllocFlags allocFlags = AllocFlags.None
        )
        {
            var info = GetAllocationInfo(alias.Handle);
            if (info.HasImplicitHeap)
            {
                ThrowHelper.ThrowArgumentException("Cannot alias with a committed resource");
            }

            var heap = GetHeapPool(info.HeapInfo)[(int)info.HeapIndex].Heap;
            
            return _device.AllocateBuffer(desc, heap, info.Offset);
        }


        /// <summary>
        /// Allocates a texture
        /// </summary>
        /// <param name="alias">The texture to alias</param>
        /// <param name="desc">The <see cref="TextureDesc"/> describing the texture</param>
        /// <param name="initialResourceState">The state of the resource when it is allocated</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Texture"/></returns>
        public Buffer AllocateBufferAliasing(
            in Texture alias,
            in BufferDesc desc,
            ResourceState initialResourceState,
            AllocFlags allocFlags = AllocFlags.None
        )
        {
            var info = GetAllocationInfo(alias.Handle);
            if (info.HasImplicitHeap)
            {
                ThrowHelper.ThrowArgumentException("Cannot alias with a committed resource");
            }

            var heap = GetHeapPool(info.HeapInfo)[(int)info.HeapIndex].Heap;

            return _device.AllocateBuffer(desc, heap, info.Offset);
        }



        private protected void SetAllocationInfo(in ResourceHandle res, in AllocationInfo info)
        {
            _allocationInfo.GetOrAddValueRef(res) = info;
        }

        private protected AllocationInfo GetAllocationInfo(in ResourceHandle res)
        {
            if (!_allocationInfo.TryGetValue(res, out var alloc))
            {
                ThrowHelper.ThrowInvalidOperationException();
            }

            return alloc;
        }

        private protected AllocationInfo RemoveAllocationInfo(in ResourceHandle res)
        {
            if (!_allocationInfo.TryGetValue(res, out var alloc))
            {
                ThrowHelper.ThrowInvalidOperationException();
            }

            _allocationInfo.Remove(res);
            return alloc;
        }

        // acceleration structure
        private protected void CreateAllocDesc(
            in ulong length,
            InternalAllocDesc* pDesc,
            AllocFlags allocFlags
        )
            => CreateAllocDesc(new BufferDesc { Length = length, ResourceFlags = ResourceFlags.AllowUnorderedAccess }, pDesc, MemoryAccess.GpuOnly, ResourceState.RaytracingAccelerationStructure, allocFlags);

            private protected void CreateAllocDesc(
            in BufferDesc desc,
            InternalAllocDesc* pDesc,
            MemoryAccess memoryKind,
            ResourceState initialResourceState,
            AllocFlags allocFlags
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

            if (initialResourceState == ResourceState.RaytracingAccelerationStructure && !_hasRaytracingSupport)
            {
                _device.ThrowGraphicsException("ResourceState.RaytracingAccelerationStructure is an invalid starting state when raytracing isn't supported");
            }

            var heapInfo = new HeapInfo
            {
                Alignment = Alignment._64KB,
                Access = memoryKind,
                Type = ResourceType.Buffer
            };

            *pDesc = new InternalAllocDesc
            {
                Desc = new(desc),
                HeapInfo = heapInfo,
                AllocFlags = allocFlags,
                InitialState = initialResourceState,
                IsBufferSuballocationCandidate = false // TODO
            };
        }

        internal static bool IsRenderTargetOrDepthStencil(ResourceFlags flags)
            => (flags & (ResourceFlags.AllowRenderTarget | ResourceFlags.AllowDepthStencil)) != 0;

        private bool ShouldCommitResource(InternalAllocDesc* desc)
        {
            if (desc->AllocFlags.HasFlag(AllocFlags.ForceAllocateNotComitted))
            {
            }

            bool mustCommit =
                ForceAllAllocationsCommitted
                || desc->AllocFlags.HasFlag(AllocFlags.ForceAllocateComitted);

            // Many resident resources on Win7 can cause ExecuteCommandLists to be slower (as OS is in charge of managing residency)
            // Placed resources only require checking heap for residency, so don't suffer as much as committed resources do
            // Render targets and depth stencils can see improved perf when committed on NVidia
            bool advantageousToCommit =
                desc->Desc.Type == ResourceType.RenderTargetOrDepthStencilTexture
                && !OperatingSystem.IsOSPlatform("windows7")
                && _device.Adapter.IsNVidia
            ;

            return mustCommit || advantageousToCommit;
        }

        private void VerifyDesc(InternalAllocDesc* desc)
        {
            const AllocFlags commitOptions = AllocFlags.ForceAllocateComitted | AllocFlags.ForceAllocateNotComitted;

            if ((desc->AllocFlags & commitOptions) == commitOptions)
            {
                ThrowHelper.ThrowArgumentException("Invalid to request 'ForceAllocateComitted' and 'ForceAllocateNotComitted'");
            }
        }

        private protected ref List<AllocatorHeap> GetHeapPool(in HeapInfo info)
        {
            if (info.Type == ResourceType.Buffer)
            {
                switch (info.Access)
                {
                    case var _ when _isCacheCoherentUma:
                    case MemoryAccess.GpuOnly:
                        return ref _buffer;
                    case MemoryAccess.CpuUpload:
                        return ref _upload!;
                    case MemoryAccess.CpuReadback:
                        return ref _readback!;
                }
            }


            if (_hasMergedHeapSupport || info.Type == ResourceType.Texture)
            {
                switch (info.Alignment)
                {
                    case Alignment._4KB:
                        return ref _4kbTextures;
                    case Alignment._64KB:
                        return ref _64kbTextures;
                    case Alignment._4MB:
                        return ref _4mbTextures;
                }
            }


            switch (info.Alignment)
            {
                case Alignment._64KB:
                    return ref _64kbRtOrDs;
                case Alignment._4MB:
                    return ref _4mbRtOrDs;
            }

            return ref Unsafe.NullRef<List<AllocatorHeap>>();
        }

        private const ulong DefaultHeapAlignment = D3D12_DEFAULT_MSAA_RESOURCE_PLACEMENT_ALIGNMENT; // 4mb for MSAA textures

        private protected ref AllocatorHeap CreateNewHeap(in HeapInfo info, out int index)
        {
            var heap = _device.CreateHeap(GetNewHeapSize(info), info);

            var allocatorHeap = new AllocatorHeap { Heap = heap, FreeBlocks = new() };
            AddFreeBlock(ref allocatorHeap, new HeapBlock { Offset = 0, Size = heap.Length });

            ref var pool = ref GetHeapPool(info);
            pool.Add(allocatorHeap);

            index = pool.Count - 1;

            return ref Common.ListExtensions.GetRef(pool, pool.Count - 1);
        }

        private ulong GetNewHeapSize(in HeapInfo info)
        {
            const ulong megabyte = 1024 * 1024;
            //const ulong kilobyte = 1024;

            if (info.Access is MemoryAccess.CpuUpload or MemoryAccess.CpuUpload)
            {
                return 32 * megabyte;
            }

            var guess = info.Type switch
            {
                ResourceType.Texture => 256 * megabyte,
                ResourceType.RenderTargetOrDepthStencilTexture => 128 * megabyte,
                ResourceType.Buffer => 64 * megabyte,
                _ => ulong.MaxValue, // shouldn't be reached
            };

            return guess;
        }

        private const int CommittedResourceHeapIndex = -1;


        internal static void _Return(object o, ref BufferHandle handle)
        {
            Debug.Assert(o is ComputeAllocator);
            Unsafe.As<ComputeAllocator>(o).Return(handle);
        }
        internal static void _Return(object o, ref TextureHandle handle)
        {
            Debug.Assert(o is ComputeAllocator);
            Unsafe.As<ComputeAllocator>(o).Return(handle);
        }
        internal static void _Return(object o, ref RaytracingAccelerationStructureHandle handle)
        {
            Debug.Assert(o is ComputeAllocator);
            Unsafe.As<ComputeAllocator>(o).Return(handle);
        }

        internal void Return(in ResourceHandle buff)
        {
            var info = RemoveAllocationInfo(buff);
            Debug.Assert(!info.HasImplicitHeap);

            var heap = GetHeapPool(info.HeapInfo)[(int)info.HeapIndex];
            heap.FreeBlocks.Add(new HeapBlock { Offset = info.Offset, Size = info.Length });
            // TODO defrag
        }

        private void MarkFreeBlockUsed(ref AllocatorHeap heap, HeapBlock wholeBlock, HeapBlock usedBlock)
        {
            ValidateFreeBlock(ref heap, usedBlock);

            var removed = heap.FreeBlocks.Remove(wholeBlock);
            SysDebug.Assert(removed);
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
            var check = heap.Heap.Length >= block.Offset + block.Size;
            SysDebug.Assert(check, "Invalid free block added");
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

        private protected bool TryGetFreeBlock(InternalAllocDesc* pDesc, out Heap heap, out uint heapIndex, out ulong offset)
        {
            ref var heaps = ref GetHeapPool(pDesc->HeapInfo);

            for (var i = 0; i < heaps.Count; i++)
            {
                ulong alignment = pDesc->Alignment;
                ulong size = pDesc->Size;
                heapIndex = (uint)i;

                var allocHeap = heaps[i];

                // try and get a block that is already aligned
                for (var j = allocHeap.FreeBlocks.Count - 1; j >= 0; j--)
                {
                    var block = allocHeap.FreeBlocks[j];

                    if (block.Size >= size
                        // because we assume the heap start is aligned, just check if the offset is too
                        && MathHelpers.IsAligned(block.Offset, alignment))
                    {
                        var freeBlock = new HeapBlock { Offset = block.Offset, Size = size };
                        heap = allocHeap.Heap;
                        MarkFreeBlockUsed(ref allocHeap, block, freeBlock);
                        offset = block.Offset;
                        return true;
                    }
                }

                // if that doesn't work, try and find a block big enough and manually align
                foreach (var block in allocHeap.FreeBlocks)
                {
                    var alignedOffset = MathHelpers.AlignUp(block.Offset, alignment);
                    var alignmentPadding = alignedOffset - block.Offset;

                    var alignedSize = block.Size - alignmentPadding;

                    // we need to ensure we don't wrap around if alignmentPadding is greater than the block size, so we check that first

                    if (alignmentPadding > block.Size && alignedSize >= size)
                    {
                        heap = allocHeap.Heap;
                        var freeBlock = new HeapBlock { Offset = alignedOffset, Size = alignedSize };
                        MarkFreeBlockUsed(ref allocHeap, block, freeBlock);

                        offset = freeBlock.Offset;
                        return true;
                    }
                }

            }

            heap = default;
            offset = default;
            heapIndex = 0;
            return false;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
