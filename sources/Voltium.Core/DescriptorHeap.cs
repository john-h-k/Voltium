using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Managers;
using static TerraFX.Interop.D3D12_DESCRIPTOR_HEAP_FLAGS;
using static TerraFX.Interop.D3D12_DESCRIPTOR_HEAP_TYPE;

namespace Voltium.Core
{
    /// <summary>
    /// A heap of descriptors for resources
    /// </summary>
    public unsafe struct DescriptorHeap
    {
        private ComPtr<ID3D12DescriptorHeap> _heap;

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
        /// <param name="shaderResourceCount">The number of depth stencil view descriptors</param>
        public static DescriptorHeap CreateConstantBufferShaderResourceUnorderedAccessViewHeap(
            GraphicsDevice device,
            uint shaderResourceCount
        )
        {
            var desc = CreateDesc(
                D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV,
                shaderResourceCount,
                false
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

            FirstDescriptor = new DescriptorHandle(device, cpu, gpu, desc.Type);

            Type = (DescriptorHeapType)desc.Type;
            NumDescriptors = desc.NumDescriptors;

            DirectXHelpers.SetObjectName(_heap.Get(), nameof(ID3D12DescriptorHeap));
        }

        /// <summary>
        /// The description of the heap
        /// </summary>
        public D3D12_DESCRIPTOR_HEAP_DESC Desc => _heap.Get()->GetDesc();

        /// <summary>
        /// The handle to the start of the descriptor heap
        /// </summary>
        public DescriptorHandle FirstDescriptor { get; private set; }

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
