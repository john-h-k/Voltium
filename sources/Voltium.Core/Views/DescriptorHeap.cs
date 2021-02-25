using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Toolkit.HighPerformance.Extensions;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using static TerraFX.Interop.D3D12_DESCRIPTOR_HEAP_FLAGS;
using static TerraFX.Interop.D3D12_DESCRIPTOR_HEAP_TYPE;

namespace Voltium.Core
{
    public enum ShaderComponentMapping
    {
        Default,
        Red,
        Green,
        Blue,
        Alpha,
        One,
        Zero
    }

    public readonly struct DepthStencilView
    {
        internal readonly D3D12_CPU_DESCRIPTOR_HANDLE Handle;

        public DepthStencilView(D3D12_CPU_DESCRIPTOR_HANDLE handle) => Handle = handle;
    }

    public unsafe sealed class DepthViewPool : IDisposable
    {
        private GraphicsDevice _device;
        private UniqueComPtr<ID3D12DescriptorHeap> _dsvs;
        private D3D12_CPU_DESCRIPTOR_HANDLE _first;
        private uint _incrementSize;
        private readonly int _length;
        private int _next;

        internal DepthViewPool(GraphicsDevice device, int minimumLength)
        {
            _device = device;
            var desc = new D3D12_DESCRIPTOR_HEAP_DESC
            {
                Type = D3D12_DESCRIPTOR_HEAP_TYPE_DSV,
                NumDescriptors = (uint)minimumLength,
                NodeMask = 0 // TODO: MULTI-GPU
            };

            _incrementSize = _device.GetIncrementSizeForDescriptorType(D3D12_DESCRIPTOR_HEAP_TYPE_DSV);
            _length = minimumLength;
            _dsvs = device.CreateDescriptorHeap(&desc);
        }

        private D3D12_CPU_DESCRIPTOR_HANDLE Next()
        {
            var alloc = Interlocked.Increment(ref _next);
            if (alloc >= _length)
            {
                ThrowHelper.ThrowInvalidOperationException("Ran out of views!");
            }

            return new(_first, alloc, _incrementSize);
        }

        public DepthStencilView CreateView(in Texture texture) => CreateView(texture, null);

        public DepthStencilView CreateView(in Texture texture, uint mip) => CreateView(texture, mip, 0..(texture.IsArray ? texture.DepthOrArraySize : 1));
        public DepthStencilView CreateView(in Texture texture, Range array) => CreateView(texture, 0, array);

        public DepthStencilView CreateView(in Texture texture, uint mip, Range array)
        {
            var desc = new D3D12_DEPTH_STENCIL_VIEW_DESC();
            if (texture.Msaa.IsMultiSampled && mip != 0)
            {
                _device.ThrowGraphicsException("MSAA textures can only have a single mip, yet a mip >0 was requested");
            }

            var (offset, length) = array.GetOffsetAndLength(texture.IsArray ? texture.DepthOrArraySize : 1);
            if (!texture.IsArray && offset != 0 && length != 1)
            {
                _device.ThrowGraphicsException("Non-array textures can only have a single array slice, yet an array slice of >0 was requested");
            }

            switch (texture.Dimension)
            {
                case TextureDimension.Tex1D when texture.IsArray:
                    desc.ViewDimension = D3D12_DSV_DIMENSION.D3D12_DSV_DIMENSION_TEXTURE1DARRAY;
                    desc.Texture1DArray.MipSlice = mip;
                    desc.Texture1DArray.FirstArraySlice = (uint)offset;
                    desc.Texture1DArray.ArraySize = (uint)length;
                    break;

                case TextureDimension.Tex1D:
                    desc.ViewDimension = D3D12_DSV_DIMENSION.D3D12_DSV_DIMENSION_TEXTURE1D;
                    desc.Texture1D.MipSlice = mip;
                    break;

                case TextureDimension.Tex2D when texture.Msaa.IsMultiSampled && texture.IsArray:
                    desc.ViewDimension = D3D12_DSV_DIMENSION.D3D12_DSV_DIMENSION_TEXTURE2DMSARRAY;
                    desc.Texture2DMSArray.FirstArraySlice = (uint)offset;
                    desc.Texture2DMSArray.ArraySize = (uint)length;
                    break;

                case TextureDimension.Tex2D when texture.IsArray:
                    desc.ViewDimension = D3D12_DSV_DIMENSION.D3D12_DSV_DIMENSION_TEXTURE2DARRAY;
                    desc.Texture2DArray.MipSlice = mip;
                    desc.Texture2DArray.FirstArraySlice = (uint)offset;
                    desc.Texture2DArray.ArraySize = (uint)length;
                    break;

                case TextureDimension.Tex2D when texture.Msaa.IsMultiSampled:
                    desc.ViewDimension = D3D12_DSV_DIMENSION.D3D12_DSV_DIMENSION_TEXTURE2DMS;
                    desc.Texture2D.MipSlice = mip;
                    break;

                case TextureDimension.Tex2D:
                    desc.ViewDimension = D3D12_DSV_DIMENSION.D3D12_DSV_DIMENSION_TEXTURE2D;
                    desc.Texture2D.MipSlice = mip;
                    break;
            }

            return CreateView(texture, &desc);
        }

        private DepthStencilView CreateView(in Texture texture, D3D12_DEPTH_STENCIL_VIEW_DESC* desc)
        {
            var handle = Next();
            _device.DevicePointer->CreateDepthStencilView(texture.GetResourcePointer(), desc, handle);
            return new (handle);
        }
    }

    public enum ViewPoolType
    {
        Depth,
        Writable,
        ReadOnly
    }

    public readonly struct DescriptorAllocation
    {
        internal DescriptorSetHandle Handle;
        private Disposal<DescriptorSetHandle> _dispose;

        public DescriptorHandle this[int index] => Span[index];
        public DescriptorHandle this[uint index] => Span[index];


        public DescriptorSpan Slice(int start) => Span.Slice(start);
        public DescriptorSpan Slice(int start, int length) => Span.Slice(start, length);

        public uint Offset { get; }
        public int Length => Span.Length;

        public DescriptorSpan Span { get; }
    }

    public readonly struct DescriptorSpan
     {
        public readonly int Length => (int)_length;

        internal readonly D3D12_GPU_DESCRIPTOR_HANDLE Gpu;
        private readonly uint _length;
        internal readonly ushort IncrementSize;
        internal readonly byte Type;

        internal DescriptorSpan(D3D12_GPU_DESCRIPTOR_HANDLE gpu, uint length, ushort incrementSize, byte type)
        {
            _length = length;
            Gpu = gpu;
            IncrementSize = incrementSize;
            Type = type;
        }

        public DescriptorHandle this[uint index] => this[(int)index];
        public DescriptorHandle this[int index] => new DescriptorHandle(OffsetGpu(index));

        private D3D12_GPU_DESCRIPTOR_HANDLE OffsetGpu(int count) => Gpu.Offset(count, IncrementSize);

        public DescriptorSpan Slice(int start)
        {
            if ((uint)start > (uint)_length)
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(start));

            return new DescriptorSpan(OffsetGpu(start), _length - (uint)start, IncrementSize, Type);
        }

        public DescriptorSpan Slice(int start, int length)
        {
            if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)_length)
                ThrowHelper.ThrowArgumentOutOfRangeException("start/length");

            return new DescriptorSpan(OffsetGpu(start), (uint)length, IncrementSize, Type);
        }
    }

    public enum DescriptorAllocationHint
    {
        LongTerm,
        ShortTerm
    }


    ///// <summary>
    ///// A heap of descriptors for resources
    ///// </summary>
    //internal unsafe class ShaderVisibleDescriptorHeap : IInternalD3D12Object, IEvictable
    //{
    //    private ComputeDevice _device;
    //    private UniqueComPtr<ID3D12DescriptorHeap> _heap;
    //    private D3D12_DESCRIPTOR_HEAP_TYPE _type;
    //    private uint _incrementSize;
    //    private List<FreeBlock> _freeBlocks;

    //    //public ShaderVisibleDescriptorHeap(uint size)
    //    //{
    //    //    _device.Create
    //    //}


    //    private struct FreeBlock { public uint Offset, Length; }
    //}

    /// <summary>
    /// A heap of descriptors for resources
    /// </summary>
    public unsafe class DescriptorHeap : IInternalGraphicsObject
    {
        private ComputeDevice _device;
        private UniqueComPtr<ID3D12DescriptorHeap> _heap;
        private D3D12_DESCRIPTOR_HEAP_TYPE _type;
        private uint _incrementSize;
        private List<FreeBlock> _freeBlocks;

        private uint _count;
        private uint _offset;
        private DescriptorSpan _allDescriptors;

        internal ComputeDevice Device => _device;

        private struct FreeBlock { public uint Offset, Length; }

        /// <summary>
        /// Whether this <see cref="DescriptorHeap"/> has been created
        /// </summary>
        public bool Exists => _heap.Exists;

        internal ID3D12DescriptorHeap* GetHeap() => _heap.Ptr;

        /// <summary>
        /// The type of the descriptor heap
        /// </summary>
        public DescriptorHeapType Type { get; private set; }

        /// <summary>
        /// The number of descriptors in the heap
        /// </summary>
        public uint NumDescriptors { get; private set; }

        public DescriptorHandle this[uint index] => _allDescriptors[index];
        public DescriptorHandle this[int index] => _allDescriptors[index];

        public int Length => _allDescriptors.Length;
        public DescriptorSpan Slice(int start) => _allDescriptors.Slice(start);
        public DescriptorSpan Slice(int start, int length) => _allDescriptors.Slice(start, length);

        internal DescriptorHeap(
            ComputeDevice device,
            DescriptorHeapType type,
            uint descriptorCount,
            bool shaderVisible
        )
        {
            var desc = new D3D12_DESCRIPTOR_HEAP_DESC
            {
                Flags = shaderVisible ? D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE : D3D12_DESCRIPTOR_HEAP_FLAG_NONE,
                NodeMask = 0, // TODO: MULTI-GPU
                NumDescriptors = descriptorCount,
                Type = (D3D12_DESCRIPTOR_HEAP_TYPE)type
            };

            _device = device;

            _heap = device.CreateDescriptorHeap(&desc);

            var cpu = _heap.Ptr->GetCPUDescriptorHandleForHeapStart();
            var gpu = shaderVisible ? _heap.Ptr->GetGPUDescriptorHandleForHeapStart() : default;

            _incrementSize = device.GetIncrementSizeForDescriptorType(desc.Type);
            _allDescriptors = new DescriptorSpan(cpu, gpu, descriptorCount, (ushort)_incrementSize, (byte)type);
            _offset = 0;
            _count = desc.NumDescriptors;
            _type = desc.Type;

            Type = (DescriptorHeapType)desc.Type;
            NumDescriptors = desc.NumDescriptors;

            _freeBlocks = new List<FreeBlock> { new FreeBlock { Offset = 0, Length = _count } };
        }

        /// <summary>
        /// Gets the next handle in the heap
        /// </summary>
        public DescriptorAllocation AllocateHandle()
            => AllocateHandles(1);

        /// <summary>
        /// Gets the next <paramref name="count"/> handles in the heap
        /// </summary>
        public DescriptorAllocation AllocateHandles(int count)
            => AllocateHandles((uint)count);

        /// <summary>
        /// Gets the next <paramref name="count"/> handles in the heap
        /// </summary>
        public DescriptorAllocation AllocateHandles(uint count)
        {
            Guard.True(_offset + count <= _count, "Too many descriptors");

            foreach (ref var block in _freeBlocks.AsSpan())
            {
                if (block.Length >= count)
                {
                    var offset = (int)block.Offset;
                    block.Offset += count;
                    block.Length -= count;

                    return new DescriptorAllocation(this, _allDescriptors[offset..(offset + (int)count)], (uint)offset);
                }
            }

            return ThrowHelper.ThrowInsufficientMemoryException<DescriptorAllocation>("Descriptor heap full");
        }

        public void Return(ref DescriptorAllocation handle)
        {
            var copy = handle;
            handle = default;

            _freeBlocks.Add(new FreeBlock { Offset = copy.Offset, Length = (uint)copy.Length });
        }


        /// <summary>
        /// Resets the heap for reuse
        /// </summary>
        public void ResetHeap() => _offset = 0;

        /// <inheritdoc cref="IComType.Dispose"/>
        public void Dispose() => _heap.Dispose();

        ID3D12Object* IInternalGraphicsObject.GetPointer() => (ID3D12Object*)_heap.Ptr;
    }

    /// <summary>
    /// Represents the type of the descriptors in a <see cref="DescriptorHeap"/>
    /// </summary>
    public enum DescriptorHeapType
    {
        /// <summary>
        /// The descriptor represents a constant buffer, shader resource, or unordered access view
        /// </summary>
        ConstantBufferShaderResourceOrUnorderedAccessView = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV,

        /// <summary>
        /// The descriptor represents a sampler
        /// </summary>
        Sampler = D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER,

        /// <summary>
        /// The descriptor represents a render target view
        /// </summary>
        RenderTargetView = D3D12_DESCRIPTOR_HEAP_TYPE_RTV,

        /// <summary>
        /// The descriptor represents a depth stencil view
        /// </summary>
        DepthStencilView = D3D12_DESCRIPTOR_HEAP_TYPE_DSV,
    }
}
