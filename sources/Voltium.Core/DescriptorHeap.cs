using System;
using TerraFX.Interop;
using Voltium.Common;
using static TerraFX.Interop.D3D12_DESCRIPTOR_HEAP_FLAGS;

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
        public D3D12_DESCRIPTOR_HEAP_TYPE Type { get; private set; }

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
        /// Create a new <see cref="DescriptorHeap"/> representing a render target view using a <see cref="ID3D12Device"/>
        /// </summary>
        /// <param name="device">The device to use during creation</param>
        /// <param name="bufferCount">The number of render target view descriptors</param>
        /// <param name="shaderVisible">Whether the render target view must be visible to shaders</param>
        public static DescriptorHeap CreateRenderTargetViewHeap(
            ID3D12Device* device,
            uint bufferCount,
            bool shaderVisible = false
        )
        {
            var desc = CreateDesc(
                D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_RTV,
                bufferCount,
                shaderVisible
            );

            return new DescriptorHeap(device, desc);
        }

        /// <summary>
        /// Create a new <see cref="DescriptorHeap"/> representing a depth stencil view using a <see cref="ID3D12Device"/>
        /// </summary>
        /// <param name="device">The device to use during creation</param>
        /// <param name="shaderVisible">Whether the depth stencil view must be visible to shaders</param>
        public static DescriptorHeap CreateDepthStencilViewHeap(
            ID3D12Device* device,
            bool shaderVisible = false
        )
        {
            var desc = CreateDesc(
                D3D12_DESCRIPTOR_HEAP_TYPE.D3D12_DESCRIPTOR_HEAP_TYPE_DSV,
                1,
                shaderVisible
            );

            return new DescriptorHeap(device, desc);
        }


        private DescriptorHeap(ID3D12Device* device, D3D12_DESCRIPTOR_HEAP_DESC desc)
        {
            ComPtr<ID3D12DescriptorHeap> heap = default;
            Guard.ThrowIfFailed(device->CreateDescriptorHeap(&desc, heap.Guid, (void**)&heap));

            _heap = heap.Move();
            var cpu = _heap.Get()->GetCPUDescriptorHandleForHeapStart();
            var gpu = _heap.Get()->GetGPUDescriptorHandleForHeapStart();

            FirstDescriptor = new DescriptorHandle(cpu, gpu, desc.Type);

            Type = desc.Type;
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
}
