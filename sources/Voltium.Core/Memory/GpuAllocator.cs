using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using static TerraFX.Interop.Windows;
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

        // Can't immediately lower this. It contains other flags
        public AllocFlags AllocFlags;
        public bool IsBufferSuballocationCandidate;
    }

    /// <summary>
    /// An allocator used for allocating temporary and long
    /// </summary>
    public unsafe sealed class GpuAllocator : IDisposable
    {
        private static readonly LogHelper.Context LogContext = LogHelper.Context.Create();

        private List<AllocatorHeap> _readback = new();
        private List<AllocatorHeap> _upload = new();

        // single merged heap
        private List<AllocatorHeap> _default = null!;

        // 3 seperate heaps when merged heap isn't supported
        private List<AllocatorHeap> _buffer = null!;

        // Used when the buffer is a render target, uav, or stream out target
        // Because resource barriers can't be used on regions of a suballocated buffer 
        private List<AllocatorHeap> _noSubAllocationBuffer = null!;


        private List<AllocatorHeap> _texture = null!;
        private List<AllocatorHeap> _rtOrDs = null!;

        private List<AllocatorHeap> _accelerationStructureHeap = null!;

        private ComputeDevice _device;
        private const ulong HighestRequiredAlign = 1024 * 1024 * 4; // 4mb

        private bool _hasMergedHeapSupport;
        private bool _isCacheCoherentUma;

        // useful for debugging
        private static bool ForceAllAllocationsCommitted => false;
        private static bool ForceNoBufferSuballocations => false;

        internal static void CreateDesc(in TextureDesc desc, out D3D12_RESOURCE_DESC resDesc)
        {
            DXGI_SAMPLE_DESC sample = new DXGI_SAMPLE_DESC(desc.Msaa.SampleCount, desc.Msaa.QualityLevel);

            // Normalize default
            if (desc.Msaa.SampleCount == 0)
            {
                sample.Count = 1;
            }

            resDesc = new D3D12_RESOURCE_DESC
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
        }


        //private UniqueComPtr<ID3D12Heap> GetHeapForArray<T>(T[] array, MemoryAccess access) where T : unmanaged
        //{
        //    // god forgive my damn'ed soul
        //    var manager = _COMArrayDisposal.Create(array);

        //    var heap = OpenHeapFromAddress(manager.Pointer);

        //    Guard.ThrowIfFailed(heap.Ptr->SetPrivateDataInterface(_COMArrayDisposal.RID, (IUnknown*)&manager));

        //    return heap;
        //}

        //[NativeComType(implements: typeof(IUnknown))]
        //private struct _COMArrayDisposal
        //{
        //    private readonly void** lpVtbl;

        //    public static readonly Guid* RID = InitGuid();

        //    private static readonly Guid Guid = // {B7D81026-6DBF-4A61-BEA5-F08D0AAF371F}
        //            new(0xb7d81026, 0x6dbf, 0x4a61, 0xbe, 0xa5, 0xf0, 0x8d, 0xa, 0xaf, 0x37, 0x1f);
        //    private static Guid* InitGuid()
        //    {
        //        var p = (Guid*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(_COMArrayDisposal), sizeof(Guid));

        //        *p = Guid;

        //        return p;
        //    }

        //    private uint _refCount;
        //    private IntPtr _handle;

        //    public void* Pointer => (void*)GCHandle.FromIntPtr(_handle).AddrOfPinnedObject();

        //    private _COMArrayDisposal(GCHandle handle)
        //    {
        //        _lpVtbl = Init();
        //        _refCount = 1;
        //        _handle = GCHandle.ToIntPtr(handle);
        //    }

        //    public static _COMArrayDisposal Create<T>(T[] arr)
        //    {
        //        var handle = GCHandle.Alloc(arr, GCHandleType.Pinned);

        //        return Create(handle);
        //    }

        //    public static _COMArrayDisposal Create(GCHandle handle)
        //    {
        //        return new(handle);
        //    }

        //    [NativeComMethod]
        //    public int QueryInterface(Guid* riid, void** ppvObject)
        //    {
        //        *ppvObject = null;
        //        return E_NOINTERFACE;
        //    }

        //    [NativeComMethod]
        //    public uint AddRef()
        //    {
        //        return Interlocked.Increment(ref _refCount);
        //    }

        //    [NativeComMethod]
        //    public uint Release()
        //    {
        //        var val = Interlocked.Decrement(ref _refCount);

        //        if (val < 0)
        //        {
        //            var gc = GCHandle.FromIntPtr(_handle);

        //            gc.Free();
        //        }

        //        return val;
        //    }
        //}


        private const D3D12_RESOURCE_FLAGS RenderTargetOrDepthStencilFlags =
            D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL | D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET;

        /// <summary>
        /// Whether <see cref="MemoryAccess.GpuOnly"/> resources are actually CPU accessible.
        /// This occurs as an optimisation to enable zero-copy resource transfer on Cache Coherent UMA architectures
        /// </summary>
        public bool GpuOnlyResourcesAreWritable => _isCacheCoherentUma;

        /// <summary>
        /// Creates a new allocator
        /// </summary>
        /// <param name="device">The <see cref="ID3D12Device"/> to allocate on</param>
        internal GpuAllocator(ComputeDevice device)
        {
            Debug.Assert(device is not null);
            _device = device;

            _device.QueryFeatureSupport(D3D12_FEATURE.D3D12_FEATURE_D3D12_OPTIONS, out D3D12_FEATURE_DATA_D3D12_OPTIONS options);
            _hasMergedHeapSupport = options.ResourceHeapTier == D3D12_RESOURCE_HEAP_TIER.D3D12_RESOURCE_HEAP_TIER_2;
            _isCacheCoherentUma = device.Architecture.IsCacheCoherentUma;

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

            _accelerationStructureHeap = new();
        }

        /// <summary>
        /// Allocates a buffer for use as a raytracing acceleration structure
        /// </summary>
        /// <param name="info">The <see cref="AccelerationStructureBuildInfo"/> containing the required sizes of the buffers</param>
        /// <param name="scratch">On return, this is filled with a <see cref="Buffer"/> with a large anough size to be used as the scratch buffer in a raytracing acceleration structure build</param>
        /// <returns>A <see cref="Buffer"/> with a large anough size to be used as the destination in a raytracing acceleration structure build</returns>
        public Buffer AllocateRaytracingAccelerationBuffer(AccelerationStructureBuildInfo info, out Buffer scratch)
            => AllocateRaytracingAccelerationBuffer(info, AllocFlags.None, out scratch);

        /// <summary>
        /// Allocates a buffer for use as a raytracing acceleration structure
        /// </summary>
        /// <param name="info">The <see cref="AccelerationStructureBuildInfo"/> containing the required sizes of the buffers</param>
        /// <param name="scratch">On return, this is filled with a <see cref="Buffer"/> with a large anough size to be used as the scratch buffer in a raytracing acceleration structure build</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A <see cref="Buffer"/> with a large anough size to be used as the destination in a raytracing acceleration structure build</returns>
        public Buffer AllocateRaytracingAccelerationBuffer(AccelerationStructureBuildInfo info, AllocFlags allocFlags, out Buffer scratch)
        {
            scratch = AllocateBuffer(info.ScratchSize, MemoryAccess.GpuOnly, ResourceState.UnorderedAccess, ResourceFlags.AllowUnorderedAccess | ResourceFlags.DenyShaderResource, allocFlags);
            return AllocateRaytracingAccelerationBuffer(info.DestSize, allocFlags);
        }

        /// <summary>
        /// Allocates a buffer for use as a raytracing acceleration structure or scratch buffer for a raytracing acceleration structure build
        /// </summary>
        /// <param name="length">The length, in bytes, to allocate</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A <see cref="Buffer"/></returns>
        public Buffer AllocateRaytracingAccelerationBuffer(ulong length, AllocFlags allocFlags = AllocFlags.None)
        {
            return AllocateBuffer(length, MemoryAccess.GpuOnly, ResourceState.RayTracingAccelerationStructure, ResourceFlags.AllowUnorderedAccess, allocFlags);
        }

        /// <summary>
        /// Allocates a <see cref="MemoryAccess.CpuUpload"/> buffer and copy initial data to it
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
        /// Allocates a <see cref="MemoryAccess.CpuUpload"/> buffer and copy initial data to it
        /// </summary>
        /// <param name="count">The number of bytes to allocate</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Buffer"/></returns>
        public Buffer AllocateReadbackBuffer(
            int count = 1,
            AllocFlags allocFlags = AllocFlags.None
        )
        {
            var buff = AllocateBuffer(count, MemoryAccess.CpuReadback, allocFlags: allocFlags);
            return buff;
        }


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
        {
            var buff = AllocateUploadBuffer(Unsafe.SizeOf<T>() * count, allocFlags);
            return buff;
        }


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
        {
            var buff = AllocateBuffer(count, MemoryAccess.CpuUpload, allocFlags: allocFlags);
            return buff;
        }

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
        {
            var buff = AllocateBuffer(count, MemoryAccess.CpuUpload, allocFlags: allocFlags);
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
        /// <param name="initialResourceState">The initial state of the resource</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Buffer"/></returns>
        public Buffer AllocateBuffer(
            in BufferDesc desc,
            MemoryAccess memoryKind,
            ResourceState initialResourceState = ResourceState.Common,
            AllocFlags allocFlags = AllocFlags.None
        )
        {
            InternalAllocDesc allocDesc = default;
            CreateAllocDesc(desc, &allocDesc, memoryKind, initialResourceState, allocFlags);

            var buffer = new Buffer(_device, Allocate(&allocDesc), 0, &allocDesc);

            return buffer;
        }

        /// <inheritdoc cref="AllocateBuffer(long, MemoryAccess, ResourceState, ResourceFlags, AllocFlags)"/>
        public Buffer AllocateBuffer(
            ulong length,
            MemoryAccess memoryKind,
            ResourceState initialResourceState = ResourceState.Common,
            ResourceFlags resourceFlags = ResourceFlags.None,
            AllocFlags allocFlags = AllocFlags.None
        )
            => AllocateBuffer((long)length, memoryKind, initialResourceState, resourceFlags, allocFlags);

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
            => AllocateBuffer(new BufferDesc { Length = length, ResourceFlags = resourceFlags }, memoryKind, initialResourceState, allocFlags);


        /// <summary>
        /// Allocates a texture
        /// </summary>
        /// <param name="alias">The buffer to alias</param>
        /// <param name="desc">The <see cref="TextureDesc"/> describing the texture</param>
        /// <param name="initialResourceState">The state of the resource when it is allocated</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Texture"/></returns>
        public Buffer AllocateBufferAliasing(
            in Buffer alias,
            in BufferDesc desc,
            ResourceState initialResourceState,
            AllocFlags allocFlags = AllocFlags.None
        )
        {
            InternalAllocDesc allocDesc = default;
            CreateAllocDesc(desc, &allocDesc, alias.Resource.MemoryKind, initialResourceState, allocFlags);

            return new Buffer(_device, AllocateAliasing(alias.Resource, &allocDesc), 0, &allocDesc);
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
            InternalAllocDesc allocDesc = default;
            CreateAllocDesc(desc, &allocDesc, alias.Resource.MemoryKind, initialResourceState, allocFlags);

            return new Buffer(_device, AllocateAliasing(alias.Resource, &allocDesc), 0, &allocDesc);
        }

        /// <summary>
        /// Allocates a texture
        /// </summary>
        /// <param name="alias">The buffer to alias</param>
        /// <param name="desc">The <see cref="TextureDesc"/> describing the texture</param>
        /// <param name="initialResourceState">The state of the resource when it is allocated</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Texture"/></returns>
        public Texture AllocateTextureAliasing(
            in Buffer alias,
            in TextureDesc desc,
            ResourceState initialResourceState,
            AllocFlags allocFlags = AllocFlags.None
        )
        {
            InternalAllocDesc allocDesc = default;
            CreateAllocDesc(desc, &allocDesc, initialResourceState, allocFlags);

            return new Texture(desc, AllocateAliasing(alias.Resource, &allocDesc));
        }


        /// <summary>
        /// Allocates a texture
        /// </summary>
        /// <param name="alias">The texture to alias</param>
        /// <param name="desc">The <see cref="TextureDesc"/> describing the texture</param>
        /// <param name="initialResourceState">The state of the resource when it is allocated</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A new <see cref="Texture"/></returns>
        public Texture AllocateTextureAliasing(
            in Texture alias,
            in TextureDesc desc,
            ResourceState initialResourceState,
            AllocFlags allocFlags = AllocFlags.None
        )
        {
            InternalAllocDesc allocDesc = default;
            CreateAllocDesc(desc, &allocDesc, initialResourceState, allocFlags);

            return new Texture(desc, AllocateAliasing(alias.Resource, &allocDesc));
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
            InternalAllocDesc allocDesc = default;
            CreateAllocDesc(desc, &allocDesc, initialResourceState, allocFlags);

            var texture = new Texture(desc, Allocate(&allocDesc));

            if (GpuOnlyResourcesAreWritable)
            {
                var numSubresources = texture.GetResourcePointer()->GetDesc().GetSubresources((ID3D12Device*)_device.DevicePointer);
                for (var i = 0u; i < numSubresources; i++)
                {
                    _device.ThrowIfFailed(texture.GetResourcePointer()->Map(i, null, null));
                }
            }

            return texture;
        }

        private const int BufferAlignment = /* 64kb */ 64 * 1024;

        private void CreateAllocDesc(in BufferDesc desc, InternalAllocDesc* pDesc, MemoryAccess memoryKind, ResourceState initialResourceState, AllocFlags allocFlags)
        {
            if (memoryKind == MemoryAccess.CpuUpload)
            {
                initialResourceState = ResourceState.GenericRead;
            }
            else if (memoryKind == MemoryAccess.CpuReadback)
            {
                initialResourceState = ResourceState.CopyDestination;
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

            *pDesc = new InternalAllocDesc
            {
                Desc = resDesc,
                AllocFlags = allocFlags,
                HeapType = (D3D12_HEAP_TYPE)memoryKind,
                HeapProperties = GetHeapProperties((D3D12_HEAP_TYPE)memoryKind),
                InitialState = (D3D12_RESOURCE_STATES)initialResourceState,
                IsBufferSuballocationCandidate = !ForceNoBufferSuballocations && !desc.ResourceFlags.IsShaderWritable()
            };
        }
        private void CreateAllocDesc(in TextureDesc desc, InternalAllocDesc* pDesc, ResourceState initialResourceState, AllocFlags allocFlags)
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
                Height = Math.Max(1, desc.Height),
                DepthOrArraySize = Math.Max((ushort)1, desc.DepthOrArraySize),
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

            *pDesc = new InternalAllocDesc
            {
                Desc = resDesc,
                ClearValue = clearVal,
                InitialState = (D3D12_RESOURCE_STATES)initialResourceState,
                AllocFlags = allocFlags,
                HeapType = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_DEFAULT,
                HeapProperties = GetHeapProperties(D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_DEFAULT),
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

            var info = GetAllocInfo(desc);

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

            var info = GetAllocInfo(desc);

            if (ShouldCommitResource(desc))
            {
                return AllocateCommitted(desc);
            }

            var res = AllocatePlacedFromHeap(desc, info);

            return res;
        }

        private D3D12_RESOURCE_ALLOCATION_INFO GetAllocInfo(InternalAllocDesc* desc)
        {
            D3D12_RESOURCE_ALLOCATION_INFO info;

            // avoid native call as we don't need to for buffers
            if (desc->Desc.Dimension == D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER)
            {
                info = new D3D12_RESOURCE_ALLOCATION_INFO(desc->Desc.Width, Windows.D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT);
            }
            else
            {
                info = _device.GetAllocationInfo(desc);

                // Write the required alignment, which is still relevant even for committed resources
                // as it can allow us to get 4kb small alignment for textures when valid
                desc->Desc.Alignment = info.Alignment;
            }

            return info;
        }

        internal static bool IsRenderTargetOrDepthStencil(D3D12_RESOURCE_FLAGS flags)
            => (flags & RenderTargetOrDepthStencilFlags) != 0;

        private bool ShouldCommitResource(InternalAllocDesc* desc)
        {
            bool mustCommit = ForceAllAllocationsCommitted || desc->AllocFlags.HasFlag(AllocFlags.ForceAllocateComitted);

            // Many resident resources on Win7 can cause ExecuteCommandLists to be slower
            // Placed resources only require checking heap for residency, so don't suffer as much as committed resources do
            // Render targets and depth stencils can see improved perf when committed
            return mustCommit || (IsRenderTargetOrDepthStencil(desc->Desc.Flags) && !PlatformInfo.IsWindows7 && _device.Adapter.IsNVidia);
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
            return ref GetHeapPool(desc, GetResType(desc.Desc.Dimension, desc.Desc.Flags));
        }

        private ref List<AllocatorHeap> GetHeapPool(in InternalAllocDesc desc, GpuResourceType res)
        {
            if (desc.InitialState == D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_RAYTRACING_ACCELERATION_STRUCTURE)
            {
                return ref _accelerationStructureHeap;
            }

            var mem = desc.HeapType;
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

            return ref Unsafe.NullRef<List<AllocatorHeap>>();
        }

        private D3D12_HEAP_PROPERTIES GetHeapProperties(D3D12_HEAP_TYPE type)
        {
            D3D12_HEAP_PROPERTIES props;
            if (type is D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_DEFAULT && _isCacheCoherentUma)
            {
                // Make a CPU visible heap
                props = new()
                {
                    Type = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_CUSTOM,
                    MemoryPoolPreference = D3D12_MEMORY_POOL.D3D12_MEMORY_POOL_L0,
                    CPUPageProperty = D3D12_CPU_PAGE_PROPERTY.D3D12_CPU_PAGE_PROPERTY_WRITE_BACK, // as they are shared caches, cache coherent UMA
                    CreationNodeMask = 0,
                    VisibleNodeMask = 0
                };
            }
            else
            {
                props = new D3D12_HEAP_PROPERTIES(type);
            };

            return props;
        }

        private const ulong DefaultHeapAlignment = Windows.D3D12_DEFAULT_MSAA_RESOURCE_PLACEMENT_ALIGNMENT; // 4mb for MSAA textures
        private ref AllocatorHeap CreateNewHeap(InternalAllocDesc* allocDesc, GpuResourceType res, out int index)
        {
            var mem = allocDesc->HeapType;
            D3D12_HEAP_FLAGS flags = default;
            ulong alignment = DefaultHeapAlignment;

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

                alignment = res switch
                {
                    GpuResourceType.Meaningless => 0, // shouldn't be reached
                    GpuResourceType.Tex => Windows.D3D12_DEFAULT_MSAA_RESOURCE_PLACEMENT_ALIGNMENT,
                    GpuResourceType.RenderTargetOrDepthStencilTexture => Windows.D3D12_DEFAULT_MSAA_RESOURCE_PLACEMENT_ALIGNMENT,
                    GpuResourceType.Buffer => Windows.D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT,
                    _ => 0, // shouldn't be reached
                };
            }

            flags |= D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_CREATE_NOT_ZEROED;

            var desc = new D3D12_HEAP_DESC
            {
                SizeInBytes = GetNewHeapSize(mem, res),
                Properties = GetHeapProperties(mem),
                Alignment = alignment,
                Flags = flags
            };

            var heap = _device.CreateHeap(&desc);

            var allocatorHeap = new AllocatorHeap { Heap = heap.Move(), FreeBlocks = new() };
            AddFreeBlock(ref allocatorHeap, new HeapBlock { Offset = 0, Size = desc.SizeInBytes });

            ref var pool = ref GetHeapPool(*allocDesc, res);
            pool.Add(allocatorHeap);

            index = pool.Count - 1;

            return ref ListExtensions.GetRef(pool, pool.Count - 1);
        }

        private ulong GetNewHeapSize(D3D12_HEAP_TYPE mem, GpuResourceType res)
        {
            const ulong megabyte = 1024 * 1024;
            //const ulong kilobyte = 1024;

            if (mem is D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_UPLOAD or D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_READBACK)
            {
                return 32 * megabyte;
            }

            var guess = res switch
            {
                GpuResourceType.Meaningless => 256 * megabyte,
                GpuResourceType.Tex => 256 * megabyte,
                GpuResourceType.RenderTargetOrDepthStencilTexture => 128 * megabyte,
                GpuResourceType.Buffer => 64 * megabyte,
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
                    Debug.Assert(refCount == 0);
                    _ = refCount;
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
            ref var heapList = ref GetHeapPool(*desc, resType);
            for (var i = 0; i < heapList.Count; i++)
            {
                if (TryAllocateFromHeap(desc, info, ref ListExtensions.GetRef(heapList, i), i, out allocation))
                {
                    return allocation;
                }
            }

            // No free blocks available anywhere. Create a new heap
            ref var newHeap = ref CreateNewHeap(desc, resType, out int index);
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
            var check = heap.Heap.Ptr->GetDesc().SizeInBytes >= block.Offset + block.Size;
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
        }
    }
}
