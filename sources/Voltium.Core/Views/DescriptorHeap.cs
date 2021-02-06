using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Toolkit.HighPerformance.Extensions;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using static TerraFX.Interop.D3D12_DESCRIPTOR_HEAP_FLAGS;
using static TerraFX.Interop.D3D12_DESCRIPTOR_HEAP_TYPE;

namespace Voltium.Core
{
    public readonly struct DescriptorAllocation : IDisposable
    {
        internal DescriptorAllocation(DescriptorHeap heap, DescriptorSpan span, uint offset)
        {
            Heap = heap;
            Span = span;

            Offset = offset;
        }

        public DescriptorHandle this[int index] => Span[index];
        public DescriptorHandle this[uint index] => Span[index];


        public DescriptorSpan Slice(int start) => Span.Slice(start);
        public DescriptorSpan Slice(int start, int length) => Span.Slice(start, length);

        public uint Offset { get; }
        public int Length => Span.Length;

        public DescriptorHeap Heap { get; }
        public DescriptorSpan Span { get; }

        public void Dispose()
        {
            Heap.Return(ref Unsafe.AsRef(in this));
        }
    }

    public readonly struct DescriptorSpan
     {
        public readonly int Length => (int)_length;

        internal readonly D3D12_CPU_DESCRIPTOR_HANDLE Cpu;
        internal readonly D3D12_GPU_DESCRIPTOR_HANDLE Gpu;
        private readonly uint _length;
        internal readonly ushort IncrementSize;
        internal readonly byte Type;

        internal DescriptorSpan(D3D12_CPU_DESCRIPTOR_HANDLE cpu, D3D12_GPU_DESCRIPTOR_HANDLE gpu, uint length, ushort incrementSize, byte type)
        {
            _length = length;
            Cpu = cpu;
            Gpu = gpu;
            IncrementSize = incrementSize;
            Type = type;
        }

        public DescriptorHandle this[uint index] => this[(int)index];
        public DescriptorHandle this[int index] => new DescriptorHandle(OffsetCpu(index), OffsetGpu(index));

        private D3D12_CPU_DESCRIPTOR_HANDLE OffsetCpu(int count) => Cpu.Offset(count, IncrementSize);
        private D3D12_GPU_DESCRIPTOR_HANDLE OffsetGpu(int count) => Gpu.Offset(Gpu.ptr == default ? 0 : count, IncrementSize);

        public DescriptorSpan Slice(int start)
        {
            if ((uint)start > (uint)_length)
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(start));

            return new DescriptorSpan(OffsetCpu(start), OffsetGpu(start), _length - (uint)start, IncrementSize, Type);
        }

        public DescriptorSpan Slice(int start, int length)
        {
            if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)_length)
                ThrowHelper.ThrowArgumentOutOfRangeException("start/length");

            return new DescriptorSpan(OffsetCpu(start), OffsetGpu(start), (uint)length, IncrementSize, Type);
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
    public unsafe class DescriptorHeap : IInternalD3D12Object
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

        ID3D12Object* IInternalD3D12Object.GetPointer() => (ID3D12Object*)_heap.Ptr;
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
