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

using Voltium.Extensions;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using Voltium.Annotations;
using System.Threading;
using Microsoft.Toolkit.HighPerformance.Extensions;

using SysDebug = System.Diagnostics.Debug;
using System.Diagnostics.CodeAnalysis;

namespace Voltium.Core.Memory
{
    // This type is "semi lowered". It needs high level alloc flags because they don't necessarily have a D3D12 equivalent
    // So we just lower most of it
    internal struct InternalAllocDesc
    {
        public D3D12_RESOURCE_DESC Desc;
        public D3D12_CLEAR_VALUE ClearValue;
        public D3D12_RESOURCE_STATES InitialState;
        public D3D12_HEAP_TYPE HeapType;
        public D3D12_HEAP_PROPERTIES HeapProperties;
        public ulong Size;

        // Can't immediately lower this. Contains other flags
        public AllocFlags AllocFlags;
        // The alignment/type/access required
        public HeapInfo HeapInfo;
        public bool IsBufferSuballocationCandidate;
    }

    /// <summary>
    /// An allocator used for allocating temporary and long
    /// </summary>
    public unsafe class ComputeAllocator : IDisposable
    {
        private protected ComputeDevice _device;

        private static readonly LogHelper.Context LogContext = LogHelper.Context.Create();

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

        [MemberNotNullWhen(false, nameof(_64kbRtOrDs), nameof(_4mbRtOrDs))]
        private protected bool _hasMergedHeapSupport { get; set; }

        [MemberNotNullWhen(true, nameof(_accelerationStructureHeap))]
        private protected bool _hasRaytracingSupport { get; set; }

        private const ulong HighestRequiredAlign = 1024 * 1024 * 4; // 4mb


        [MemberNotNullWhen(false, nameof(_readback), nameof(_upload))]
        private protected bool _isCacheCoherentUma { get; set; }

        // useful for debugging
        private protected static bool ForceAllAllocationsCommitted => false;
        private protected static bool ForceNoBufferSuballocations => false;

        private const D3D12_RESOURCE_FLAGS RenderTargetOrDepthStencilFlags =
            D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL | D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET;

        /// <summary>
        /// Whether <see cref="MemoryAccess.GpuOnly"/> resources are actually CPU accessible.
        /// This occurs as an optimisation to enable zero-copy resource transfer on Cache Coherent UMA architectures
        /// </summary>
        public bool GpuOnlyResourcesAreWritable => _isCacheCoherentUma;

        public ComputeAllocator(ComputeDevice device)
        {
            _device = device;

            _isCacheCoherentUma = device.Architecture.IsCacheCoherentUma;

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

            var buffer = new Buffer(_device, Allocate(&allocDesc), 0, allocDesc);

            return buffer;
        }

        /// <inheritdoc cref="AllocateBuffer(long, MemoryAccess, ResourceFlags, AllocFlags)"/>
        public Buffer AllocateBuffer(
            ulong length,
            MemoryAccess memoryKind,
            ResourceFlags resourceFlags = ResourceFlags.None,
            AllocFlags allocFlags = AllocFlags.None
        )
            => AllocateBuffer((long)length, memoryKind, resourceFlags, allocFlags);

        /// <summary>
        /// Allocates a buffer
        /// </summary>
        /// <param name="length">The length, in bytes, to allocate</param>
        /// <param name="memoryKind">The <see cref="MemoryAccess"/> to allocate the buffer in</param>
        /// <param name="resourceFlags">Any additional resource flags</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Buffer"/></returns>
        public Buffer AllocateBuffer(
            long length,
            MemoryAccess memoryKind,
            ResourceFlags resourceFlags = ResourceFlags.None,
            AllocFlags allocFlags = AllocFlags.None
        )
            => AllocateBuffer(new BufferDesc { Length = length, ResourceFlags = resourceFlags }, memoryKind, allocFlags);


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
            InternalAllocDesc allocDesc = default;
            CreateAllocDesc(desc, &allocDesc, alias.Resource.Desc.HeapInfo.Access, ResourceState.Common, allocFlags);

            return new Buffer(_device, AllocateAliasing(alias.Resource, &allocDesc), 0, allocDesc);
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
            InternalAllocDesc allocDesc;
            CreateAllocDesc(desc, &allocDesc, alias.Resource.Desc.HeapInfo.Access, initialResourceState, allocFlags);

            return new Buffer(_device, AllocateAliasing(alias.Resource, &allocDesc), 0, allocDesc);
        }

        private const int BufferAlignment = /* 64kb */ 64 * 1024;

        private protected void CreateAllocDesc(in BufferDesc desc, InternalAllocDesc* pDesc, MemoryAccess memoryKind, ResourceState initialResourceState, AllocFlags allocFlags)
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

            var resDesc = new D3D12_RESOURCE_DESC
            {
                Width = (ulong)desc.Length,
                Height = 1,
                DepthOrArraySize = 1,
                Dimension = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER,
                Flags = (D3D12_RESOURCE_FLAGS)desc.ResourceFlags,
                MipLevels = 1,

                // required values for a buffer
                SampleDesc = new DXGI_SAMPLE_DESC(1, 0),
                Layout = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_ROW_MAJOR
            };

            var heapInfo = new HeapInfo
            {
                Alignment = Alignment._64KB,
                Access = memoryKind,
                Type = ResourceType.Buffer
            };

            *pDesc = new InternalAllocDesc
            {
                Desc = resDesc,
                HeapInfo = heapInfo,
                AllocFlags = allocFlags,
                HeapType = (D3D12_HEAP_TYPE)memoryKind,
                HeapProperties = GetHeapProperties(heapInfo),
                InitialState = (D3D12_RESOURCE_STATES)initialResourceState,
                IsBufferSuballocationCandidate = !ForceNoBufferSuballocations && !desc.ResourceFlags.IsShaderWritable()
            };
        }



        /// <summary>
        /// Allocates a new region of GPU memory
        /// </summary>
        /// <param name="alias">The resource to alias</param>
        /// <param name="desc">The description for the resource to allocate</param>
        /// <returns>A new <see cref="GpuResource"/> which encapsulates the allocated region</returns>
        internal GpuResource AllocateAliasing(
            GpuResource alias,
            InternalAllocDesc* desc // we use a pointer here because eventually we have to pass D3D12_RESOURCE_DESC*, so avoid unnecessary pinning
        )
        {
            if (desc->AllocFlags.HasFlag(AllocFlags.ForceAllocateComitted))
            {
                ThrowHelper.ThrowArgumentException(nameof(InternalAllocDesc.AllocFlags), "Can't commit an aliased resource");
            }
            if (alias.HeapIndex == CommittedResourceHeapIndex)
            {
                ThrowHelper.ThrowArgumentException(nameof(alias), "Can't alias a committed resource");
            }

            var info = _device.GetAllocationInfo(desc);

            // fast path
            if (info.SizeInBytes <= alias.Block.Size)
            {
                return CreatePlaced(desc, ref GetHeap(alias).AsSpan()[alias.HeapIndex], alias.HeapIndex, alias.Block);
            }
            else
            {
                ThrowHelper.ThrowNotImplementedException("Multi-block aliased resources - TODO");
                return null!;
            }
        }

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

            var res = AllocatePlacedFromHeap(desc);

            return res;
        }

        internal static bool IsRenderTargetOrDepthStencil(D3D12_RESOURCE_FLAGS flags)
            => (flags & RenderTargetOrDepthStencilFlags) != 0;

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
                IsRenderTargetOrDepthStencil(desc->Desc.Flags)
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

        private ref List<AllocatorHeap> GetHeap(GpuResource allocation)
        {
            ref readonly InternalAllocDesc desc = ref allocation.Desc;
            return ref GetHeapPool(desc.HeapInfo);
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

        private static readonly D3D12_HEAP_PROPERTIES UmaHeap = new()
        {
            Type = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_CUSTOM,
            MemoryPoolPreference = D3D12_MEMORY_POOL.D3D12_MEMORY_POOL_L0,
            CPUPageProperty = D3D12_CPU_PAGE_PROPERTY.D3D12_CPU_PAGE_PROPERTY_WRITE_BACK, // as they are shared caches, cache coherent UMA
            CreationNodeMask = 0,
            VisibleNodeMask = 0
        };

        private protected D3D12_HEAP_PROPERTIES GetHeapProperties(in HeapInfo info)
        {
            return info.Access is MemoryAccess.GpuOnly && _isCacheCoherentUma ? UmaHeap : new D3D12_HEAP_PROPERTIES((D3D12_HEAP_TYPE)info.Access);
        }

        private const ulong DefaultHeapAlignment = D3D12_DEFAULT_MSAA_RESOURCE_PLACEMENT_ALIGNMENT; // 4mb for MSAA textures

        private protected ref AllocatorHeap CreateNewHeap(in HeapInfo info, out int index)
        {
            var flags = D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE;

            if (!_hasMergedHeapSupport && this is GraphicsAllocator)
            {
                flags |= info.Type switch
                {
                    ResourceType.Texture => D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_ALLOW_ONLY_NON_RT_DS_TEXTURES,
                    ResourceType.RenderTargetOrDepthStencilTexture => D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_ALLOW_ONLY_RT_DS_TEXTURES,
                    ResourceType.Buffer => D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS,
                    _ => 0
                };
            }

            flags |= D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_CREATE_NOT_ZEROED;

            var desc = new D3D12_HEAP_DESC
            {
                SizeInBytes = GetNewHeapSize(info),
                Properties = GetHeapProperties(info),
                Alignment = (ulong)info.Alignment,
                Flags = flags
            };

            var heap = _device.CreateHeap(&desc);

            var allocatorHeap = new AllocatorHeap { Heap = heap.Move(), FreeBlocks = new() };
            AddFreeBlock(ref allocatorHeap, new HeapBlock { Offset = 0, Size = desc.SizeInBytes });

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

        private UniqueComPtr<ID3D12Heap> OpenHeapFromAddress(void* address)
        {
            using UniqueComPtr<ID3D12Heap> heap = default;
            _device.ThrowIfFailed(_device.DevicePointer->OpenExistingHeapFromAddress(address, heap.Iid, (void**)&heap));

            return heap.Move();
        }

        private UniqueComPtr<ID3D12Heap> OpenHeapFromFile(FileStream file)
            => OpenHeapFromFile(file.SafeFileHandle.DangerousGetHandle());

        private UniqueComPtr<ID3D12Heap> OpenHeapFromFile(IntPtr file)
        {
            using UniqueComPtr<ID3D12Heap> heap = default;
            _device.ThrowIfFailed(_device.DevicePointer->OpenExistingHeapFromFileMapping(file, heap.Iid, (void**)&heap));

            return heap.Move();
        }

        private const int CommittedResourceHeapIndex = -1;
        internal void Return(GpuResource alloc)
        {
            if (alloc.HeapIndex != CommittedResourceHeapIndex)
            {
                ref var heap = ref GetHeap(alloc).AsSpan()[alloc.HeapIndex];

                Release();

                ReturnPlacedAllocation(alloc, ref heap);
            }
            else
            {
                Release();
            }

            void Release()
            {
                if (ShouldEvict(alloc))
                {
                    _device.Evict(alloc);
                }
                else
                {
                    var refCount = alloc.GetResourcePointer()->Release();
                }
            }
        }

        private bool ShouldEvict(GpuResource alloc)
        {
            ref readonly var desc = ref alloc.Desc;
            return alloc.HeapIndex == CommittedResourceHeapIndex && (desc.AllocFlags.HasFlag(AllocFlags.FastRelease) || _device.Architecture.VirtualAddressSpaceSize > /* TODO: budget */ _device.Adapter.DedicatedVideoMemory);
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
                _device,
                resource.Move(),
                desc,
                null,
                CommittedResourceHeapIndex
            );
        }

        private bool TryAllocateFromHeap(InternalAllocDesc* desc, ref AllocatorHeap heap, int heapIndex, out GpuResource allocation)
        {
            if (TryGetFreeBlock(ref heap, desc, out HeapBlock freeBlock))
            {
                allocation = CreatePlaced(desc, ref heap, heapIndex, freeBlock);
                return true;
            }

            allocation = default!;
            return false;
        }

        private GpuResource AllocatePlacedFromHeap(InternalAllocDesc* desc)
        {
            GpuResource allocation;
            ref var heapList = ref GetHeapPool(desc->HeapInfo);
            for (var i = 0; i < heapList.Count; i++)
            {
                if (TryAllocateFromHeap(desc, ref Common.ListExtensions.GetRef(heapList, i), i, out allocation))
                {
                    return allocation;
                }
            }

            // No free blocks available anywhere. Create a new heap
            ref var newHeap = ref CreateNewHeap(desc->HeapInfo, out int index);
            var result = TryAllocateFromHeap(desc, ref newHeap, index, out allocation);
            if (!result) // too big to fit in heap, realloc as comitted
            {
                if (desc->AllocFlags.HasFlag(AllocFlags.ForceAllocateNotComitted))
                {
                    ThrowHelper.ThrowInsufficientMemoryException(
                        $"Could not satisfy allocation - required {desc->Size} bytes, but this " +
                        "is larget than the maximum heap size, and AllocFlags.ForceAllocateNonComitted prevented it being allocated committed"
                    );
                }
                return AllocateCommitted(desc);
            }
            return allocation;
        }

        private protected ResourceType GetResourceType(D3D12_RESOURCE_DIMENSION dimension, D3D12_RESOURCE_FLAGS flags)
        {
            if (dimension == D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER)
            {
                return ResourceType.Buffer;
            }
            else if (IsRenderTargetOrDepthStencil(flags))
            {
                return ResourceType.RenderTargetOrDepthStencilTexture;
            }

            return ResourceType.Texture;
        }

        private GpuResource CreatePlaced(InternalAllocDesc* desc, ref AllocatorHeap heap, int heapIndex, HeapBlock block)
        {
            var resource = _device.CreatePlacedResource(heap.Heap.Ptr, block.Offset, desc);

            return new GpuResource(
                _device,
                resource.Move(),
                desc,
                this,
                heapIndex,
                block
            );
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
            var check = heap.Heap.Ptr->GetDesc().SizeInBytes >= block.Offset + block.Size;
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

        private bool TryGetFreeBlock(ref AllocatorHeap heap, InternalAllocDesc* pDesc, out HeapBlock freeBlock)
        {
            SysDebug.Assert(pDesc->Desc.Alignment <= HighestRequiredAlign);

            ulong alignment = pDesc->Desc.Alignment;
            ulong size = pDesc->Size;

            // try and get a block that is already aligned
            for (var i = heap.FreeBlocks.Count - 1; i >= 0; i--)
            {
                var block = heap.FreeBlocks[i];

                if (block.Size >= size
                    // because we assume the heap start is aligned, just check if the offset is too
                    && MathHelpers.IsAligned(block.Offset, alignment))
                {
                    freeBlock = new HeapBlock { Offset = block.Offset, Size = size };
                    MarkFreeBlockUsed(ref heap, block, freeBlock);
                    return true;
                }
            }

            // if that doesn't work, try and find a block big enough and manually align
            foreach (var block in heap.FreeBlocks)
            {
                var alignedOffset = MathHelpers.AlignUp(block.Offset, alignment);
                var alignmentPadding = alignedOffset - block.Offset;

                var alignedSize = block.Size - alignmentPadding;

                // we need to ensure we don't wrap around if alignmentPadding is greater than the block size, so we check that first

                if (alignmentPadding > block.Size && alignedSize >= size)
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
        }
    }
}
