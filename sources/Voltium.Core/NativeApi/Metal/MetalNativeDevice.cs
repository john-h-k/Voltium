using System;
using System.Buffers;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Veldrid.MetalBindings;
using Voltium.Common;
using Voltium.Core.Contexts;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;
using Voltium.Core.Queries;
using System.Runtime.InteropServices;
using System.Collections.Immutable;
using Voltium.Core.NativeApi;
using Voltium.Allocators;
using System.Runtime.Versioning;

namespace Voltium.Core.Devices
{
    /// <summary>
    /// The native device type used to interface with a GPU
    /// </summary>
    [SupportedOSPlatform("macOS")]
    [SupportedOSPlatform("iOS")]
    [SupportedOSPlatform("MacCatalyst")]
    public unsafe class MetalNativeDevice : INativeDevice
    {
        public DeviceInfo Info => throw new NotImplementedException();

        private MTLDevice _device;
        private MetalHandleMapper _mapper;

        public MetalNativeDevice()
        {
            _device = MTLDevice.MTLCreateSystemDefaultDevice();
        }

        public BufferHandle AllocateBuffer(in BufferDesc desc, MemoryAccess access)
        {
            var options = MTLResourceOptions.HazardTrackingModeUntracked | access switch {
                MemoryAccess.GpuOnly => MTLResourceOptions.StorageModePrivate,
                MemoryAccess.CpuUpload => MTLResourceOptions.StorageModeShared | MTLResourceOptions.CPUCacheModeWriteCombined,
                MemoryAccess.CpuReadback => MTLResourceOptions.StorageModeShared | MTLResourceOptions.CPUCacheModeDefaultCache,
                _ => default
            };
            
            
            var native = _device.newBufferWithLengthOptions((nuint)desc.Length, options);

            var buffer = new MetalBuffer
            {
                Buffer = native,
                Flags = options,
                Length = desc.Length,
                CpuAddress = native.contents()
            };

            return _mapper.Create(buffer);
        }

        public BufferHandle AllocateBuffer(in BufferDesc desc, HeapHandle heap, ulong offset) => throw new NotImplementedException();

        public TextureHandle AllocateTexture(in TextureDesc desc, ResourceState initial) => throw new NotImplementedException();
        public TextureHandle AllocateTexture(in TextureDesc desc, ResourceState initial, HeapHandle heap, ulong offset) => throw new NotImplementedException();
        public void CopyDescriptors(DescriptorSetHandle source, uint firstSource, DescriptorSetHandle dest, uint firstDest, uint count) => throw new NotImplementedException();
        public DescriptorSetHandle CreateDescriptorSet(DescriptorType type, uint count) => throw new NotImplementedException();
        public DynamicBufferDescriptorHandle CreateDynamicDescriptor(BufferHandle buffer) => throw new NotImplementedException();
        public FenceHandle CreateFence(ulong initialValue, FenceFlags flags = FenceFlags.None) => throw new NotImplementedException();
        public HeapHandle CreateHeap(ulong size, in HeapInfo info) => throw new NotImplementedException();
        public IndirectCommandHandle CreateIndirectCommand(RootSignatureHandle rootSig, ReadOnlySpan<IndirectArgument> arguments, uint byteStride) => throw new NotImplementedException();
        public IndirectCommandHandle CreateIndirectCommand(in IndirectArgument arguments, uint byteStride) => throw new NotImplementedException();
        public LocalRootSignatureHandle CreateLocalRootSignature(ReadOnlySpan<RootParameter> rootParams, ReadOnlySpan<StaticSampler> samplers, RootSignatureFlags flags) => throw new NotImplementedException();
        public PipelineHandle CreatePipeline(in NativeComputePipelineDesc desc) => throw new NotImplementedException();
        public PipelineHandle CreatePipeline(in NativeGraphicsPipelineDesc desc)
        {
            var lib = _device.newLibraryWithSource();

            var nativeDesc = new MTLRenderPipelineDescriptor();
            nativeDesc.alphaToCoverageEnabled = desc.Blend.UseAlphaToCoverage;
            // nativeDesc.vertexFunction = MTLFunction. desc.VertexShader;

            _device.newRenderPipelineStateWithDescriptor();
        }

        public QuerySetHandle CreateQuerySet(QuerySetType type, uint length) => throw new NotImplementedException();
        public INativeQueue CreateQueue(ExecutionEngine context) => throw new NotImplementedException();
        public RootSignatureHandle CreateRootSignature(ReadOnlySpan<RootParameter> rootParams, ReadOnlySpan<StaticSampler> samplers, RootSignatureFlags flags) => throw new NotImplementedException();
        public ViewHandle CreateView(ViewSetHandle viewHeap, uint index, BufferHandle handle) => throw new NotImplementedException();
        public ViewHandle CreateView(ViewSetHandle viewHeap, uint index, BufferHandle handle, in BufferViewDesc desc) => throw new NotImplementedException();
        public ViewHandle CreateView(ViewSetHandle viewHeap, uint index, TextureHandle handle) => throw new NotImplementedException();
        public ViewHandle CreateView(ViewSetHandle viewHeap, uint index, TextureHandle handle, in TextureViewDesc desc) => throw new NotImplementedException();
        public ViewSetHandle CreateViewSet(uint viewCount) => throw new NotImplementedException();
        public void Dispose() => throw new NotImplementedException();
        public void DisposeBuffer(BufferHandle handle) => throw new NotImplementedException();
        public void DisposeDescriptorSet(DescriptorSetHandle handle) => throw new NotImplementedException();
        public void DisposeDynamicDescriptor(DynamicBufferDescriptorHandle handle) => throw new NotImplementedException();
        public void DisposeFence(FenceHandle fence) => throw new NotImplementedException();
        public void DisposeHeap(HeapHandle handle) => throw new NotImplementedException();
        public void DisposeIndirectCommand(IndirectCommandHandle handle) => throw new NotImplementedException();
        public void DisposeLocalRootSignature(LocalRootSignatureHandle handle) => throw new NotImplementedException();
        public void DisposePipeline(PipelineHandle handle) => throw new NotImplementedException();
        public void DisposeQuerySet(QuerySetHandle handle) => throw new NotImplementedException();
        public void DisposeRootSignature(RootSignatureHandle handle) => throw new NotImplementedException();
        public void DisposeTexture(TextureHandle handle) => throw new NotImplementedException();
        public void DisposeViewSet(ViewSetHandle handle) => throw new NotImplementedException();
        public ulong GetCompletedValue(FenceHandle fence) => throw new NotImplementedException();
        public OSEvent GetEventForWait(ReadOnlySpan<FenceHandle> fences, ReadOnlySpan<ulong> values, WaitMode mode) => throw new NotImplementedException();
        public (ulong Alignment, ulong Length) GetTextureAllocationInfo(in TextureDesc desc) => throw new NotImplementedException();
        public void* Map(BufferHandle handle) => throw new NotImplementedException();
        public void Unmap(BufferHandle handle) => throw new NotImplementedException();
        public void UpdateDescriptors(ViewSetHandle views, uint firstView, DescriptorSetHandle descriptors, uint firstDescriptor, uint count) => throw new NotImplementedException();
        public void Wait(ReadOnlySpan<FenceHandle> fences, ReadOnlySpan<ulong> values, WaitMode mode) => throw new NotImplementedException();
    }
}