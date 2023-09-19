
// namespace Voltium.Core.Devices;

// public sealed class RemoteNativeDevice : INativeDevice
// {
//         DeviceInfo Info { get; }
//         (ulong DestSize, ulong ScratchSize, ulong UpdateSize) GetBottomLevelAccelerationStructureBuildInfo(ReadOnlySpan<GeometryDesc> geometry, BuildAccelerationStructureFlags flags);
//         (ulong DestSize, ulong ScratchSize, ulong UpdateSize) GetTopLevelAccelerationStructureBuildInfo(uint numInstances, BuildAccelerationStructureFlags flags);
//         void GetRaytracingShaderIdentifier(PipelineHandle raytracingPipeline, ReadOnlySpan<char> shaderName, Span<byte> identifier); 
//         (ulong Alignment, ulong Length) GetTextureAllocationInfo(in TextureDesc desc);
//         public INativeQueue CreateQueue(ExecutionEngine context);
//         public ulong GetCompletedValue(FenceHandle fence);
//         public void Wait(ReadOnlySpan<FenceHandle> fences, ReadOnlySpan<ulong> values, WaitMode mode);
//         public OSEvent GetEventForWait(ReadOnlySpan<FenceHandle> fences, ReadOnlySpan<ulong> values, WaitMode mode);
//         FenceHandle CreateFence(ulong initialValue, FenceFlags flags = FenceFlags.None);
//         void DisposeFence(FenceHandle fence);
//         HeapHandle CreateHeap(ulong size, in HeapInfo info);
//         void DisposeHeap(HeapHandle handle);
//         unsafe void* Map(BufferHandle handle);
//         void Unmap(BufferHandle handle);
//         ulong GetDeviceVirtualAddress(BufferHandle handle);
//         ulong GetDeviceVirtualAddress(RaytracingAccelerationStructureHandle handle);
//         BufferHandle AllocateBuffer(in BufferDesc desc, MemoryAccess access);
//         BufferHandle AllocateBuffer(in BufferDesc desc, HeapHandle heap, ulong offset);
//         void DisposeBuffer(BufferHandle handle);
//         TextureHandle AllocateTexture(in TextureDesc desc, ResourceState initial);
//         TextureHandle AllocateTexture(in TextureDesc desc, ResourceState initial, HeapHandle heap, ulong offset);
//         void DisposeTexture(TextureHandle handle);
//         RaytracingAccelerationStructureHandle AllocateRaytracingAccelerationStructure(ulong length);
//         RaytracingAccelerationStructureHandle AllocateRaytracingAccelerationStructure(ulong length, HeapHandle heap, ulong offset);
//         void DisposeRaytracingAccelerationStructure(RaytracingAccelerationStructureHandle handle);
//         QuerySetHandle CreateQuerySet(QuerySetType type, uint length);
//         void DisposeQuerySet(QuerySetHandle handle);
//         RootSignatureHandle CreateRootSignature(ReadOnlySpan<RootParameter> rootParams, ReadOnlySpan<StaticSampler> samplers, RootSignatureFlags flags);
//         LocalRootSignatureHandle CreateLocalRootSignature(ReadOnlySpan<RootParameter> rootParams, ReadOnlySpan<StaticSampler> samplers, RootSignatureFlags flags);
//         void DisposeRootSignature(RootSignatureHandle handle);
//         void DisposeLocalRootSignature(LocalRootSignatureHandle handle);
//         PipelineHandle CreatePipeline(in NativeComputePipelineDesc desc);
//         PipelineHandle CreatePipeline(in NativeGraphicsPipelineDesc desc);

//         PipelineHandle CreatePipeline(in NativeRaytracingPipelineDesc desc);
//         PipelineHandle CreatePipeline(in NativeMeshPipelineDesc desc);
//         void DisposePipeline(PipelineHandle handle);
//         IndirectCommandHandle CreateIndirectCommand(RootSignatureHandle rootSig, ReadOnlySpan<IndirectArgument> arguments, uint byteStride);
//         IndirectCommandHandle CreateIndirectCommand(in IndirectArgument arguments, uint byteStride);
//         void DisposeIndirectCommand(IndirectCommandHandle handle);
//         DynamicBufferDescriptorHandle CreateDynamicDescriptor(BufferHandle buffer);
//         void DisposeDynamicDescriptor(DynamicBufferDescriptorHandle handle);
//         DynamicRaytracingAccelerationStructureDescriptorHandle CreateDynamicDescriptor(RaytracingAccelerationStructureHandle buffer);
//         void DisposeDynamicDescriptor(DynamicRaytracingAccelerationStructureDescriptorHandle handle);
//         DescriptorSetHandle CreateDescriptorSet(DescriptorType type, uint count);
//         void DisposeDescriptorSet(DescriptorSetHandle handle);
//         void UpdateDescriptors(ViewSetHandle views, uint firstView, DescriptorSetHandle descriptors, uint firstDescriptor, uint count);
//         void CopyDescriptors(DescriptorSetHandle source, uint firstSource, DescriptorSetHandle dest, uint firstDest, uint count);
//         ViewSetHandle CreateViewSet(uint viewCount);
//         void DisposeViewSet(ViewSetHandle handle);
//         ViewHandle CreateView(ViewSetHandle viewHeap, uint index, BufferHandle handle);
//         ViewHandle CreateView(ViewSetHandle viewHeap, uint index, BufferHandle handle, in BufferViewDesc desc);
//         ViewHandle CreateView(ViewSetHandle viewHeap, uint index, TextureHandle handle);
//         ViewHandle CreateView(ViewSetHandle viewHeap, uint index, TextureHandle handle, in TextureViewDesc desc);
//         ViewHandle CreateView(ViewSetHandle viewHeap, uint index, RaytracingAccelerationStructureHandle handle);

// }