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
using Voltium.Core.Raytracing;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core.Devices
{
    /// <summary>
    /// Information about a <see cref="INativeDevice"/>
    /// </summary>
    public readonly struct DeviceInfo
    {
        /// <summary>
        /// Whether this device is a UMA (unified-memory architecture) device
        /// </summary>
        public bool IsUma { get; init; }

        /// <summary>
        /// Whether this device is cache coherent with the CPU
        /// </summary>
        public bool IsCacheCoherent { get; init; }

        /// <summary>
        /// Whether this device <see cref="IsUma"/> and <see cref="IsCacheCoherent"/>
        /// </summary>
        public bool IsCacheCoherentUma => IsCacheCoherent && IsUma;

        /// <summary>
        /// Whether this device supports a merged-resource heap
        /// </summary>
        public bool MergedHeapSupport { get; init; }

        /// <summary>
        /// The size, in bytes, of this device's virtual address range
        /// </summary>
        public ulong VirtualAddressRange { get; init; }

        /// <summary>
        /// The size, in bytes, that raytracing shader identifiers user
        /// </summary>
        public uint RaytracingShaderIdentifierSize { get; init; }
    }


    /// <summary>
    /// The wait mode for a fence wait
    /// </summary>
    public enum WaitMode
    {
        /// <summary>
        /// Wait for all fences to complete
        /// </summary>
        WaitForAll,

        /// <summary>
        /// Wait for any fence to complete
        /// </summary>
        WaitForAny
    }

    /// <summary>
    /// The native device type used to interface with a GPU
    /// </summary>
    public interface INativeDevice : IDisposable
    {
        /// <summary>
        /// <see cref="DeviceInfo"/> about this device
        /// </summary>
        DeviceInfo Info { get; }

        /// <summary>
        /// Calculates memory size information about a bottom-level raytracing acceleration structure build and potentially its updates
        /// </summary>
        /// <param name="geometry">The <see cref="GeometryDesc"/>s in the acceleration structures</param>
        /// <param name="flags">The <see cref="BuildAccelerationStructureFlags"/> for the build</param>
        /// <returns>The size, in bytes, for the destination acceleration structure, the intermediate scratch buffer, and (if applicable) the intermediate scratch buffer used for updates</returns>
        (ulong DestSize, ulong ScratchSize, ulong UpdateSize) GetBottomLevelAccelerationStructureBuildInfo(ReadOnlySpan<GeometryDesc> geometry, BuildAccelerationStructureFlags flags);

        /// <summary>
        /// Calculates memory size information about a top-level raytracing acceleration structure build and potentially its updates
        /// </summary>
        /// <param name="numInstances">The number of <see cref="GeometryInstance"/>s in the acceleration structures</param>
        /// <param name="flags">The <see cref="BuildAccelerationStructureFlags"/> for the build</param>
        /// <returns>The size, in bytes, for the destination acceleration structure, the intermediate scratch buffer, and (if applicable) the intermediate scratch buffer used for updates</returns>
        (ulong DestSize, ulong ScratchSize, ulong UpdateSize) GetTopLevelAccelerationStructureBuildInfo(uint numInstances, BuildAccelerationStructureFlags flags);


        /// <summary>
        /// Retrieves the shader identifier for a raytracing shader
        /// </summary>
        /// <param name="raytracingPipeline">The <see cref="PipelineHandle"/> to the pipeline that contains the shader</param>
        /// <param name="shaderName">The name of the shader to retrieve</param>
        /// <param name="identifier">The memory to write the identifier to. Must be at least as large as <see cref="DeviceInfo.RaytracingShaderIdentifierSize"/></param>
        void GetRaytracingShaderIdentifier(PipelineHandle raytracingPipeline, ReadOnlySpan<char> shaderName, Span<byte> identifier); 

        /// <summary>
        /// Calculate memory size and alignment information about a texture
        /// </summary>
        /// <param name="desc">The <see cref="TextureDesc"/> to determine information about</param>
        /// <returns>A <see cref="ulong"/> determining the required alignment (in bytes) of the texture, and a <see cref="ulong"/> determing the size (in byte) of the texture</returns>
        (ulong Alignment, ulong Length) GetTextureAllocationInfo(in TextureDesc desc);

        /// <summary>
        /// Creates a new <see cref="INativeQueue"/> for a given <see cref="ExecutionEngine"/>
        /// </summary>
        /// <param name="context">The <see cref="ExecutionEngine"/> this queue should map to</param>
        /// <returns>A new <see cref="INativeQueue"/></returns>
        public INativeQueue CreateQueue(ExecutionEngine context);

        /// <summary>
        /// Returns the highest reached value of a given <see cref="FenceHandle"/>
        /// </summary>
        /// <param name="fence">The <see cref="FenceHandle"/> to determine the reached value for</param>
        /// <returns>A <see cref="ulong"/> indicating the last </returns>
        public ulong GetCompletedValue(FenceHandle fence);

        /// <summary>
        /// Wait synchronously for a set of <see cref="FenceHandle"/>s to reach a set of values
        /// </summary>
        /// <param name="fences">The set of <see cref="FenceHandle"/>s to wait for. Each fence is paired with the equivalent value in <paramref name="values"/></param>
        /// <param name="values">The set of values indicating the values fences need to be waited for</param>
        /// <param name="mode">The <see cref="WaitMode"/> determining whether the method should wait for all or any of the fences to complete</param>
        public void Wait(ReadOnlySpan<FenceHandle> fences, ReadOnlySpan<ulong> values, WaitMode mode);

        /// <summary>
        /// Return a <see cref="OSEvent"/> which is an OS-level event indicating when a set of <see cref="FenceHandle"/>s reach a set of values
        /// </summary>
        /// <param name="fences">The set of <see cref="FenceHandle"/>s to wait for. Each fence is paired with the equivalent value in <paramref name="values"/></param>
        /// <param name="values">The set of values indicating the values fences need to be waited for</param>
        /// <param name="mode">The <see cref="WaitMode"/> determining whether the method should wait for all or any of the fences to complete</param>
        public OSEvent GetEventForWait(ReadOnlySpan<FenceHandle> fences, ReadOnlySpan<ulong> values, WaitMode mode);

        /// <summary>
        /// Create a new fence object, used for CPU and GPU synchronization as well as cross-GPU synchronization
        /// </summary>
        /// <param name="initialValue">The starting value for the fence</param>
        /// <param name="flags">A combination of <see cref="FenceFlags"/> to control creation of the fence</param>
        /// <returns>A new <see cref="FenceHandle"/></returns>
        FenceHandle CreateFence(ulong initialValue, FenceFlags flags = FenceFlags.None);

        /// <summary>
        /// Destroys a given <see cref="FenceHandle"/>
        /// </summary>
        /// <param name="fence">The <see cref="FenceHandle"/> to destroy</param>
        void DisposeFence(FenceHandle fence);

        /// <summary>
        /// Creates a new heap object, representing a contigous region of GPU physical memory
        /// </summary>
        /// <param name="size">The size (in bytes) to allocate</param>
        /// <param name="info">The <see cref="HeapInfo"/> deciding information about the heap to create</param>
        /// <returns> new <see cref="HeapHandle"/></returns>
        HeapHandle CreateHeap(ulong size, in HeapInfo info);


        /// <summary>
        /// Destroys a given heap
        /// </summary>
        /// <param name="handle">The heap to destroy</param>
        void DisposeHeap(HeapHandle handle);

        /// <summary>
        /// Maps a buffer, making it accessible to the CPU via a pointer
        /// </summary>
        /// <param name="handle">The buffer to map</param>
        /// <returns></returns>
        unsafe void* Map(BufferHandle handle);

        /// <summary>
        /// Unmaps a buffer
        /// </summary>
        /// <param name="handle">The buffer to unmap</param>
        void Unmap(BufferHandle handle);

        /// <summary>
        /// Returns the GPU virtual address of the buffer
        /// </summary>
        /// <param name="handle">The<see cref="BufferHandle"/> to retrieve the GPU virtual address of</param>
        /// <returns>A GPU virtual address</returns>
        ulong GetDeviceVirtualAddress(BufferHandle handle);

        /// <summary>
        /// Returns the GPU virtual address of the buffer
        /// </summary>
        /// <param name="handle">The<see cref="RaytracingAccelerationStructureHandle"/> to retrieve the GPU virtual address of</param>
        /// <returns>A GPU virtual address</returns>
        ulong GetDeviceVirtualAddress(RaytracingAccelerationStructureHandle handle);

        /// <summary>
        /// Allocates a new buffer, using a driver dedicated allocation
        /// </summary>
        /// <param name="desc">The <see cref="BufferDesc"/> describing the buffer to allocate</param>
        /// <param name="access">The <see cref="MemoryAccess"/> describing how the CPU can access the buffer</param>
        /// <returns>A new <see cref="BufferHandle"/></returns>
        BufferHandle AllocateBuffer(in BufferDesc desc, MemoryAccess access);

        /// <summary>
        /// Allocates a new buffer from a heap
        /// </summary>
        /// <param name="desc">The <see cref="BufferDesc"/> describing the buffer to allocate</param>
        /// <param name="heap">The heap to allocate the buffer within</param>
        /// <param name="offset">The offset (in bytes) within <paramref name="heap"/> to allocate the buffer at</param>
        /// <returns>A new <see cref="BufferHandle"/></returns>
        BufferHandle AllocateBuffer(in BufferDesc desc, HeapHandle heap, ulong offset);

        /// <summary>
        /// Destroys a given <see cref="BufferHandle"/>
        /// </summary>
        /// <param name="handle">The <see cref="BufferHandle"/> to destroy</param>
        void DisposeBuffer(BufferHandle handle);

        /// <summary>
        /// Allocates a new texture, using a driver dedicated allocation
        /// </summary>
        /// <param name="desc">The <see cref="TextureDesc"/> describing the buffer to allocate</param>
        /// <param name="initial">The <see cref="ResourceState"/> indicating the initial state of the texture</param>
        /// <returns>A new <see cref="TextureHandle"/></returns>
        TextureHandle AllocateTexture(in TextureDesc desc, ResourceState initial);

        /// <summary>
        /// Allocates a new texture, using a driver dedicated allocation
        /// </summary>
        /// <param name="desc">The <see cref="TextureDesc"/> describing the texture to allocate</param>
        /// <param name="initial">The <see cref="ResourceState"/> indicating the initial state of the texture</param>
        /// <param name="heap">The heap to allocate the texture within</param>
        /// <param name="offset">The offset (in bytes) within <paramref name="heap"/> to allocate the texture at</param>
        /// <returns>A new <see cref="TextureHandle"/></returns>
        TextureHandle AllocateTexture(in TextureDesc desc, ResourceState initial, HeapHandle heap, ulong offset);

        /// <summary>
        /// Destroys a given <see cref="TextureHandle"/>
        /// </summary>
        /// <param name="handle">The <see cref="TextureHandle"/> to destroy</param>
        void DisposeTexture(TextureHandle handle);

        /// <summary>
        /// Allocates a new raytracing acceleration structure from a heap
        /// </summary>
        /// <param name="length">The length, in bytes, of the raytracing acceleration structure</param>
        /// <returns>A new <see cref="RaytracingAccelerationStructureHandle"/></returns>
        RaytracingAccelerationStructureHandle AllocateRaytracingAccelerationStructure(ulong length);

        /// <summary>
        /// Allocates a new raytracing acceleration structure, using a driver dedication allocation
        /// </summary>
        /// <param name="length">The length, in bytes, of the raytracing acceleration structure</param>
        /// <param name="heap">The heap to allocate the raytracing acceleration structure within</param>
        /// <param name="offset">The offset (in bytes) within <paramref name="heap"/> to allocate the raytracing acceleration structure at</param>
        /// <returns>A new <see cref="RaytracingAccelerationStructureHandle"/></returns>
        RaytracingAccelerationStructureHandle AllocateRaytracingAccelerationStructure(ulong length, HeapHandle heap, ulong offset);

        /// <summary>
        /// Destroys a given <see cref="RaytracingAccelerationStructureHandle"/>
        /// </summary>
        /// <param name="handle">The <see cref="RaytracingAccelerationStructureHandle"/> to destroy</param>
        void DisposeRaytracingAccelerationStructure(RaytracingAccelerationStructureHandle handle);

        /// <summary>
        /// Creates a new query set
        /// </summary>
        /// <param name="type">The <see cref="QuerySetType"/> for this query set</param>
        /// <param name="length">The length, in queries, for this query set</param>
        /// <returns>A new <see cref="QuerySetHandle"/></returns>
        QuerySetHandle CreateQuerySet(QuerySetType type, uint length);

        /// <summary>
        /// Destroys a given <see cref="QuerySetHandle"/>
        /// </summary>
        /// <param name="handle">The <see cref="QuerySetHandle"/> to destroy</param>
        void DisposeQuerySet(QuerySetHandle handle);

        /// <summary>
        /// Creates a new root signature
        /// </summary>
        /// <param name="rootParams">The parameters for this root signature</param>
        /// <param name="samplers">The static sampler to embed this into the root signature</param>
        /// <param name="flags">The <see cref="RootSignatureFlags"/> for this root signature</param>
        /// <returns>A new <see cref="RootSignatureHandle"/></returns>
        RootSignatureHandle CreateRootSignature(ReadOnlySpan<RootParameter> rootParams, ReadOnlySpan<StaticSampler> samplers, RootSignatureFlags flags);

        /// <summary>
        /// Creates a new local root signature
        /// </summary>
        /// <param name="rootParams">The parameters for this root signature</param>
        /// <param name="samplers">The static sampler to embed this into the root signature</param>
        /// <param name="flags">The <see cref="RootSignatureFlags"/> for this root signature</param>
        /// <returns>A new <see cref="RootSignatureHandle"/></returns>
        LocalRootSignatureHandle CreateLocalRootSignature(ReadOnlySpan<RootParameter> rootParams, ReadOnlySpan<StaticSampler> samplers, RootSignatureFlags flags);

        /// <summary>
        /// Destroys a given <see cref="RootSignatureHandle"/>
        /// </summary>
        /// <param name="handle">The <see cref="RootSignatureHandle"/> to destroy</param>
        void DisposeRootSignature(RootSignatureHandle handle);

        /// <summary>
        /// Destroys a given <see cref="LocalRootSignatureHandle"/>
        /// </summary>
        /// <param name="handle">The <see cref="LocalRootSignatureHandle"/> to destroy</param>
        void DisposeLocalRootSignature(LocalRootSignatureHandle handle);

        /// <summary>
        /// Creates a new <see cref="PipelineHandle"/> for a compute pipeline
        /// </summary>
        /// <param name="desc">The <see cref="NativeComputePipelineDesc"/> for this pipeline</param>
        /// <returns>A new <see cref="PipelineHandle"/></returns>
        PipelineHandle CreatePipeline(in NativeComputePipelineDesc desc);

        /// <summary>
        /// Creates a new <see cref="PipelineHandle"/> for a graphics pipeline
        /// </summary>
        /// <param name="desc">The <see cref="NativeGraphicsPipelineDesc"/> for this pipeline</param>
        /// <returns>A new <see cref="PipelineHandle"/></returns>
        PipelineHandle CreatePipeline(in NativeGraphicsPipelineDesc desc);

        PipelineHandle CreatePipeline(in NativeRaytracingPipelineDesc desc);

        /// <summary>
        /// Creates a new <see cref="PipelineHandle"/> for a mesh pipeline
        /// </summary>
        /// <param name="desc">The <see cref="NativeMeshPipelineDesc"/> for this pipeline</param>
        /// <returns>A new <see cref="PipelineHandle"/></returns>
        PipelineHandle CreatePipeline(in NativeMeshPipelineDesc desc);

        /// <summary>
        /// Destroys a given <see cref="PipelineHandle"/>
        /// </summary>
        /// <param name="handle">The <see cref="PipelineHandle"/> to destroy</param>
        void DisposePipeline(PipelineHandle handle);


        /// <summary>
        /// Creates an indirect command
        /// </summary>
        /// <param name="rootSig">A <see cref="RootSignature"/> indicating the rootsignature that will be changed by the command</param>
        /// <param name="arguments">The <see cref="ReadOnlySpan{IndirectArgument}"/>s indicating the arguments</param>
        /// <param name="byteStride">The stride (in bytes) between each command</param>
        /// <returns>A new <see cref="IndirectCommandHandle"/></returns>
        IndirectCommandHandle CreateIndirectCommand(RootSignatureHandle rootSig, ReadOnlySpan<IndirectArgument> arguments, uint byteStride);


        /// <summary>
        /// Creates an indirect command
        /// </summary>
        /// <param name="arguments">The <see cref="IndirectArgument"/>s for this command</param>
        /// <param name="byteStride">The stride (in bytes) between each command</param>
        /// <returns>A new <see cref="IndirectCommandHandle"/></returns>
        IndirectCommandHandle CreateIndirectCommand(in IndirectArgument arguments, uint byteStride);


        /// <summary>
        /// Destroys a given <see cref="IndirectCommandHandle"/>
        /// </summary>
        /// <param name="handle">The <see cref="IndirectCommandHandle"/> to destroy</param>
        void DisposeIndirectCommand(IndirectCommandHandle handle);

        /// <summary>
        /// Creates a <see cref="DynamicBufferDescriptorHandle"/> to a  <see cref="BufferHandle"/>
        /// </summary>
        /// <param name="buffer">The <see cref="BufferHandle"/> to create this dynamic descriptor for</param>
        /// <returns>A new <see cref="DynamicBufferDescriptorHandle"/></returns>
        DynamicBufferDescriptorHandle CreateDynamicDescriptor(BufferHandle buffer);

        /// <summary>
        /// Destroys a given <see cref="DynamicBufferDescriptorHandle"/>
        /// </summary>
        /// <param name="handle">The <see cref="DynamicBufferDescriptorHandle"/> to destroy</param>
        void DisposeDynamicDescriptor(DynamicBufferDescriptorHandle handle);

        /// <summary>
        /// Creates a <see cref="DynamicRaytracingAccelerationStructureDescriptorHandle"/> to a  <see cref="RaytracingAccelerationStructureHandle"/>
        /// </summary>
        /// <param name="buffer">The <see cref="RaytracingAccelerationStructureHandle"/> to create this dynamic descriptor for</param>
        /// <returns>A new <see cref="RaytracingAccelerationStructureHandle"/></returns>
        DynamicRaytracingAccelerationStructureDescriptorHandle CreateDynamicDescriptor(RaytracingAccelerationStructureHandle buffer);

        /// <summary>
        /// Destroys a given <see cref="DynamicRaytracingAccelerationStructureDescriptorHandle"/>
        /// </summary>
        /// <param name="handle">The <see cref="DynamicRaytracingAccelerationStructureDescriptorHandle"/> to destroy</param>
        void DisposeDynamicDescriptor(DynamicRaytracingAccelerationStructureDescriptorHandle handle);

        /// <summary>
        /// Creates a <see cref="DescriptorSetHandle"/>
        /// </summary>
        /// <param name="type">The <see cref="DescriptorType"/> of these descriptors</param>
        /// <param name="count">The number of descriptors in this set</param>
        /// <returns>A new <see cref="DescriptorSetHandle"/></returns>
        DescriptorSetHandle CreateDescriptorSet(DescriptorType type, uint count);

        /// <summary>
        /// Destroys a given <see cref="DescriptorSetHandle"/>
        /// </summary>
        /// <param name="handle">The <see cref="DescriptorSetHandle"/> to destroy</param>
        void DisposeDescriptorSet(DescriptorSetHandle handle);

        /// <summary>
        /// Updates descriptors from resource views
        /// </summary>
        /// <param name="views">The <see cref="ViewSetHandle"/> used as the source to create the descriptors from</param>
        /// <param name="firstView">The offset, into <paramref name="views"/>, to start from</param>
        /// <param name="descriptors">The <see cref="DescriptorSetHandle"/> to write descriptors to</param>
        /// <param name="firstDescriptor">The offset, into <paramref name="descriptors"/>, to start from</param>
        /// <param name="count">The number of descriptors to update</param>
        void UpdateDescriptors(ViewSetHandle views, uint firstView, DescriptorSetHandle descriptors, uint firstDescriptor, uint count);

        /// <summary>
        /// Copy descriptors between descriptor sets
        /// </summary>
        /// <param name="source">The <see cref="DescriptorSetHandle"/> to copy from</param>
        /// <param name="firstSource">The offset, into <paramref name="source"/>, to start from</param>
        /// <param name="dest">The <see cref="DescriptorSetHandle"/> to copy to</param>
        /// <param name="firstDest">The offset, into <paramref name="dest"/>, to start from</param>
        /// <param name="count">The number of descriptors to copy</param>
        void CopyDescriptors(DescriptorSetHandle source, uint firstSource, DescriptorSetHandle dest, uint firstDest, uint count);

        /// <summary>
        /// Create a new set of resource views
        /// </summary>
        /// <param name="viewCount">The number of views to allocate</param>
        /// <returns>A new <see cref="ViewSetHandle"/></returns>
        ViewSetHandle CreateViewSet(uint viewCount);

        /// <summary>
        /// Destroys a given <see cref="ViewSetHandle"/>
        /// </summary>
        /// <param name="handle">The <see cref="ViewSetHandle"/> to destroy</param>
        void DisposeViewSet(ViewSetHandle handle);

        /// <summary>
        /// Creates a default view to a buffer
        /// </summary>
        /// <param name="viewHeap">The <see cref="ViewSetHandle"/> to create the view in</param>
        /// <param name="index">The index within <paramref name="viewHeap"/> to create the view at</param>
        /// <param name="handle">The <see cref="BufferHandle"/> to create the view to</param>
        /// <returns>A new <see cref="ViewHandle"/></returns>
        ViewHandle CreateView(ViewSetHandle viewHeap, uint index, BufferHandle handle);

        /// <summary>
        /// Creates a view to a buffer
        /// </summary>
        /// <param name="viewHeap">The <see cref="ViewSetHandle"/> to create the view in</param>
        /// <param name="index">The index within <paramref name="viewHeap"/> to create the view at</param>
        /// <param name="handle">The <see cref="BufferHandle"/> to create the view to</param>
        /// <param name="desc">The <see cref="BufferViewDesc"/> to describe the view to</param>
        /// <returns>A new <see cref="ViewHandle"/></returns>
        ViewHandle CreateView(ViewSetHandle viewHeap, uint index, BufferHandle handle, in BufferViewDesc desc);

        /// <summary>
        /// Creates a default view to a texture
        /// </summary>
        /// <param name="viewHeap">The <see cref="ViewSetHandle"/> to create the view in</param>
        /// <param name="index">The index within <paramref name="viewHeap"/> to create the view at</param>
        /// <param name="handle">The <see cref="TextureHandle"/> to create the view to</param>
        /// <returns>A new <see cref="ViewHandle"/></returns>
        ViewHandle CreateView(ViewSetHandle viewHeap, uint index, TextureHandle handle);

        /// <summary>
        /// Creates a view to a texture
        /// </summary>
        /// <param name="viewHeap">The <see cref="ViewSetHandle"/> to create the view in</param>
        /// <param name="index">The index within <paramref name="viewHeap"/> to create the view at</param>
        /// <param name="handle">The <see cref="TextureHandle"/> to create the view to</param>
        /// <param name="desc">The <see cref="TextureViewDesc"/> to describe the view to</param>
        /// <returns>A new <see cref="ViewHandle"/></returns>
        ViewHandle CreateView(ViewSetHandle viewHeap, uint index, TextureHandle handle, in TextureViewDesc desc);

        /// <summary>
        /// Creates a view to a raytracing acceleration structure
        /// </summary>
        /// <param name="viewHeap">The <see cref="ViewSetHandle"/> to create the view in</param>
        /// <param name="index">The index within <paramref name="viewHeap"/> to create the view at</param>
        /// <param name="handle">The <see cref="RaytracingAccelerationStructureHandle"/> to create the view to</param>
        /// <returns>A new <see cref="ViewHandle"/></returns>
        ViewHandle CreateView(ViewSetHandle viewHeap, uint index, RaytracingAccelerationStructureHandle handle);

    }

    /// <summary>
    /// The type of the descriptor
    /// </summary>
    public enum DescriptorType
    {
        /// <summary>
        /// A buffer of constant data, under 64kb
        /// </summary>
        ConstantBuffer,

        /// <summary>
        /// A buffer of constant date, with a dynamic offset, under 64kb
        /// </summary>
        DynamicConstantBuffer,

        /// <summary>
        /// A buffer of constant data
        /// </summary>
        StructuredBuffer,

        /// <summary>
        /// A writable buffer
        /// </summary>
        WritableStructuredBuffer,

        /// <summary>
        /// A constant buffer, with a dynamic offset
        /// </summary>
        DynamicStructuredBuffer,

        /// <summary>
        /// A writable buffer, with a dynamic offset
        /// </summary>
        DynamicWritableStructuredBuffer,

        /// <summary>
        /// A buffer of typed data
        /// </summary>
        TypedBuffer,

        /// <summary>
        /// A writable buffer of typed data
        /// </summary>
        WritableTypedBuffer,


        /// <summary>
        /// A texture
        /// </summary>
        Texture,

        /// <summary>
        /// A writable texture
        /// </summary>
        WritableTexture,


        /// <summary>
        /// A sampler
        /// </summary>
        Sampler,

        /// <summary>
        /// A raytracing acceleration structure
        /// </summary>
        RaytracingAccelerationStructure
    }

    /// <summary>
    /// A description of a <see cref="ViewHandle"/> to a buffer
    /// </summary>
    public struct BufferViewDesc
    {
        /// <summary>
        /// The <see cref="DataFormat"/> for this buffer, it is of type <see cref="DescriptorType.TypedBuffer"/> or <see cref="DescriptorType.WritableTypedBuffer"/>
        /// </summary>
        public DataFormat Format;

        /// <summary>
        /// The offset, in bytes, of the buffer start
        /// </summary>
        public uint Offset;

        /// <summary>
        /// The length, in bytes, of the buffer view
        /// </summary>
        public uint Length;

        /// <summary>
        /// Whether this buffer is viewed as a raw byte-buffer
        /// </summary>
        /// <remarks>
        /// This prevents compilers optimising this buffer to a different layout (e.g from AOS to SOA)
        /// </remarks>
        public bool IsRaw;
    }

    /// <summary>
    /// A description of a <see cref="ViewHandle"/> to a texture
    /// </summary>
    public struct TextureViewDesc
    {
        /// <summary>
        /// The format to view this texture as
        /// </summary>
        public DataFormat Format;

        /// <summary>
        /// Defines the mapping for the elements of the texture
        /// </summary>
        public ShaderComponentMapping X, Y, Z, W;

        /// <summary>
        /// The first mip in the chain to start the view from
        /// </summary>
        public uint FirstMip;

        /// <summary>
        /// The number of views in the chain to view
        /// </summary>
        public uint MipCount;

        /// <summary>
        /// The first depth slice (if the view is of a 3D texture), or array slice, to start the view from
        /// </summary>
        public uint FirstArrayOrDepthSlice;

        /// <summary>
        /// The number of depth slices (if the view is of a 3D texture), or number of array slices, to view
        /// </summary>
        public uint ArrayOrDepthSliceCount;

        /// <summary>
        /// The <see cref="TextureAspect"/> to view this texture as
        /// </summary>
        public TextureAspect Aspect;

        /// <summary>
        /// The minimum level-of-detail to view this texture as when sampling
        /// </summary>
        public float MinSampleLevelOfDetail;
    }

    /// <summary>
    /// The aspect of a texture
    /// </summary>
    public enum TextureAspect
    {
        /// <summary>
        /// The color aspect. For most textures, this is the only aspect
        /// </summary>
        Color,

        /// <summary>
        /// The depth aspect of a depth-stencil texture
        /// </summary>
        Depth,

        /// <summary>
        /// The stencil aspect of a depth-stencil texture
        /// </summary>
        Stencil
    }

    /// <summary>
    /// The classification, for allocation purposes, of a resource
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// The resource is a texture, but is not valid as a render-target or depth-stencil
        /// </summary>
        Texture,

        /// <summary>
        /// The resource is render-target or depth-stencil texture
        /// </summary>
        RenderTargetOrDepthStencilTexture,

        /// <summary>
        /// The resource buffer
        /// </summary>
        Buffer
    }


    /// <summary>
    /// Alignment in bytes
    /// </summary>
    public enum Alignment : ulong
    {
        /// <summary>
        /// 4 kilobytes
        /// </summary>
        _4KB = 4 * 1024,

        /// <summary>
        /// 64 kilobytes
        /// </summary>
        _64KB = 64 * 1024,

        /// <summary>
        /// 4 megabytes
        /// </summary>
        _4MB = 4 * 1024 * 1024
    }

    /// <summary>
    /// Properties for a given <see cref="HeapHandle"/>
    /// </summary>
    public struct HeapInfo
    {
        /// <summary>
        /// The <see cref="Alignment"/> for this heap
        /// </summary>
        public Alignment Alignment;

        /// <summary>
        /// The <see cref="MemoryAccess"/> for this heap's CPU accessibility
        /// </summary>
        public MemoryAccess Access;

        /// <summary>
        /// The <see cref="ResourceType" /> that this heap tolds
        /// </summary>
        public ResourceType Type;
    }
}
