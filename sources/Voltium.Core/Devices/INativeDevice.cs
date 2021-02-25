using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;
using Voltium.Core.Queries;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core.Devices
{
    public struct DeviceInfo { }

    public interface INativeDevice : IDisposable
    {
        public DeviceInfo Info { get; }

        public (ulong Alignment, ulong Length) GetTextureAllocationInfo(in TextureDesc desc);


        public HeapHandle CreateHeap(ulong size, in HeapInfo info);
        public void DisposeHeap(HeapHandle handle);


        public BufferHandle AllocateBuffer(in BufferDesc desc, MemoryAccess access);
        public BufferHandle AllocateBuffer(in BufferDesc desc, HeapHandle heap, ulong offset);
        public void DisposeBuffer(BufferHandle handle);

        public TextureHandle AllocateTexture(in TextureDesc desc, ResourceState initial);
        public TextureHandle AllocateTexture(in TextureDesc desc, ResourceState initial, HeapHandle heap, ulong offset);
        public void DisposeTexture(TextureHandle handle);

        public RaytracingAccelerationStructureHandle AllocateRaytracingAccelerationStructure(ulong length);
        public RaytracingAccelerationStructureHandle AllocateRaytracingAccelerationStructure(ulong length, HeapHandle heap, ulong offset);
        public void DisposeRaytracingAccelerationStructure(RaytracingAccelerationStructureHandle handle);

        public QuerySetHandle CreateQuerySet(QuerySetType type, uint length);
        public void CreateQuerySet(QuerySetHandle handle);

        public RootSignatureHandle CreateRootSignature(ReadOnlySpan<RootParameter> rootParams, ReadOnlySpan<StaticSampler> samplers, RootSignatureFlags flags);
        public void DisposeRootSignature(RootSignatureHandle sig);

        public PipelineHandle CreatePipeline(in ComputePipelineDesc desc);
        public PipelineHandle CreatePipeline(in GraphicsPipelineDesc desc);
        public PipelineHandle CreatePipeline(in RaytracingPipelineDesc desc);
        public PipelineHandle CreatePipeline(in MeshPipelineDesc desc);
        public void DisposePipeline(PipelineHandle handle);


        public IndirectCommandHandle CreateIndirectCommand(RootSignature rootSig, ReadOnlySpan<IndirectArgument> arguments, uint byteStride);
        public IndirectCommandHandle CreateIndirectCommand(IndirectArgument arguments, uint byteStride);


        public DescriptorSetHandle CreateDescriptorSet(DescriptorType type, uint count);
        public void DisposeDescriptorSet(DescriptorSetHandle handle);
        
        public void UpdateDescriptors(ViewSetHandle views, uint firstView, DescriptorSetHandle descriptors, uint firstDescriptor, uint count);
        public void CopyDescriptors(DescriptorSetHandle source, uint firstSource, DescriptorSetHandle dest, uint firstDest, uint count);

        public ViewSetHandle CreateViewSet(uint viewCount);
        public void DisposeViewSet(ViewSetHandle handle);

        public ViewHandle CreateView(ViewSetHandle viewHeap, uint index, BufferHandle handle);
        public ViewHandle CreateView(ViewSetHandle viewHeap, uint index, BufferHandle handle, in BufferViewDesc desc);
        public ViewHandle CreateView(ViewSetHandle viewHeap, uint index, TextureHandle handle);
        public ViewHandle CreateView(ViewSetHandle viewHeap, uint index, TextureHandle handle, in TextureViewDesc desc);
        public ViewHandle CreateView(ViewSetHandle viewHeap, uint index, RaytracingAccelerationStructureHandle handle);

        public GpuTask Execute(ReadOnlySpan<ReadOnlyMemory<byte>> cmds);
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
