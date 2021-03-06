using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Voltium.Core.Memory;
using Voltium.Core.NativeApi;
using Voltium.Core.Pipeline;
using Voltium.Core.Queries;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core.Devices
{
    public readonly struct DeviceInfo
    {
        public bool IsUma { get; init; }
        public bool IsCacheCoherent { get; init; }

        public bool IsCacheCoherentUma => IsCacheCoherent && IsUma;

        public bool MergedHeapSupport { get; init; }
        public ulong VirtualAddressRange { get; init; }
    }
    public enum WaitMode
    {
        WaitForAll,
        WaitForAny
    }

    public interface INativeDevice : IDisposable
    {
        DeviceInfo Info { get; }

        (ulong Alignment, ulong Length) GetTextureAllocationInfo(in TextureDesc desc);


        public INativeQueue CreateQueue(DeviceContext context);

        public ulong GetCompletedValue(FenceHandle fence);
        public void Wait(ReadOnlySpan<FenceHandle> fences, ReadOnlySpan<ulong> values, WaitMode mode);
        public OSEvent GetEventForWait(ReadOnlySpan<FenceHandle> fences, ReadOnlySpan<ulong> values, WaitMode mode);


        FenceHandle CreateFence(ulong initialValue, FenceFlags flags = FenceFlags.None);
        void DisposeFence(FenceHandle fence);


        HeapHandle CreateHeap(ulong size, in HeapInfo info);
        void DisposeHeap(HeapHandle handle);


        unsafe void* Map(BufferHandle handle);
        void Unmap(BufferHandle handle);

        BufferHandle AllocateBuffer(in BufferDesc desc, MemoryAccess access);
        BufferHandle AllocateBuffer(in BufferDesc desc, HeapHandle heap, ulong offset);
        void DisposeBuffer(BufferHandle handle);

        TextureHandle AllocateTexture(in TextureDesc desc, ResourceState initial);
        TextureHandle AllocateTexture(in TextureDesc desc, ResourceState initial, HeapHandle heap, ulong offset);
        void DisposeTexture(TextureHandle handle);

        RaytracingAccelerationStructureHandle AllocateRaytracingAccelerationStructure(ulong length);
        RaytracingAccelerationStructureHandle AllocateRaytracingAccelerationStructure(ulong length, HeapHandle heap, ulong offset);
        void DisposeRaytracingAccelerationStructure(RaytracingAccelerationStructureHandle handle);

        QuerySetHandle CreateQuerySet(QuerySetType type, uint length);
        void DisposeQuerySet(QuerySetHandle handle);

        RootSignatureHandle CreateRootSignature(ReadOnlySpan<RootParameter> rootParams, ReadOnlySpan<StaticSampler> samplers, RootSignatureFlags flags);
        void DisposeRootSignature(RootSignatureHandle sig);

        PipelineHandle CreatePipeline(in RootSignatureHandle rootSignature, in ComputePipelineDesc desc);
        PipelineHandle CreatePipeline(in RootSignatureHandle rootSignature, in GraphicsPipelineDesc desc);
        //PipelineHandle CreatePipeline(in RaytracingPipelineDesc desc);
        PipelineHandle CreatePipeline(in RootSignatureHandle rootSignature, in MeshPipelineDesc desc);
        void DisposePipeline(PipelineHandle handle);


        IndirectCommandHandle CreateIndirectCommand(in RootSignature rootSig, ReadOnlySpan<IndirectArgument> arguments, uint byteStride);
        IndirectCommandHandle CreateIndirectCommand(in IndirectArgument arguments, uint byteStride);


        DescriptorSetHandle CreateDescriptorSet(DescriptorType type, uint count);
        void DisposeDescriptorSet(DescriptorSetHandle handle);

        void UpdateDescriptors(ViewSetHandle views, uint firstView, DescriptorSetHandle descriptors, uint firstDescriptor, uint count);
        void CopyDescriptors(DescriptorSetHandle source, uint firstSource, DescriptorSetHandle dest, uint firstDest, uint count);

        ViewSetHandle CreateViewSet(uint viewCount);
        void DisposeViewSet(ViewSetHandle handle);

        ViewHandle CreateView(ViewSetHandle viewHeap, uint index, BufferHandle handle);
        ViewHandle CreateView(ViewSetHandle viewHeap, uint index, BufferHandle handle, in BufferViewDesc desc);
        ViewHandle CreateView(ViewSetHandle viewHeap, uint index, TextureHandle handle);
        ViewHandle CreateView(ViewSetHandle viewHeap, uint index, TextureHandle handle, in TextureViewDesc desc);
        ViewHandle CreateView(ViewSetHandle viewHeap, uint index, RaytracingAccelerationStructureHandle handle);

    }

    public enum DescriptorType
    {
        ConstantBuffer,
        DynamicConstantBuffer,

        StructuredBuffer,
        WritableStructuredBuffer,

        DynamicStructuredBuffer,
        DynamicWritableStructuredBuffer,

        TypedBuffer,
        WritableTypedBuffer,

        Texture,
        WritableTexture,

        Sampler,
        RaytracingAccelerationStructure
    }

    public enum BufferViewUsage
    {

    }

    public struct BufferViewDesc
    {
        public DataFormat Format;
        public uint Offset;
        public uint Length;
        public bool IsRaw;
    }

    public struct TextureViewDesc
    {
        public DataFormat Format;
        public ShaderComponentMapping X, Y, Z, W;
        public uint FirstMip, MipCount;
        public uint FirstArrayOrDepthSlice, ArrayOrDepthSliceCount;
        public TextureAspect Aspect;
        public float MinSampleLevelOfDetail;
    }

    public enum TextureAspect
    {
        Color,
        Depth,
        Stencil
    }

    public enum ResourceType
    {
        Texture,
        RenderTargetOrDepthStencilTexture,
        Buffer
    }

    public enum Alignment : ulong
    {
        _4KB = 4 * 1024,
        _64KB = 64 * 1024,
        _4MB = 4 * 1024 * 1024
    }

    public struct HeapInfo
    {
        public Alignment Alignment;
        public MemoryAccess Access;
        public ResourceType Type;
    }
}
