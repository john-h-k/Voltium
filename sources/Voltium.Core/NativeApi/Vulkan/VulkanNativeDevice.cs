using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using static TerraFX.Interop.Vulkan;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;
using Voltium.Core.Queries;
using Voltium.Common;
using System.Runtime.InteropServices;
using Microsoft.Toolkit.HighPerformance.Buffers;
using System.Reflection;

namespace Voltium.Core.NativeApi.Vulkan
{
    public sealed unsafe class VulkanNativeDevice : INativeDevice
    {
        public DeviceInfo Info { get; }

        public VulkanNativeDevice()
        {
            uint extensionCount = 0;
            ThrowIfFailed(vkEnumerateInstanceExtensionProperties(null, &extensionCount, null));

            if (extensionCount > 0)
            {
                using var extensions = RentedArray<VkExtensionProperties>.Create((int)extensionCount);

                fixed (VkExtensionProperties* pExtensions = extensions.Value)
                {
                    if (vkEnumerateInstanceExtensionProperties(null, &extensionCount, pExtensions) == VkResult.VK_SUCCESS)
                    {
                        foreach (var extension in extensions.AsSpan())
                        {
                            _instanceExtensions.Add(new(extension.extensionName));
                        }
                    }
                }
            }

            using var applicationName = new AsciiNativeString(Assembly.GetEntryAssembly()?.FullName ?? "");
            using var engineName = new AsciiNativeString("Voltium");

            var appInfo = new VkApplicationInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_APPLICATION_INFO,
                apiVersion = VK_API_VERSION_1_2,
                pEngineName = engineName,
                pApplicationName = applicationName,
                applicationVersion = 0 // TODO
            };

            var instanceCreateInfo = new VkInstanceCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO,
                pApplicationInfo = &appInfo
            };
        }

        private static readonly string[] DefaultExtensions =
        {

        };

        private ValueList<string> _instanceExtensions = ValueList<string>.Create();

        private VkInstance _instance;
        private VkPhysicalDevice _physicalDevice;
        private VkDevice _device;
        private VulkanHandleMapper _mapper;

        private const VkBufferUsageFlags DefaultBufferUsage =
            VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_SRC_BIT |
            VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFER_DST_BIT |
            VkBufferUsageFlags.VK_BUFFER_USAGE_UNIFORM_TEXEL_BUFFER_BIT |
            // UAV - VkBufferUsageFlags.VK_BUFFER_USAGE_STORAGE_TEXEL_BUFFER_BIT |
            VkBufferUsageFlags.VK_BUFFER_USAGE_UNIFORM_BUFFER_BIT |
            // UAV - VkBufferUsageFlags.VK_BUFFER_USAGE_STORAGE_BUFFER_BIT |
            VkBufferUsageFlags.VK_BUFFER_USAGE_INDEX_BUFFER_BIT |
            VkBufferUsageFlags.VK_BUFFER_USAGE_VERTEX_BUFFER_BIT |
            VkBufferUsageFlags.VK_BUFFER_USAGE_INDIRECT_BUFFER_BIT |
            VkBufferUsageFlags.VK_BUFFER_USAGE_CONDITIONAL_RENDERING_BIT_EXT |
            VkBufferUsageFlags.VK_BUFFER_USAGE_SHADER_BINDING_TABLE_BIT_KHR |
            VkBufferUsageFlags.VK_BUFFER_USAGE_RAY_TRACING_BIT_NV |
            VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFORM_FEEDBACK_BUFFER_BIT_EXT |
            VkBufferUsageFlags.VK_BUFFER_USAGE_TRANSFORM_FEEDBACK_COUNTER_BUFFER_BIT_EXT |
            VkBufferUsageFlags.VK_BUFFER_USAGE_SHADER_DEVICE_ADDRESS_BIT |
            VkBufferUsageFlags.VK_BUFFER_USAGE_SHADER_DEVICE_ADDRESS_BIT_EXT |
            VkBufferUsageFlags.VK_BUFFER_USAGE_SHADER_DEVICE_ADDRESS_BIT_KHR |
            VkBufferUsageFlags.VK_BUFFER_USAGE_ACCELERATION_STRUCTURE_BUILD_INPUT_READ_ONLY_BIT_KHR;
        // AS - VkBufferUsageFlags.VK_BUFFER_USAGE_ACCELERATION_STRUCTURE_STORAGE_BIT_KHR;


        private const VkBufferUsageFlags UnorderedAccessBufferUsage = VkBufferUsageFlags.VK_BUFFER_USAGE_STORAGE_TEXEL_BUFFER_BIT | VkBufferUsageFlags.VK_BUFFER_USAGE_STORAGE_BUFFER_BIT;

        private VkAllocationCallbacks* AllocationCallbacks = null;


        internal ref VulkanHandleMapper GetMapperRef() => ref _mapper;
        internal VkDevice GetDevice() => _device;
        internal VulkanFence GetFence(FenceHandle fence) => _mapper.GetInfo(fence);

        public BufferHandle AllocateBuffer(in BufferDesc desc, MemoryAccess access)
        {
            var info = new VkBufferCreateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO,
                sharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE,
                size = desc.Length,
                usage = DefaultBufferUsage | (desc.ResourceFlags.HasFlag(ResourceFlags.AllowUnorderedAccess) ? UnorderedAccessBufferUsage : 0),
            };

            VkBuffer buffer;
            ThrowIfFailed(vkCreateBuffer(
                _device,
                &info,
                AllocationCallbacks,
                (ulong*)&buffer
            ));

            var dedicatedAlloc = new VkMemoryDedicatedAllocateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_DEDICATED_ALLOCATE_INFO,
                buffer = buffer
            };

            var memoryAlloc = new VkMemoryAllocateInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
                pNext = &dedicatedAlloc,
                allocationSize = desc.Length,
                memoryTypeIndex = GetMemoryTypeIndex(access)
            };

            VkDeviceMemory memory;
            ThrowIfFailed(vkAllocateMemory(
                _device,
                &memoryAlloc,
                AllocationCallbacks,
                (ulong*)&memory
            ));

            void* pData;

            ThrowIfFailed(vkMapMemory(
                _device,
                memory,
                0,
                VK_WHOLE_SIZE,
                0,
                &pData
            ));

            ThrowIfFailed(vkBindBufferMemory(
                _device,
                buffer,
                memory,
                0
            ));

            var getAddressInfo = new VkBufferDeviceAddressInfo
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_BUFFER_DEVICE_ADDRESS_INFO,
                buffer = buffer
            };

            return _mapper.Create(new VulkanBuffer
            {
                Buffer = buffer,
                Memory = memory,
                CpuAddress = pData,
                GpuAddress = vkGetBufferDeviceAddress(_device, &getAddressInfo),
                Length = desc.Length,
                Uav = desc.ResourceFlags.HasFlag(ResourceFlags.AllowUnorderedAccess)
            });
        }

        public BufferHandle AllocateBuffer(in BufferDesc desc, HeapHandle heap, ulong offset) => throw new NotImplementedException();
        public RaytracingAccelerationStructureHandle AllocateRaytracingAccelerationStructure(ulong length) => throw new NotImplementedException();
        public RaytracingAccelerationStructureHandle AllocateRaytracingAccelerationStructure(ulong length, HeapHandle heap, ulong offset) => throw new NotImplementedException();
        public TextureHandle AllocateTexture(in TextureDesc desc, ResourceState initial) => throw new NotImplementedException();
        public TextureHandle AllocateTexture(in TextureDesc desc, ResourceState initial, HeapHandle heap, ulong offset) => throw new NotImplementedException();
        public void CopyDescriptors(DescriptorSetHandle source, uint firstSource, DescriptorSetHandle dest, uint firstDest, uint count) => throw new NotImplementedException();
        public DescriptorSetHandle CreateDescriptorSet(DescriptorType type, uint count) => throw new NotImplementedException();
        public DynamicBufferDescriptorHandle CreateDynamicDescriptor(BufferHandle buffer) => throw new NotImplementedException();
        public DynamicRaytracingAccelerationStructureDescriptorHandle CreateDynamicDescriptor(RaytracingAccelerationStructureHandle buffer) => throw new NotImplementedException();
        public FenceHandle CreateFence(ulong initialValue, FenceFlags flags = FenceFlags.None) => throw new NotImplementedException();
        public HeapHandle CreateHeap(ulong size, in HeapInfo info) => throw new NotImplementedException();
        public IndirectCommandHandle CreateIndirectCommand(RootSignatureHandle rootSig, ReadOnlySpan<IndirectArgument> arguments, uint byteStride) => throw new NotImplementedException();
        public IndirectCommandHandle CreateIndirectCommand(in IndirectArgument arguments, uint byteStride) => throw new NotImplementedException();
        public LocalRootSignatureHandle CreateLocalRootSignature(ReadOnlySpan<RootParameter> rootParams, ReadOnlySpan<StaticSampler> samplers, RootSignatureFlags flags) => throw new NotImplementedException();
        public PipelineHandle CreatePipeline(in NativeComputePipelineDesc desc) => throw new NotImplementedException();
        public PipelineHandle CreatePipeline(in NativeGraphicsPipelineDesc desc) => throw new NotImplementedException();
        public PipelineHandle CreatePipeline(in NativeRaytracingPipelineDesc desc) => throw new NotImplementedException();
        public PipelineHandle CreatePipeline(in NativeMeshPipelineDesc desc) => throw new NotImplementedException();
        public QuerySetHandle CreateQuerySet(QuerySetType type, uint length) => throw new NotImplementedException();
        public INativeQueue CreateQueue(ExecutionEngine context) => throw new NotImplementedException();
        public RootSignatureHandle CreateRootSignature(ReadOnlySpan<RootParameter> rootParams, ReadOnlySpan<StaticSampler> samplers, RootSignatureFlags flags) => throw new NotImplementedException();
        public ViewHandle CreateView(ViewSetHandle viewHeap, uint index, BufferHandle handle) => throw new NotImplementedException();
        public ViewHandle CreateView(ViewSetHandle viewHeap, uint index, BufferHandle handle, in BufferViewDesc desc) => throw new NotImplementedException();
        public ViewHandle CreateView(ViewSetHandle viewHeap, uint index, TextureHandle handle) => throw new NotImplementedException();
        public ViewHandle CreateView(ViewSetHandle viewHeap, uint index, TextureHandle handle, in TextureViewDesc desc) => throw new NotImplementedException();
        public ViewHandle CreateView(ViewSetHandle viewHeap, uint index, RaytracingAccelerationStructureHandle handle) => throw new NotImplementedException();
        public ViewSetHandle CreateViewSet(uint viewCount) => throw new NotImplementedException();
        public void Dispose() => throw new NotImplementedException();
        public void DisposeBuffer(BufferHandle handle)
        {
            var info = _mapper.GetAndFree(handle);
            vkDestroyBuffer(_device, info.Buffer, AllocationCallbacks);

            if (info.DedicatedAllocation)
            {
                vkFreeMemory(_device, info.Memory, AllocationCallbacks);
            }
        }

        public void DisposeDescriptorSet(DescriptorSetHandle handle) => throw new NotImplementedException();
        public void DisposeDynamicDescriptor(DynamicBufferDescriptorHandle handle) => throw new NotImplementedException();
        public void DisposeDynamicDescriptor(DynamicRaytracingAccelerationStructureDescriptorHandle handle) => throw new NotImplementedException();
        public void DisposeFence(FenceHandle fence) => throw new NotImplementedException();
        public void DisposeHeap(HeapHandle handle) => throw new NotImplementedException();
        public void DisposeIndirectCommand(IndirectCommandHandle handle) => throw new NotImplementedException();
        public void DisposeLocalRootSignature(LocalRootSignatureHandle handle) => throw new NotImplementedException();
        public void DisposePipeline(PipelineHandle handle) => throw new NotImplementedException();
        public void DisposeQuerySet(QuerySetHandle handle) => throw new NotImplementedException();
        public void DisposeRaytracingAccelerationStructure(RaytracingAccelerationStructureHandle handle) => throw new NotImplementedException();
        public void DisposeRootSignature(RootSignatureHandle handle) => throw new NotImplementedException();
        public void DisposeTexture(TextureHandle handle) => throw new NotImplementedException();
        public void DisposeViewSet(ViewSetHandle handle) => throw new NotImplementedException();
        public (ulong DestSize, ulong ScratchSize, ulong UpdateSize) GetBottomLevelAccelerationStructureBuildInfo(ReadOnlySpan<GeometryDesc> geometry, BuildAccelerationStructureFlags flags) => throw new NotImplementedException();
        public ulong GetCompletedValue(FenceHandle fence) => throw new NotImplementedException();
        public ulong GetDeviceVirtualAddress(BufferHandle handle) => _mapper.GetInfo(handle).GpuAddress;
        public ulong GetDeviceVirtualAddress(RaytracingAccelerationStructureHandle handle) => throw new NotImplementedException();
        public OSEvent GetEventForWait(ReadOnlySpan<FenceHandle> fences, ReadOnlySpan<ulong> values, WaitMode mode) => throw new NotImplementedException();
        public void GetRaytracingShaderIdentifier(PipelineHandle raytracingPipeline, ReadOnlySpan<char> shaderName, Span<byte> identifier) => throw new NotImplementedException();
        public (ulong Alignment, ulong Length) GetTextureAllocationInfo(in TextureDesc desc) => throw new NotImplementedException();
        public (ulong DestSize, ulong ScratchSize, ulong UpdateSize) GetTopLevelAccelerationStructureBuildInfo(uint numInstances, BuildAccelerationStructureFlags flags) => throw new NotImplementedException();
        public unsafe void* Map(BufferHandle handle) => _mapper.GetInfo(handle).CpuAddress;
        public void Unmap(BufferHandle handle) { }
        public void UpdateDescriptors(ViewSetHandle views, uint firstView, DescriptorSetHandle descriptors, uint firstDescriptor, uint count) => throw new NotImplementedException();
        public void Wait(ReadOnlySpan<FenceHandle> fences, ReadOnlySpan<ulong> values, WaitMode mode) => throw new NotImplementedException();

        private uint GetMemoryTypeIndex(MemoryAccess access) => throw new NotImplementedException();


        internal void ThrowIfFailed(VkResult result)
        {
            if (result >= 0)
            {
                return;
            }

            Throw(result);

            static void Throw(VkResult result) => throw new ExternalException();
        }
    }
}
