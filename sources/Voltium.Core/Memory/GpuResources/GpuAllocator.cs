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
using Voltium.Core.Memory.GpuResources;
using Buffer = Voltium.Core.Memory.GpuResources.Buffer;
using System.Runtime.CompilerServices;

namespace Voltium.Core.GpuResources
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
    /// 
    /// </summary>
    public struct TextureClearValue
    {
        /// <summary>
        /// If used with a render target, the <see cref="RgbaColor"/> to optimise clearing for
        /// </summary>
        public RgbaColor Color;


        /// <summary>
        /// If used with a depth target, the <see cref="float"/> to optimise clearing depth for
        /// </summary>
        public float Depth;

        /// <summary>
        /// If used with a depth target, the <see cref="byte"/> to optimise clearing stencil for
        /// </summary>
        public byte Stencil;

        /// <summary>
        /// Creates a new <see cref="TextureClearValue"/> for a render target
        /// </summary>
        /// <param name="color">The <see cref="RgbaColor"/> to optimise clearing for</param>
        /// <returns>A new <see cref="TextureClearValue"/></returns>
        public static TextureClearValue CreateForRenderTarget(RgbaColor color) => new TextureClearValue { Color = color };

        /// <summary>
        /// Creates a new <see cref="TextureClearValue"/> for a render target
        /// </summary>
        /// <param name="depth">The <see cref="float"/> to optimise clearing depth for</param>
        /// <param name="stencil">The <see cref="byte"/> to optimise clearing stencil for</param>
        /// <returns>A new <see cref="TextureClearValue"/></returns>
        public static TextureClearValue CreateForDepthStencil(float depth, byte stencil) => new TextureClearValue { Depth = depth, Stencil = stencil };
    }

    /// <summary>
    /// Flags used in resource creation
    /// </summary>
    public enum ResourceFlags : uint
    {
        /// <summary>
        /// None
        /// </summary>
        None = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_NONE,

        /// <summary>
        /// Allows the resource to be used as a depth stencil. This is only relevant if the resource is a texture
        /// </summary>
        AllowDepthStencil = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL,

        /// <summary>
        /// Allows the resource to be used as a render target. This is only relevant if the resource is a texture
        /// </summary>
        AllowRenderTarget = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET,

        /// <summary>
        /// Allows the resource to be used as an unordered access resource
        /// </summary>
        AllowUnorderedAccess = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS,

        /// <summary>
        /// Prevents the resource being used by shaders
        /// </summary>
        DenyShaderResource = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_DENY_SHADER_RESOURCE,
    }

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

        /// <summary>
        /// Allocates a buffer
        /// </summary>
        /// <param name="desc">The <see cref="BufferDesc"/> describing the buffer</param>
        /// <returns>A new <see cref="Buffer"/></returns>
        public Buffer AllocateBuffer(
            in BufferDesc desc
        )
            => AllocateBuffer(desc.Length, desc.MemoryKind, desc.InitialResourceState, desc.ResourceFlags, desc.AllocFlags);

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
            ResourceState initialResourceState,
            ResourceFlags resourceFlags = ResourceFlags.None,
            AllocFlags allocFlags = AllocFlags.None
        )
        {
            var desc = new D3D12_RESOURCE_DESC
            {
                Width = (ulong)length,
                Height = 1,
                DepthOrArraySize = 1,
                Dimension = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER,
                Flags = (D3D12_RESOURCE_FLAGS)resourceFlags,
                MipLevels = 1,
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

            return new Buffer((ulong)length, Allocate(resource));
        }

        //public Texture AllocateTexture1D(
        //    DataFormat format,
        //    ushort width,
        //    MemoryAccess memoryKind,
        //    ResourceState initialResourceState,
        //    ResourceFlags flags = ResourceFlags.None,
        //    AllocFlags allocFlags = AllocFlags.None
        //)
        //    => AllocateTexture(format, TextureDimension.Tex1D, width, 1, 1, memoryKind, initialResourceState, flags);

        //public Texture AllocateTexture2D(
        //    DataFormat format,
        //    ulong width,
        //    uint height,
        //    MemoryAccess memoryKind,
        //    ResourceState initialResourceState,
        //    ResourceFlags flags = ResourceFlags.None,
        //    AllocFlags allocFlags = AllocFlags.None
        //)
        //    => AllocateTexture(format, TextureDimension.Tex2D, width, height, 1, memoryKind, initialResourceState, flags);

        //public Texture AllocateTexture3D(
        //    DataFormat format,
        //    ulong width,
        //    uint height,
        //    ushort depth,
        //    MemoryAccess memoryKind,
        //    ResourceState initialResourceState,
        //    ResourceFlags flags = ResourceFlags.None,
        //    AllocFlags allocFlags = AllocFlags.None
        //)
        //    => AllocateTexture(format, TextureDimension.Tex3D, width, height, depth, memoryKind, initialResourceState, flags);

        //public Texture AllocateTextureArray1D(
        //    DataFormat format,
        //    ulong width,
        //    ushort arraySize,
        //    MemoryAccess memoryKind,
        //    ResourceState initialResourceState,
        //    ResourceFlags flags = ResourceFlags.None,
        //    AllocFlags allocFlags = AllocFlags.None
        //)
        //    => AllocateTexture(format, TextureDimension.Tex1D, width, 1, arraySize, memoryKind, initialResourceState, flags);

        //public Texture AllocateTextureArray2D(
        //    DataFormat format,
        //    ulong width,
        //    uint height,
        //    ushort arraySize,
        //    MemoryAccess memoryKind,
        //    ResourceState initialResourceState,
        //    ResourceFlags flags = ResourceFlags.None,
        //    AllocFlags allocFlags = AllocFlags.None
        //)
        //    => AllocateTexture(format, TextureDimension.Tex2D, width, height, arraySize, memoryKind, initialResourceState, flags);


        /// <summary>
        /// Allocates a texture
        /// </summary>
        /// <param name="desc">The <see cref="TextureDesc"/> describing the texture</param>
        /// <returns>A new <see cref="Texture"/></returns>
        public Texture AllocateTexture(
            in TextureDesc desc
        )
        {
            var resDesc = new D3D12_RESOURCE_DESC
            {
                Dimension = (D3D12_RESOURCE_DIMENSION)desc.Dimension,
                Alignment = 0,
                Width = desc.Width,
                Height = desc.Height,
                DepthOrArraySize = desc.DepthOrArraySize,
                MipLevels = 0,
                Format = (DXGI_FORMAT)desc.Format,
                Flags = (D3D12_RESOURCE_FLAGS)desc.ResourceFlags,
                SampleDesc = new DXGI_SAMPLE_DESC(1, 0) // TODO: MSAA
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
                InitialState = (D3D12_RESOURCE_STATES)desc.InitialResourceState,
                AllocFlags = desc.AllocFlags,
                HeapType = (D3D12_HEAP_TYPE)desc.MemoryKind
            };

            return new Texture(desc, Allocate(resource));
        }

        


        /// <summary>
        /// Allocates a new region of GPU memory
        /// </summary>
        /// <param name="desc">The description for the resource to allocate</param>
        /// <returns>A new <see cref="GpuResource"/> which encapsulates the allocated region</returns>
        internal GpuResource Allocate(
            InternalAllocDesc desc
        )
        {
            VerifyDesc(desc);

            var info = _device.GetAllocationInfo(desc);

            if (desc.AllocFlags.HasFlag(AllocFlags.ForceAllocateComitted))
            {
                return AllocateCommitted(desc, info);
            }

            return AllocatePlacedFromHeap(desc, info);
        }

        private void VerifyDesc(InternalAllocDesc desc)
        {
            var flags = desc.AllocFlags;
            if (flags.HasFlag(AllocFlags.ForceAllocateComitted))
            {
                Debug.Assert(!flags.HasFlag(AllocFlags.ForceAllocateNotComitted));
            }
            else if (flags.HasFlag(AllocFlags.ForceAllocateNotComitted))
            {
                Debug.Assert(!flags.HasFlag(AllocFlags.ForceAllocateComitted));
            }
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

            for (var i = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_DEFAULT; i <= D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_READBACK; i++)
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

        private void GetHeapType(int index, out D3D12_HEAP_TYPE mem, out GpuResourceType res)
        {
            if (_hasMergedHeapSupport)
            {
                mem = (D3D12_HEAP_TYPE)index + 1;
                res = GpuResourceType.Meaningless;
                return;
            }

            mem = (D3D12_HEAP_TYPE)(index / 3) + 1;
            res = GpuResourceType.Meaningless;
        }

        private int GetHeapIndex(D3D12_HEAP_TYPE mem, GpuResourceType res)
        {
            if (_hasMergedHeapSupport)
            {
                return (int)(mem - 1);
            }

            var ind = (((int)mem - 1) * 3) + (int)(res - 1);
            return ind;
        }

        private AllocatorHeap CreateNewHeap(D3D12_HEAP_TYPE mem, GpuResourceType res)
        {
            D3D12_HEAP_FLAGS flags = default;

            if (!_hasMergedHeapSupport)
            {
                flags = res switch
                {
                    GpuResourceType.Meaningless => 0, // shouldn't be reached
                    GpuResourceType.Tex => D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES | D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_DENY_BUFFERS,
                    GpuResourceType.RtOrDs => D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES | D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_DENY_BUFFERS,
                    GpuResourceType.Buffer => D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_DENY_NON_RT_DS_TEXTURES | D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_DENY_RT_DS_TEXTURES,
                    _ => 0, // shouldn't be reached
                };
            }

            D3D12_HEAP_PROPERTIES props = new(mem);
            D3D12_HEAP_DESC desc = new(GetNewHeapSize(mem, res), props, flags: flags);

            var heap = _device.CreateHeap(desc);

            var allocatorHeap = new AllocatorHeap { Heap = heap.Move(), FreeBlocks = new() };
            bool result = allocatorHeap.FreeBlocks.Add(new HeapBlock { Offset = 0, Size = desc.SizeInBytes });
            Debug.Assert(result);
            _ = result;

            return allocatorHeap;
        }

        private ulong GetNewHeapSize(D3D12_HEAP_TYPE mem, GpuResourceType res)
        {
            const ulong megabyte = 1024 * 1024;
            //const ulong kilobyte = 1024;

            var guess = res switch
            {
                GpuResourceType.Meaningless => 256 * megabyte , // shouldn't be reached
                GpuResourceType.Tex => 256 * megabyte,
                GpuResourceType.RtOrDs => 64 * megabyte,
                GpuResourceType.Buffer => 256 * megabyte,
                _ => ulong.MaxValue, // shouldn't be reached
            };

            return guess;
        }

        internal void Return(GpuResource gpuAllocation)
        {
            var heap = gpuAllocation.Heap;
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
            var block = gpuAllocation.Block;
            var heap = gpuAllocation.Heap;

            heap.FreeBlocks.Add(block);

            // TODO defrag
        }

        private static uint CalculateConstantBufferSize(int size)
            => (uint)((size + 255) & ~255);

        private GpuResource AllocateCommitted(InternalAllocDesc desc, D3D12_RESOURCE_ALLOCATION_INFO allocInfo)
        {
            var resource = _device.CreateComittedResource(desc);

            return new GpuResource(
                resource.Move(),
                desc,
                default
            );
        }

        private bool TryAllocateFromHeap(InternalAllocDesc desc, D3D12_RESOURCE_ALLOCATION_INFO info, AllocatorHeap heap, out GpuResource allocation)
        {
            if (TryGetFreeBlock(heap, info, out HeapBlock freeBlock))
            {
                allocation = CreatePlaced(desc, heap, freeBlock);
                return true;
            }

            allocation = default!;
            return false;
        }

        private GpuResource AllocatePlacedFromHeap(InternalAllocDesc desc, D3D12_RESOURCE_ALLOCATION_INFO info)
        {
            var resType = GetResType(desc);
            GpuResource allocation;
            var heapList = _heapPools[GetHeapIndex(desc.HeapType, resType)];
            for (var i = 0; i < heapList.Count; i++)
            {
                if (TryAllocateFromHeap(desc, info, heapList[i], out allocation))
                {
                    return allocation;
                }
            }

            // No free blocks available anywhere. Create a new heap
            var newHeap = CreateNewHeap(desc.HeapType, resType);
            var result = TryAllocateFromHeap(desc, info, newHeap, out allocation);
            Debug.Assert(result);
            return allocation;
        }

        private GpuResourceType GetResType(InternalAllocDesc desc)
        {
            const D3D12_RESOURCE_FLAGS rtOrDsFlags =
                D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL | D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET;

            if (_hasMergedHeapSupport)
            {
                return GpuResourceType.Meaningless;
            }

            if (desc.Desc.Dimension == D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER)
            {
                return GpuResourceType.Buffer;
            }
            if ((desc.Desc.Flags & rtOrDsFlags) != 0)
            {
                return GpuResourceType.RtOrDs;
            }
            return GpuResourceType.Tex;
        }

        private GpuResource CreatePlaced(InternalAllocDesc desc, AllocatorHeap heap, HeapBlock block)
        {
            var resource = _device.CreatePlacedResource(heap.Heap.Get(), block.Offset, desc);

            return new GpuResource(
                resource.Move(),
                desc,
                this,
                heap,
                block
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


    /// <summary>
    /// Describes a buffer, for use by the <see cref="GpuAllocator"/>
    /// </summary>
    public struct BufferDesc
    {
        /// <summary>
        /// The size of the buffer, in bytes
        /// </summary>
        public long Length;

        /// <summary>
        /// The 
        /// </summary>
        public MemoryAccess MemoryKind;

        /// <summary>
        /// The state of the resource when it is allocated. This is ignored for <see cref="MemoryAccess.CpuUpload"/> buffers
        /// </summary>
        public ResourceState InitialResourceState;

        /// <summary>
        /// Any addition resource flags
        /// </summary>
        public ResourceFlags ResourceFlags;

        /// <summary>
        /// Any additional allocation flags
        /// </summary>
        public AllocFlags AllocFlags;
    }

    /// <summary>
    /// Describes a buffer, for use by the <see cref="GpuAllocator"/>
    /// </summary>
    public struct TextureDesc
    {
        /// <summary>
        /// The format of the texture
        /// </summary>
        public DataFormat Format;

        /// <summary>
        /// The number of dimensions in the texture
        /// </summary>
        public TextureDimension Dimension;

        /// <summary>
        /// The width, in bytes, of the texture
        /// </summary>
        public ulong Width;

        /// <summary>
        /// The height, in bytes, of the texture
        /// </summary>
        public uint Height;

        /// <summary>
        /// The depth, if <see cref="Dimension"/> is <see cref="TextureDimension.Tex3D"/>, else the number of elements in this texture array
        /// </summary>
        public ushort DepthOrArraySize;

        /// <summary>
        /// If this texture is a render target or depth stencil, the value for which it is optimised to call <see cref="GraphicsContext.ClearRenderTarget(DescriptorHandle, RgbaColor, Rectangle)"/>
        /// or <see cref="GraphicsContext.ClearDepthStencil(DescriptorHandle, float, byte, ReadOnlySpan{Rectangle})"/> for
        /// </summary>
        public TextureClearValue? ClearValue;

        /// <summary>
        /// The 
        /// </summary>
        public MemoryAccess MemoryKind;

        /// <summary>
        /// The state of the resource when it is allocated. This is ignored for <see cref="MemoryAccess.CpuUpload"/> buffers
        /// </summary>
        public ResourceState InitialResourceState;

        /// <summary>
        /// Any addition resource flags
        /// </summary>
        public ResourceFlags ResourceFlags;

        /// <summary>
        /// Any additional allocation flags
        /// </summary>
        public AllocFlags AllocFlags;
    }
}
