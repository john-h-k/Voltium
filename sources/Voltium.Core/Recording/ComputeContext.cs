using System;
using Voltium.Core.GpuResources;
using Voltium.Core.Memory.GpuResources;
using Voltium.TextureLoading;
using Buffer = Voltium.Core.Memory.GpuResources.Buffer;

namespace Voltium.Core
{
    /// <summary>
    /// Represents a context on which GPU commands can be recorded
    /// </summary>
    public unsafe partial struct ComputeContext : IDisposable
    {
        private GpuContext _context;

        internal ComputeContext(GpuContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public void Dispose() => _context.Dispose();
        //AtomicCopyBufferUINT
        //AtomicCopyBufferUINT64
        //CopyBufferRegion
        //CopyResource
        //CopyTextureRegion
        //CopyTiles
        //EndQuery
        //ResolveQueryData
        //ResourceBarrier
        //SetProtectedResourceSession
        //WriteBufferImmediate

        //BuildRaytracingAccelerationStructure
        //ClearState
        //ClearUnorderedAccessViewFloat
        //ClearUnorderedAccessViewUint
        //CopyRaytracingAccelerationStructure
        //DiscardResource
        //Dispatch
        //DispatchRays
        //EmitRaytracingAccelerationStructurePostbuildInfo
        //ExecuteIndirect
        //ExecuteMetaCommand
        //InitializeMetaCommand
        //ResolveQueryData
        //ResourceBarrier
        //SetComputeRoot32BitConstant
        //SetComputeRoot32BitConstants
        //SetComputeRootConstantBufferView
        //SetComputeRootDescriptorTable
        //SetComputeRootShaderResourceView
        //SetComputeRootSignature
        //SetComputeRootUnorderedAccessView
        //SetDescriptorHeaps
        //SetPipelineState
        //SetPipelineState1
        //SetPredication

        //BeginEvent
        //BeginQuery
        //ClearState
        //ClearUnorderedAccessViewFloat
        //ClearUnorderedAccessViewUint
        //Close
        //CopyBufferRegion
        //CopyResource
        //CopyTextureRegion
        //Dispatch
        //EndEvent
        //EndQuery
        //Reset
        //ResolveQueryData
        //ResourceBarrier
        //SetComputeRoot32BitConstant
        //SetComputeRoot32BitConstants
        //SetComputeRootConstantBufferView
        //SetComputeRootDescriptorTable
        //SetComputeRootShaderResourceView
        //SetComputeRootSignature
        //SetComputeRootUnorderedAccessView
        //SetDescriptorHeaps
        //SetMarker
        //SetPipelineState
        //SetPredication

        /// <summary>
        /// Sets a directly-bound constant buffer view descriptor to the compute pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="cbuffer">The <see cref="Buffer"/> containing the buffer to add</param>
        public void SetConstantBuffer(uint paramIndex, Buffer cbuffer)
            => SetConstantBuffer<byte>(paramIndex, cbuffer, 0);

        /// <summary>
        /// Sets a directly-bound constant buffer view descriptor to the compute pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="cbuffer">The <see cref="Buffer"/> containing the buffer to add</param>
        /// <param name="offset">The offset in bytes to start the view at</param>
        public void SetConstantBuffer<T>(uint paramIndex, Buffer cbuffer, uint offset = 0) where T : unmanaged
        {
            var alignedSize = (sizeof(T) + 255) & ~255;

            _context.List->SetComputeRootConstantBufferView(paramIndex, cbuffer.GpuAddress + (ulong)(alignedSize * offset));
        }

        /// <summary>
        /// Sets a directly-bound constant buffer view descriptor to the compute pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="cbuffer">The <see cref="Buffer"/> containing the buffer to add</param>
        /// <param name="offset">The offset in bytes to start the view at</param>
        public void SetConstantBufferByteOffset(uint paramIndex, Buffer cbuffer, uint offset = 0)
        {
            _context.List->SetComputeRootConstantBufferView(paramIndex, cbuffer.GpuAddress + offset);
        }

        /// <summary>
        /// Sets a descriptor table to the compute pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="handle">The <see cref="DescriptorHandle"/> containing the first view</param>
        public void SetRootDescriptorTable(uint paramIndex, DescriptorHandle handle)
        {
            _context.List->SetComputeRootDescriptorTable(paramIndex, handle.GpuHandle);
        }

        /// <summary>
        /// Set the compute root signature for the command list
        /// </summary>
        /// <param name="signature">The signature to set to</param>
        public void SetRootSignature(RootSignature signature)
        {
            _context.List->SetComputeRootSignature(signature.Value);
        }

        #region CopyContext Methods

        /// <summary>
        /// Copy an entire resource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="dest">The resource to copy to</param>
        public void CopyResource(Buffer source, Buffer dest)
            => this.AsCopyContext().CopyResource(source, dest);

        /// <summary>
        /// Copy an entire resource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="dest">The resource to copy to</param>
        public void CopyResource(Texture source, Texture dest)
            => this.AsCopyContext().CopyResource(source, dest);

        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="allocator"></param>
        /// <param name="buffer"></param>
        /// <param name="destination"></param>
        public void UploadBuffer<T>(GpuAllocator allocator, T[] buffer, Buffer destination) where T : unmanaged
            => UploadBuffer(allocator, (ReadOnlySpan<T>)buffer, destination);


        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="allocator"></param>
        /// <param name="buffer"></param>
        /// <param name="destination"></param>
        public void UploadBuffer<T>(GpuAllocator allocator, Span<T> buffer, Buffer destination) where T : unmanaged
            => UploadBuffer(allocator, (ReadOnlySpan<T>)buffer, destination);


        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="allocator"></param>
        /// <param name="buffer"></param>
        /// <param name="destination"></param>
        public void UploadBuffer<T>(GpuAllocator allocator, ReadOnlySpan<T> buffer, Buffer destination) where T : unmanaged
            => this.AsCopyContext().UploadBuffer<T>(allocator, buffer, destination);

        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="allocator"></param>
        /// <param name="buffer"></param>
        /// <param name="destination"></param>
        public void UploadBuffer<T>(GpuAllocator allocator, T[] buffer, out Buffer destination) where T : unmanaged
            => UploadBuffer(allocator, (ReadOnlySpan<T>)buffer, out destination);


        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="allocator"></param>
        /// <param name="buffer"></param>
        /// <param name="destination"></param>
        public void UploadBuffer<T>(GpuAllocator allocator, Span<T> buffer, out Buffer destination) where T : unmanaged
            => UploadBuffer(allocator, (ReadOnlySpan<T>)buffer, out destination);


        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="allocator"></param>
        /// <param name="buffer"></param>
        /// <param name="destination"></param>
        public void UploadBuffer<T>(GpuAllocator allocator, ReadOnlySpan<T> buffer, out Buffer destination) where T : unmanaged
            => this.AsCopyContext().UploadBuffer<T>(allocator, buffer, out destination);

        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="allocator"></param>
        /// <param name="texture"></param>
        /// <param name="subresources"></param>
        /// <param name="tex"></param>
        /// <param name="destination"></param>
        public void UploadTexture(GpuAllocator allocator, ReadOnlySpan<byte> texture, ReadOnlySpan<SubresourceData> subresources, TextureDesc tex, out Texture destination)
            => this.AsCopyContext().UploadTexture(allocator, texture, subresources, tex, out destination);

        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="allocator"></param>
        /// <param name="texture"></param>
        /// <param name="subresources"></param>
        /// <param name="destination"></param>
        public void UploadTexture(GpuAllocator allocator, ReadOnlySpan<byte> texture, ReadOnlySpan<SubresourceData> subresources, Texture destination)
        => this.AsCopyContext().UploadTexture(allocator, texture, subresources, destination);


        /// <summary>
        /// Mark a resource barrier on the command list
        /// </summary>
        /// <param name="resource">The resource to transition</param>
        /// <param name="transition">The transition</param>
        /// <param name="subresource">The subresource to transition</param>
        public void ResourceTransition(Buffer resource, ResourceState transition, uint subresource = 0xFFFFFFFF)
            => this.AsCopyContext().ResourceTransition(resource, transition, subresource);


        /// <summary>
        /// Mark a resource barrier on the command list
        /// </summary>
        /// <param name="resource">The resource to transition</param>
        /// <param name="transition">The transition</param>
        /// <param name="subresource">The subresource to transition</param>
        public void ResourceTransition(Texture resource, ResourceState transition, uint subresource = 0xFFFFFFFF)
            => this.AsCopyContext().ResourceTransition(resource, transition, subresource);

        #endregion
    }
}
