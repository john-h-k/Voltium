using System;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Core.Devices;

using SysDebug = System.Diagnostics.Debug;

namespace Voltium.Core.Memory
{
    public unsafe sealed class GraphicsAllocator : ComputeAllocator
    {


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

        /// <summary>
        /// Creates a new allocator
        /// </summary>
        /// <param name="device">The <see cref="ID3D12Device"/> to allocate on</param>
        internal GraphicsAllocator(GraphicsDevice device) : base(device)
        {
            SysDebug.Assert(device is not null);

            _device.QueryFeatureSupport(D3D12_FEATURE.D3D12_FEATURE_D3D12_OPTIONS, out D3D12_FEATURE_DATA_D3D12_OPTIONS options);
            _hasMergedHeapSupport = options.ResourceHeapTier == D3D12_RESOURCE_HEAP_TIER.D3D12_RESOURCE_HEAP_TIER_2;

            _4kbTextures = new();
            _64kbTextures = new();
            _4mbTextures = new();
            if (!_hasMergedHeapSupport)
            {
                _64kbRtOrDs = new();
                _4mbRtOrDs = new();
            }

            _accelerationStructureHeap = new();

            _device.QueryFeatureSupport(D3D12_FEATURE.D3D12_FEATURE_D3D12_OPTIONS6, out D3D12_FEATURE_DATA_D3D12_OPTIONS6 options6);
            _tileSize = options6.ShadingRateImageTileSize;
        }

        private uint _tileSize;

        /// <summary>
        /// Allocates a buffer for use as a raytracing acceleration structure
        /// </summary>
        /// <param name="info">The <see cref="AccelerationStructureBuildInfo"/> containing the required sizes of the buffers</param>
        /// <param name="scratch">On return, this is filled with a <see cref="Buffer"/> with a large anough size to be used as the scratch buffer in a raytracing acceleration structure build</param>
        /// <returns>A <see cref="RaytracingAccelerationStructure"/> with a large anough size to be used as the destination in a raytracing acceleration structure build</returns>
        public RaytracingAccelerationStructure AllocateRaytracingAccelerationBuffer(AccelerationStructureBuildInfo info, out Buffer scratch)
            => AllocateRaytracingAccelerationBuffer(info, AllocFlags.None, out scratch);

        /// <summary>
        /// Allocates a buffer for use as a raytracing acceleration structure
        /// </summary>
        /// <param name="info">The <see cref="AccelerationStructureBuildInfo"/> containing the required sizes of the buffers</param>
        /// <param name="scratch">On return, this is filled with a <see cref="Buffer"/> with a large anough size to be used as the scratch buffer in a raytracing acceleration structure build</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A <see cref="RaytracingAccelerationStructure"/> with a large anough size to be used as the destination in a raytracing acceleration structure build</returns>
        public RaytracingAccelerationStructure AllocateRaytracingAccelerationBuffer(AccelerationStructureBuildInfo info, AllocFlags allocFlags, out Buffer scratch)
        {
            scratch = AllocateBuffer(info.ScratchSize, MemoryAccess.GpuOnly, ResourceFlags.AllowUnorderedAccess | ResourceFlags.DenyShaderResource, allocFlags);
            return AllocateRaytracingAccelerationBuffer(info.DestSize, allocFlags);
        }

        /// <summary>
        /// Allocates a buffer for use as a raytracing acceleration structure or scratch buffer for a raytracing acceleration structure build
        /// </summary>
        /// <param name="length">The length, in bytes, to allocate</param>
        /// <param name="allocFlags">Any additional allocation flags</param>
        /// <returns>A <see cref="RaytracingAccelerationStructure"/></returns>
        public RaytracingAccelerationStructure AllocateRaytracingAccelerationBuffer(ulong length, AllocFlags allocFlags = AllocFlags.None)
        {
            InternalAllocDesc allocDesc = default;
            CreateAllocDesc(new BufferDesc { Length = (long)length, ResourceFlags = ResourceFlags.AllowUnorderedAccess }, &allocDesc, MemoryAccess.GpuOnly, ResourceState.RaytracingAccelerationStructure, allocFlags);

            var buffer = new RaytracingAccelerationStructure(new Buffer(_device, Allocate(&allocDesc), 0, allocDesc));

            return buffer;
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

        internal void CreateAllocDesc(in TextureDesc desc, InternalAllocDesc* pDesc, ResourceState initialResourceState, AllocFlags allocFlags)
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
                SampleDesc = sample,
                Layout = (D3D12_TEXTURE_LAYOUT)desc.Layout
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

            var allocInfo = _device.GetAllocationInfo(pDesc);
            resDesc.Alignment = allocInfo.Alignment;

            var heapInfo = new HeapInfo
            {
                Alignment = (Alignment)allocInfo.Alignment,
                Access = MemoryAccess.GpuOnly,
                Type = GetResourceType(resDesc.Dimension, resDesc.Flags)
            };

            *pDesc = new InternalAllocDesc
            {
                Desc = resDesc,
                ClearValue = clearVal,
                InitialState = (D3D12_RESOURCE_STATES)initialResourceState,
                AllocFlags = allocFlags,
                HeapType = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_DEFAULT,
                HeapProperties = GetHeapProperties(heapInfo),
                Size = allocInfo.SizeInBytes
            };
        }
    }
}
