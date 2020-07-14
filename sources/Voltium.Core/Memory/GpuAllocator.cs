using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Buffer = Voltium.Core.Memory.Buffer;
using Rectangle = System.Drawing.Rectangle;

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
    /// 
    /// </summary>
    public struct TextureClearValue
    {
        /// <summary>
        /// If used with a render target, the <see cref="Rgba128"/> to optimise clearing for
        /// </summary>
        public Rgba128 Color;

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
        /// <param name="color">The <see cref="Rgba128"/> to optimise clearing for</param>
        /// <returns>A new <see cref="TextureClearValue"/></returns>
        public static TextureClearValue CreateForRenderTarget(Rgba128 color) => new TextureClearValue { Color = color };

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
        /// Allows the resource to be used as a render target
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
        private ComputeDevice _device;
        private const ulong HighestRequiredAlign = 1024 * 1024 * 4; // 4mb

        private bool _hasMergedHeapSupport;

        /// <summary>
        /// Creates a new allocator
        /// </summary>
        /// <param name="device">The <see cref="ID3D12Device"/> to allocate on</param>
        public GpuAllocator(GraphicsDevice device)
        {
            Debug.Assert(device is not null);
            _device = device;

            _hasMergedHeapSupport = CheckMergedHeapSupport(_device.DevicePointer);

            CreateOriginalHeaps();
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
            ResourceState initialResourceState,
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
                SampleDesc = sample // TODO: MSAA
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

            // *BUG* heaps don't properly work because of byval passing , TODO
            // for now just commit all
            if (true || desc.AllocFlags.HasFlag(AllocFlags.ForceAllocateComitted))
            {
                return AllocateCommitted(desc);
            }

            var info = _device.GetAllocationInfo(desc);
            return AllocatePlacedFromHeap(desc, info);
        }

        private void VerifyDesc(InternalAllocDesc desc)
        {
            var flags = desc.AllocFlags;
            if (flags.HasFlag(AllocFlags.ForceAllocateComitted) && flags.HasFlag(AllocFlags.ForceAllocateNotComitted))
            {
                ThrowHelper.ThrowArgumentException("Invalid to say 'ForceAllocateComitted' and 'ForceAllocateNotComitted'");
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

        private const ulong DefaultHeapAlignment = Windows.D3D12_DEFAULT_MSAA_RESOURCE_PLACEMENT_ALIGNMENT; // 4mb for MSAA textures
        private ref AllocatorHeap CreateNewHeap(D3D12_HEAP_TYPE mem, GpuResourceType res)
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
            D3D12_HEAP_DESC desc = new(GetNewHeapSize(mem, res), props, alignment: DefaultHeapAlignment, flags: flags);

            var heap = _device.CreateHeap(desc);

            var allocatorHeap = new AllocatorHeap { Heap = heap.Move(), FreeBlocks = new() };
            bool result = allocatorHeap.FreeBlocks.Add(new HeapBlock { Offset = 0, Size = desc.SizeInBytes });
            Debug.Assert(result);
            _ = result;

            var pool = _heapPools[GetHeapIndex(mem, res)];
            pool.Add(allocatorHeap);

            return ref ListExtensions.GetRef(pool, pool.Count - 1);
        }

        private ulong GetNewHeapSize(D3D12_HEAP_TYPE mem, GpuResourceType res)
        {
            const ulong megabyte = 1024 * 1024;
            //const ulong kilobyte = 1024;

            var guess = res switch
            {
                GpuResourceType.Meaningless => 256 * megabyte, // shouldn't be reached
                GpuResourceType.Tex => 256 * megabyte,
                // GpuResourceType.RtOrDs => 64 * megabyte,
                GpuResourceType.RtOrDs => 128 * megabyte,
                GpuResourceType.Buffer => 64 * megabyte,
                _ => ulong.MaxValue, // shouldn't be reached
            };

            return guess;
        }

        internal void Return(GpuResource gpuAllocation)
        {
            var heap = gpuAllocation.Heap;

            uint refCount = uint.MaxValue;
            while (refCount != 0)
            {
                refCount = gpuAllocation.UnderlyingResource->Release();
            }
            if (heap.Heap.Exists)
            {
                ReturnPlacedAllocation(gpuAllocation);
            }
            else
            {
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

        private GpuResource AllocateCommitted(InternalAllocDesc desc)
        {
            var resource = _device.CreateCommittedResource(desc);

            return new GpuResource(
                _device,
                resource.Move(),
                desc,
                default
            );
        }

        private bool TryAllocateFromHeap(InternalAllocDesc desc, D3D12_RESOURCE_ALLOCATION_INFO info, ref AllocatorHeap heap, out GpuResource allocation)
        {
            if (TryGetFreeBlock(ref heap, info, out HeapBlock freeBlock))
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
                if (TryAllocateFromHeap(desc, info, ref ListExtensions.GetRef(heapList, i), out allocation))
                {
                    return allocation;
                }
            }

            // No free blocks available anywhere. Create a new heap
            ref var newHeap = ref CreateNewHeap(desc.HeapType, resType);
            var result = TryAllocateFromHeap(desc, info, ref newHeap, out allocation);
            if (!result) // too big to fit in heap, realloc as comitted
            {
                if (desc.AllocFlags.HasFlag(AllocFlags.ForceAllocateNotComitted))
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
                _device,
                resource.Move(),
                desc,
                this,
                heap,
                block
            );
        }

        private void MarkFreeBlockUsed(ref AllocatorHeap heap, HeapBlock wholeBlock, HeapBlock usedBlock)
        {
            heap.FreeBlocks.Remove(wholeBlock);

            var alignOffset = usedBlock.Offset - wholeBlock.Offset;

            if (alignOffset != 0)
            {
                var loBlock = new HeapBlock { Offset = wholeBlock.Offset, Size = usedBlock.Offset - wholeBlock.Offset };
                heap.FreeBlocks.Add(loBlock);
            }
            if (usedBlock.Size != wholeBlock.Size)
            {
                var hiBlock = new HeapBlock { Offset = usedBlock.Offset + usedBlock.Size, Size = wholeBlock.Size - usedBlock.Size - alignOffset };
                heap.FreeBlocks.Add(hiBlock);
            }
        }

        private bool TryGetFreeBlock(ref AllocatorHeap heap, D3D12_RESOURCE_ALLOCATION_INFO info, out HeapBlock freeBlock)
        {
            Debug.Assert(info.Alignment <= HighestRequiredAlign);

            // try and get a block that is already aligned
            foreach (var block in heap.FreeBlocks)
            {
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
                var alignedSize = block.Size - (alignedOffset - block.Offset);

                if (alignedSize >= info.SizeInBytes)
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
        /// Any addition resource flags
        /// </summary>
        public ResourceFlags ResourceFlags;
    }

    /// <summary>
    /// Describes a buffer, for use by the <see cref="GpuAllocator"/>
    /// </summary>
    public struct TextureDesc
    {
        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a 2D render target
        /// </summary>
        /// <param name="format">The <see cref="BackBufferFormat"/> for the render target</param>
        /// <param name="height">The height, in texels, of the target</param>
        /// <param name="width">The width, in texels, of the target</param>
        /// <param name="clearColor">The <see cref="Rgba128"/> to set to be the optimized clear value</param>
        /// <param name="msaa">Optionally, the <see cref="MultisamplingDesc"/> for the render target</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a render target</returns>
        public static TextureDesc CreateRenderTargetDesc(BackBufferFormat format, uint height, uint width, Rgba128 clearColor, MultisamplingDesc msaa = default)
            => CreateRenderTargetDesc((DataFormat)format, height, width, clearColor, msaa);

        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a 2D render target, with no height or width, for unspecified size targets
        /// </summary>
        /// <param name="format">The <see cref="BackBufferFormat"/> for the render target</param>
        /// <param name="clearColor">The <see cref="Rgba128"/> to set to be the optimized clear value</param>
        /// <param name="msaa">Optionally, the <see cref="MultisamplingDesc"/> for the render target</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a render target</returns>
        public static TextureDesc CreateRenderTargetDesc(BackBufferFormat format, Rgba128 clearColor, MultisamplingDesc msaa = default)
            => CreateRenderTargetDesc((DataFormat)format, 0, 0, clearColor, msaa);

        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a 2D render target, with no height or width, for unspecified size targets
        /// </summary>
        /// <param name="format">The <see cref="BackBufferFormat"/> for the render target</param>
        /// <param name="clearColor">The <see cref="Rgba128"/> to set to be the optimized clear value</param>
        /// <param name="msaa">Optionally, the <see cref="MultisamplingDesc"/> for the render target</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a render target</returns>
        public static TextureDesc CreateRenderTargetDesc(DataFormat format, Rgba128 clearColor, MultisamplingDesc msaa = default)
            => CreateRenderTargetDesc(format, 0, 0, clearColor, msaa);

        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a 2D render target
        /// </summary>
        /// <param name="format">The <see cref="DataFormat"/> for the render target</param>
        /// <param name="height">The height, in texels, of the target</param>
        /// <param name="width">The width, in texels, of the target</param>
        /// <param name="clearColor">The <see cref="Rgba128"/> to set to be the optimized clear value</param>
        /// <param name="msaa">Optionally, the <see cref="MultisamplingDesc"/> for the render target</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a render target</returns>
        public static TextureDesc CreateRenderTargetDesc(DataFormat format, uint height, uint width, Rgba128 clearColor, MultisamplingDesc msaa = default)
        {
            return new TextureDesc
            {
                Height = height,
                Width = width,
                DepthOrArraySize = 1,
                MipCount = 1,
                Dimension = TextureDimension.Tex2D,
                Format = format,
                ClearValue = TextureClearValue.CreateForRenderTarget(clearColor),
                Msaa = msaa,
                ResourceFlags = ResourceFlags.AllowRenderTarget,
            };
        }

        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a 2D depth stencil, with no height or width, for unspecified size targets
        /// </summary>
        /// <param name="format">The <see cref="DataFormat"/> for the depth stencil</param>s
        /// <param name="clearDepth">The <see cref="float"/> to set to be the optimized clear value for the depth element</param>
        /// <param name="clearStencil">The <see cref="byte"/> to set to be the optimized clear value for the stencil element</param>
        /// <param name="shaderVisible">Whether the <see cref="Texture"/> is shader visible. <see langword="true"/> by default</param>
        /// <param name="msaa">Optionally, the <see cref="MultisamplingDesc"/> for the depth stencil</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a depth stencil</returns>
        public static TextureDesc CreateDepthStencilDesc(DataFormat format, float clearDepth, byte clearStencil, bool shaderVisible = true, MultisamplingDesc msaa = default)
        => CreateDepthStencilDesc(format, 0, 0, clearDepth, clearStencil, shaderVisible, msaa);

        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a 2D depth stencil
        /// </summary>
        /// <param name="format">The <see cref="DataFormat"/> for the depth stencil</param>
        /// <param name="height">The height, in texels, of the depth stencil</param>
        /// <param name="width">The width, in texels, of the depth stencil</param>
        /// <param name="clearDepth">The <see cref="float"/> to set to be the optimized clear value for the depth element</param>
        /// <param name="clearStencil">The <see cref="byte"/> to set to be the optimized clear value for the stencil element</param>
        /// <param name="shaderVisible">Whether the <see cref="Texture"/> is shader visible. <see langword="true"/> by default</param>
        /// <param name="msaa">Optionally, the <see cref="MultisamplingDesc"/> for the depth stencil</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a depth stencil</returns>
        public static TextureDesc CreateDepthStencilDesc(DataFormat format, uint height, uint width, float clearDepth, byte clearStencil, bool shaderVisible = true, MultisamplingDesc msaa = default)
        {
            return new TextureDesc
            {
                Height = height,
                Width = width,
                DepthOrArraySize = 1,
                MipCount = 1,
                Dimension = TextureDimension.Tex2D,
                Format = format,
                ClearValue = TextureClearValue.CreateForDepthStencil(clearDepth, clearStencil),
                Msaa = msaa,
                ResourceFlags = ResourceFlags.AllowDepthStencil | (shaderVisible ? 0 : ResourceFlags.DenyShaderResource),
            };
        }


        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a shader resource, with no height or width, for unspecified size resources
        /// </summary>
        /// <param name="format">The <see cref="DataFormat"/> for the shader resource</param>
        /// <param name="dimension">The <see cref="TextureDimension"/> of the resource
        /// is <see cref="TextureDimension.Tex3D"/>, else, the number of textures in the array</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a shader resource</returns>
        public static TextureDesc CreateShaderResourceDesc(DataFormat format, TextureDimension dimension)
            => CreateShaderResourceDesc(format, dimension, 0, 0, 0);

        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a shader resource
        /// </summary>
        /// <param name="format">The <see cref="DataFormat"/> for the shader resource</param>
        /// <param name="dimension">The <see cref="TextureDimension"/> of the resource</param>
        /// <param name="height">The height, in texels, of the resource</param>
        /// <param name="width">The width, in texels, of the resource</param>
        /// <param name="depthOrArraySize">The depth, in texels, of the resource, if <paramref name="dimension"/>
        /// is <see cref="TextureDimension.Tex3D"/>, else, the number of textures in the array</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a shader resource</returns>
        public static TextureDesc CreateShaderResourceDesc(DataFormat format, TextureDimension dimension, uint height, uint width = 1, ushort depthOrArraySize = 1)
        {
            return new TextureDesc
            {
                Height = height,
                Width = width,
                DepthOrArraySize = depthOrArraySize,
                Dimension = dimension,
                Format = format,
            };
        }

        /// <summary>
        /// Creates a new <see cref="TextureDesc"/> representing a unordered access resource
        /// </summary>
        /// <param name="format">The <see cref="DataFormat"/> for the unordered access resource</param>
        /// <param name="dimension">The <see cref="TextureDimension"/> of the unordered access resource</param>
        /// <param name="height">The height, in texels, of the resource</param>
        /// <param name="width">The width, in texels, of the resource</param>
        /// <param name="depthOrArraySize">The depth, in texels, of the resource, if <paramref name="dimension"/>
        /// is <see cref="TextureDimension.Tex3D"/>, else, the number of textures in the array</param>
        /// <returns>A new <see cref="TextureDesc"/> representing a shader resource</returns>
        public static TextureDesc CreateUnorderedAccessResourceDesc(DataFormat format, TextureDimension dimension, uint height, uint width = 1, ushort depthOrArraySize = 1)
        {
            return new TextureDesc
            {
                Height = height,
                Width = width,
                DepthOrArraySize = depthOrArraySize,
                Dimension = dimension,
                Format = format,
                ResourceFlags = ResourceFlags.AllowUnorderedAccess
            };
        }

        /// <summary>
        /// The format of the texture
        /// </summary>
        public DataFormat Format;

        /// <summary>
        /// The number of dimensions in the texture
        /// </summary>
        public TextureDimension Dimension;

        /// <summary>
        /// The number of mips this resource will contain
        /// </summary>
        public ushort MipCount;

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
        /// If this texture is a render target or depth stencil, the value for which it is optimised to call <see cref="GraphicsContext.ClearRenderTarget(DescriptorHandle, Rgba128, Rectangle)"/>
        /// or <see cref="GraphicsContext.ClearDepthStencil(DescriptorHandle, float, byte, ReadOnlySpan{Rectangle})"/> for
        /// </summary>
        public TextureClearValue? ClearValue;

        /// <summary>
        /// Any addition resource flags
        /// </summary>
        public ResourceFlags ResourceFlags;

        /// <summary>
        /// Optionally, the <see cref="MultisamplingDesc"/> describing multi-sampling for this texture.
        /// This is only meaningful when used with a render target or depth stencil
        /// </summary>
        public MultisamplingDesc Msaa;
    }
}
