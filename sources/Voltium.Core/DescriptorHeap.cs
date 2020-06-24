using System.Diagnostics;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.GpuResources;
using Voltium.Core.Managers;
using Voltium.Core.Memory.GpuResources;
using static TerraFX.Interop.D3D12_DESCRIPTOR_HEAP_FLAGS;
using static TerraFX.Interop.D3D12_DESCRIPTOR_HEAP_TYPE;

namespace Voltium.Core
{
    /// <summary>
    /// Describes the metadata used to create a shader resource view to a <see cref="Buffer"/>
    /// </summary>
    public struct BufferShaderResourceViewDesc
    {
        /// <summary>
        /// The <see cref="DataFormat"/> the buffer will be viewed as
        /// </summary>
        public DataFormat Format;

        /// <summary>
        /// The number of elements to view
        /// </summary>
        public uint ElementCount;

        /// <summary>
        /// The size, in bytes, of each element
        /// </summary>
        public uint ElementStride;

        /// <summary>
        /// The offset, in bytes, to start the view at
        /// </summary>
        public ulong Offset;

        /// <summary>
        /// Whether the buffer should be viewed as a raw buffer
        /// </summary>
        public bool Raw;
    }

    /// <summary>
    /// Describes the metadata used to create a shader resource view to a <see cref="Texture"/>
    /// </summary>
    public struct TextureShaderResourceViewDesc
    {
        /// <summary>
        /// The <see cref="DataFormat"/> the texture will be viewed as
        /// </summary>
        public DataFormat Format;

        /// <summary>
        /// The index of the most detailed mip to use
        /// </summary>
        public uint MostDetailedMip;

        /// <summary>
        /// The number of mip levels to use, or -1 to use all available
        /// </summary>
        public uint MipLevels;

        /// <summary>
        /// The minimum LOD to clamp to
        /// </summary>
        public float ResourceMinLODClamp;

        /// <summary>
        /// For 2D views to 2D arrays or 3D textures, the index to the plane to view
        /// </summary>
        public uint PlaneSlice;

        /// <summary>
        /// Whether the view should be multisampled
        /// </summary>
        public bool IsMultiSampled;
    }

    /// <summary>
    /// A heap of descriptors for resources
    /// </summary>
    public unsafe struct DescriptorHeap
    {
        private ComPtr<ID3D12DescriptorHeap> _heap;

        internal ID3D12DescriptorHeap* GetHeap() => _heap.Get();

        /// <summary>
        /// The type of the descriptor heap
        /// </summary>
        public DescriptorHeapType Type { get; private set; }

        /// <summary>
        /// The number of descriptors in the heap
        /// </summary>
        public uint NumDescriptors { get; private set; }

        private static D3D12_DESCRIPTOR_HEAP_DESC CreateDesc(
            D3D12_DESCRIPTOR_HEAP_TYPE type,
            uint numDescriptors,
            bool shaderVisible
        )
        {
            var desc = new D3D12_DESCRIPTOR_HEAP_DESC
            {
                Flags = shaderVisible ? D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE : D3D12_DESCRIPTOR_HEAP_FLAG_NONE,
                NodeMask = 0, // TODO: MULTI-GPU
                NumDescriptors = numDescriptors,
                Type = type
            };

            return desc;
        }

        /// <summary>
        /// Create a new <see cref="DescriptorHeap"/> that contains render target views using a <see cref="ID3D12Device"/>
        /// </summary>
        /// <param name="device">The device to use during creation</param>
        /// <param name="renderTargetCount">The number of render target view descriptors</param>
        public static DescriptorHeap CreateRenderTargetViewHeap(
            GraphicsDevice device,
            uint renderTargetCount
        )
        {
            var desc = CreateDesc(
                D3D12_DESCRIPTOR_HEAP_TYPE_RTV,
                renderTargetCount,
                false
            );

            return new DescriptorHeap(device, desc);
        }

        /// <summary>
        /// Create a new <see cref="DescriptorHeap"/> that contains depth stencil views using a <see cref="ID3D12Device"/>
        /// </summary>
        /// <param name="device">The device to use during creation</param>
        /// <param name="depthStencilCount">The number of depth stencil view descriptors</param>
        public static DescriptorHeap CreateDepthStencilViewHeap(
            GraphicsDevice device,
            uint depthStencilCount
        )
        {
            var desc = CreateDesc(
                D3D12_DESCRIPTOR_HEAP_TYPE_DSV,
                depthStencilCount,
                false
            );

            return new DescriptorHeap(device, desc);
        }

        /// <summary>
        /// Create a new <see cref="DescriptorHeap"/> that contains constant buffer, shader resource, and unordered access views using a <see cref="ID3D12Device"/>
        /// </summary>
        /// <param name="device">The device to use during creation</param>
        /// <param name="shaderResourceCount">The number of view descriptors</param>
        public static DescriptorHeap CreateConstantBufferShaderResourceUnorderedAccessViewHeap(
            GraphicsDevice device,
            uint shaderResourceCount
        )
        {
            var desc = CreateDesc(
                D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV,
                shaderResourceCount,
                true // always want this shader visible
            );

            return new DescriptorHeap(device, desc);
        }

        /// <summary>
        /// Create a new <see cref="DescriptorHeap"/> that contains samplers using a <see cref="ID3D12Device"/>
        /// </summary>
        /// <param name="device">The device to use during creation</param>
        /// <param name="samplerCount">The number of sampler descriptors</param>
        public static DescriptorHeap CreateSamplerHeap(
            GraphicsDevice device,
            uint samplerCount
        )
        {
            var desc = CreateDesc(
                D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER,
                samplerCount,
                true // always want this shader visible
            );

            return new DescriptorHeap(device, desc);
        }


        private DescriptorHeap(GraphicsDevice device, D3D12_DESCRIPTOR_HEAP_DESC desc)
        {
            ComPtr<ID3D12DescriptorHeap> heap = default;
            Guard.ThrowIfFailed(device.Device->CreateDescriptorHeap(&desc, heap.Guid, (void**)&heap));

            _heap = heap.Move();
            var cpu = _heap.Get()->GetCPUDescriptorHandleForHeapStart();
            var gpu = _heap.Get()->GetGPUDescriptorHandleForHeapStart();

            _firstHandle = new DescriptorHandle(device, cpu, gpu, desc.Type);
            _offset = 0;
            _count = desc.NumDescriptors;

            Type = (DescriptorHeapType)desc.Type;
            NumDescriptors = desc.NumDescriptors;

            DirectXHelpers.SetObjectName(_heap.Get(), nameof(ID3D12DescriptorHeap) + " " + desc.Type.ToString());
        }

        /// <summary>
        /// The description of the heap
        /// </summary>
        public D3D12_DESCRIPTOR_HEAP_DESC Desc => _heap.Get()->GetDesc();

        private uint _count;
        private uint _offset;
        private DescriptorHandle _firstHandle;

        /// <summary>
        /// Gets the next handle in the heap
        /// </summary>
        public DescriptorHandle GetNextHandle()
        {
            Guard.True(_offset < _count, "Too many descriptors");
            return _firstHandle + _offset++;
        }

        /// <summary>
        /// Resets the heap for reuse
        /// </summary>
        public void ResetHeap() => _offset = 0;
        
        /// <inheritdoc cref="IComType.Dispose"/>
        public void Dispose() => _heap.Dispose();
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
