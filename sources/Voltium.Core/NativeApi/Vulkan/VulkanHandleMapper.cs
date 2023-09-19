using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using MathSharp;
using Microsoft.Collections.Extensions;
using SixLabors.ImageSharp;
using TerraFX.Interop;
using static TerraFX.Interop.Vulkan;
using Voltium.Common;
using Voltium.Common.Pix;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.NativeApi;
using Voltium.Core.Pipeline;
using Voltium.Core.Queries;
using Vector = MathSharp.Vector;

namespace Voltium.Core.NativeApi.Vulkan
{
    internal unsafe struct VulkanView
    {
        public IVulkanResource* Resource;
        public ulong Length;
        public uint Height, Width, DepthOrArraySize;
        public DXGI_FORMAT Format;
        public Vulkan_CPU_DESCRIPTOR_HANDLE DepthStencil, RenderTarget, UnorderedAccess, ShaderResource, ConstantBuffer;
    }

    internal unsafe struct VulkanFence
    {
        public VkSemaphore Fence;
    }

    internal unsafe struct VulkanBuffer
    {
        public VkBuffer Buffer;
        public VkDeviceMemory Memory;
        public bool Uav;
        public bool DedicatedAllocation;
        public void* CpuAddress;
        public ulong GpuAddress;
        public ulong Length;
    }

    internal unsafe struct VulkanTexture
    {
        public VkImage Texture;
        public uint Width, Height, DepthOrArraySize;
        public DXGI_FORMAT Format;
        public VkImageUsageFlags Flags;
    }

    internal unsafe struct VulkanRaytracingAccelerationStructure
    {
        public VkAccelerationStructureKHR RaytracingAccelerationStructure;
        public ulong GpuAddress;
        public ulong Length;
    }

    internal unsafe struct VulkanQueryHeap
    {
        public VkQueryPool QueryHeap;
        public uint Length;
    }

    internal unsafe struct VulkanRenderPass
    {
    }

    internal unsafe struct VulkanPipelineState
    {
        public VkPipeline PipelineState;
        public VkPipelineLayout RootSignature;
        public D3D_PRIMITIVE_TOPOLOGY Topology;
        public BindPoint BindPoint;
        public bool IsRaytracing;
        public ImmutableArray<RootParameter> RootParameters;
    }

    internal unsafe struct VulkanHeap
    {
        public VkDeviceMemory Heap;
        public Vulkan_HEAP_PROPERTIES Properties;
        public Vulkan_HEAP_FLAGS Flags;
        public ulong Alignment;
        public ulong Length;
    }


    internal unsafe struct VulkanLocalRootSignature
    {
        public VkPipelineLayout RootSignature;
        public ImmutableArray<RootParameter> RootParameters;
    }

    internal unsafe struct VulkanRootSignature
    {
        public VkPipelineLayout RootSignature;
        public ImmutableArray<RootParameter> RootParameters;
    }

    internal unsafe struct VulkanIndirectCommand
    {
        public IndirectArgument[] IndirectArguments;
        public VkPipelineLayout RootSignature;
        public uint ByteStride;
    }



    internal unsafe struct VulkanViewSet
    {
        public uint Length;
    }

    internal unsafe struct VulkanDynamicBufferDescriptor
    {
        public ulong GpuAddress;
    }

    internal unsafe struct VulkanDynamicRaytracingAccelerationStructureDescriptor
    {
        public ulong GpuAddress;
    }

    internal unsafe struct VulkanHandleMapper
    {
        public VulkanHandleMapper(bool _)
        {
            const int capacity = 32;
            _dynamicRaytracingAccelerationStructureDescriptors = new(capacity);
            _dynamicBufferDescriptors = new(capacity);
            _buffers = new(capacity);
            _textures = new(capacity);
            _accelarationStructures = new(capacity);
            _queryHeaps = new(capacity);
            _renderPasses = new(capacity);
            _pipelines = new(capacity);
            _viewSets = new(capacity);
            _views = new(capacity);
            _heaps = new(capacity);
            _indirectCommands = new(capacity);
            _signatures = new(capacity);
            _localSignatures = new(capacity);
            _fences = new(capacity);
        }

        private GenerationHandleAllocator<DynamicBufferDescriptorHandle, VulkanDynamicBufferDescriptor> _dynamicBufferDescriptors;
        private GenerationHandleAllocator<DynamicRaytracingAccelerationStructureDescriptorHandle, VulkanDynamicRaytracingAccelerationStructureDescriptor> _dynamicRaytracingAccelerationStructureDescriptors;
        private GenerationHandleAllocator<BufferHandle, VulkanBuffer> _buffers;
        private GenerationHandleAllocator<TextureHandle, VulkanTexture> _textures;
        private GenerationHandleAllocator<RaytracingAccelerationStructureHandle, VulkanRaytracingAccelerationStructure> _accelarationStructures;
        private GenerationHandleAllocator<QuerySetHandle, VulkanQueryHeap> _queryHeaps;
        private GenerationHandleAllocator<RenderPassHandle, VulkanRenderPass> _renderPasses;
        private GenerationHandleAllocator<RootSignatureHandle, VulkanRootSignature> _signatures;
        private GenerationHandleAllocator<LocalRootSignatureHandle, VulkanLocalRootSignature> _localSignatures;
        private GenerationHandleAllocator<PipelineHandle, VulkanPipelineState> _pipelines;
        private GenerationHandleAllocator<ViewSetHandle, VulkanViewSet> _viewSets;
        private GenerationHandleAllocator<ViewHandle, VulkanView> _views;
        private GenerationHandleAllocator<HeapHandle, VulkanHeap> _heaps;
        private GenerationHandleAllocator<IndirectCommandHandle, VulkanIndirectCommand> _indirectCommands;
        private GenerationHandleAllocator<FenceHandle, VulkanFence> _fences;

        public VulkanDynamicRaytracingAccelerationStructureDescriptor GetAndFree(DynamicRaytracingAccelerationStructureDescriptorHandle handle) => _dynamicRaytracingAccelerationStructureDescriptors.GetAndFreeHandle(handle);
        public VulkanBuffer GetAndFree(BufferHandle handle) => _buffers.GetAndFreeHandle(handle);
        public VulkanTexture GetAndFree(TextureHandle handle) => _textures.GetAndFreeHandle(handle);
        public VulkanRaytracingAccelerationStructure GetAndFree(RaytracingAccelerationStructureHandle handle) => _accelarationStructures.GetAndFreeHandle(handle);
        public VulkanQueryHeap GetAndFree(QuerySetHandle handle) => _queryHeaps.GetAndFreeHandle(handle);
        public VulkanRenderPass GetAndFree(RenderPassHandle handle) => _renderPasses.GetAndFreeHandle(handle);
        public VulkanRootSignature GetAndFree(RootSignatureHandle handle) => _signatures.GetAndFreeHandle(handle);
        public VulkanLocalRootSignature GetAndFree(LocalRootSignatureHandle handle) => _localSignatures.GetAndFreeHandle(handle);
        public VulkanPipelineState GetAndFree(PipelineHandle handle) => _pipelines.GetAndFreeHandle(handle);
        public VulkanView GetAndFree(ViewHandle handle) => _views.GetAndFreeHandle(handle);
        public VulkanViewSet GetAndFree(ViewSetHandle handle) => _viewSets.GetAndFreeHandle(handle);
        public VulkanHeap GetAndFree(HeapHandle handle) => _heaps.GetAndFreeHandle(handle);
        public VulkanIndirectCommand GetAndFree(IndirectCommandHandle handle) => _indirectCommands.GetAndFreeHandle(handle);
        public VulkanFence GetAndFree(FenceHandle handle) => _fences.GetAndFreeHandle(handle);
        public VulkanDynamicBufferDescriptor GetAndFree(DynamicBufferDescriptorHandle handle) => _dynamicBufferDescriptors.GetAndFreeHandle(handle);

        public DynamicRaytracingAccelerationStructureDescriptorHandle Create(in VulkanDynamicRaytracingAccelerationStructureDescriptor data) => _dynamicRaytracingAccelerationStructureDescriptors.AllocateHandle(data);
        public DynamicBufferDescriptorHandle Create(in VulkanDynamicBufferDescriptor data) => _dynamicBufferDescriptors.AllocateHandle(data);
        public FenceHandle Create(in VulkanFence data) => _fences.AllocateHandle(data);
        public ViewSetHandle Create(in VulkanViewSet data) => _viewSets.AllocateHandle(data);
        public BufferHandle Create(in VulkanBuffer data) => _buffers.AllocateHandle(data);
        public TextureHandle Create(in VulkanTexture data) => _textures.AllocateHandle(data);
        public RaytracingAccelerationStructureHandle Create(in VulkanRaytracingAccelerationStructure data) => _accelarationStructures.AllocateHandle(data);
        public QuerySetHandle Create(in VulkanQueryHeap data) => _queryHeaps.AllocateHandle(data);
        public RenderPassHandle Create(in VulkanRenderPass data) => _renderPasses.AllocateHandle(data);
        public PipelineHandle Create(in VulkanPipelineState data) => _pipelines.AllocateHandle(data);
        public ViewHandle Create(in VulkanView data) => _views.AllocateHandle(data);
        public HeapHandle Create(in VulkanHeap data) => _heaps.AllocateHandle(data);
        public IndirectCommandHandle Create(in VulkanIndirectCommand data) => _indirectCommands.AllocateHandle(data);
        public RootSignatureHandle Create(in VulkanRootSignature handle) => _signatures.AllocateHandle(handle);
        public LocalRootSignatureHandle Create(in VulkanLocalRootSignature handle) => _localSignatures.AllocateHandle(handle);

        public VulkanDynamicRaytracingAccelerationStructureDescriptor GetInfo(DynamicRaytracingAccelerationStructureDescriptorHandle handle) => _dynamicRaytracingAccelerationStructureDescriptors.GetHandleData(handle);
        public VulkanDynamicBufferDescriptor GetInfo(DynamicBufferDescriptorHandle handle) => _dynamicBufferDescriptors.GetHandleData(handle);
        public VulkanIndirectCommand GetInfo(IndirectCommandHandle handle) => _indirectCommands.GetHandleData(handle);
        public VulkanFence GetInfo(FenceHandle handle) => _fences.GetHandleData(handle);
        public VulkanView GetInfo(ViewHandle handle) => _views.GetHandleData(handle);
        public VulkanViewSet GetInfo(ViewSetHandle handle) => _viewSets.GetHandleData(handle);
        public VulkanQueryHeap GetInfo(QuerySetHandle handle) => _queryHeaps.GetHandleData(handle);
        public VulkanPipelineState GetInfo(PipelineHandle handle) => _pipelines.GetHandleData(handle);
        public VulkanBuffer GetInfo(BufferHandle handle) => _buffers.GetHandleData(handle);
        public VulkanTexture GetInfo(TextureHandle handle) => _textures.GetHandleData(handle);
        public VulkanRaytracingAccelerationStructure GetInfo(RaytracingAccelerationStructureHandle handle) => _accelarationStructures.GetHandleData(handle);
        public VulkanHeap GetInfo(HeapHandle handle) => _heaps.GetHandleData(handle);
        public VulkanRootSignature GetInfo(RootSignatureHandle handle) => _signatures.GetHandleData(handle);
        public VulkanLocalRootSignature GetInfo(LocalRootSignatureHandle handle) => _localSignatures.GetHandleData(handle);
    }
}
