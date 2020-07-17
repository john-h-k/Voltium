using System;
using TerraFX.Interop;
using Voltium.Core.Memory;
using Voltium.TextureLoading;
using Buffer = Voltium.Core.Memory.Buffer;

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
        internal ID3D12GraphicsCommandList* GetListPointer() => _context.List;

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

        internal void Bad(Texture tex)
        {

        }

        /// <summary>
        /// Sets a directly-bound constant buffer view descriptor to the compute pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="cbuffer">The <see cref="Buffer"/> containing the buffer to add</param>
        public void SetConstantBuffer(uint paramIndex, in Buffer cbuffer)
            => SetConstantBuffer<byte>(paramIndex, cbuffer, 0);

        /// <summary>
        /// Sets a directly-bound constant buffer view descriptor to the compute pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="cbuffer">The <see cref="Buffer"/> containing the buffer to add</param>
        /// <param name="offset">The offset in bytes to start the view at</param>
        public void SetConstantBuffer<T>(uint paramIndex, in Buffer cbuffer, uint offset = 0) where T : unmanaged
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
        public void SetConstantBufferByteOffset(uint paramIndex, in Buffer cbuffer, uint offset = 0)
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
        public void CopyResource(in Buffer source, in Buffer dest)
            => this.AsCopyContext().CopyResource(source, dest);

        /// <summary>
        /// Copy an entire resource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="dest">The resource to copy to</param>
        public void CopyResource(in Texture source, in Texture dest)
            => this.AsCopyContext().CopyResource(source, dest);

        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="state"></param>
        /// <param name="destination"></param>
        public void UploadBufferToPreexisting<T>(T[] buffer, ResourceState state, in Buffer destination) where T : unmanaged
            => UploadBufferToPreexisting((ReadOnlySpan<T>)buffer, state, destination);


        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="state"></param>
        /// <param name="destination"></param>
        public void UploadBufferToPreexisting<T>(Span<T> buffer, ResourceState state, in Buffer destination) where T : unmanaged
            => UploadBufferToPreexisting((ReadOnlySpan<T>)buffer, state, destination);


        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="state"></param>
        /// <param name="destination"></param>
        public void UploadBufferToPreexisting<T>(ReadOnlySpan<T> buffer, ResourceState state, in Buffer destination) where T : unmanaged
            => this.AsCopyContext().UploadBufferToPreexisting<T>(buffer, state, destination);

        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="state"></param>
        /// <param name="destination"></param>
        public void UploadBuffer<T>(T[] buffer, ResourceState state, out Buffer destination) where T : unmanaged
            => UploadBuffer((ReadOnlySpan<T>)buffer, state, out destination);


        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="state"></param>
        /// <param name="destination"></param>
        public void UploadBuffer<T>(Span<T> buffer, ResourceState state, out Buffer destination) where T : unmanaged
            => UploadBuffer((ReadOnlySpan<T>)buffer, state, out destination);


        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="state"></param>
        /// <param name="destination"></param>
        public void UploadBuffer<T>(ReadOnlySpan<T> buffer, ResourceState state, out Buffer destination) where T : unmanaged
            => this.AsCopyContext().UploadBuffer<T>(buffer, state, out destination);

        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="subresources"></param>
        /// <param name="tex"></param>
        /// <param name="state"></param>
        /// <param name="destination"></param>
        public void UploadTexture(ReadOnlySpan<byte> texture, ReadOnlySpan<SubresourceData> subresources, in TextureDesc tex, ResourceState state, out Texture destination)
            => this.AsCopyContext().UploadTexture(texture, subresources, tex, state, out destination);

        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="subresources"></param>
        /// <param name="state"></param>
        /// <param name="destination"></param>
        public void UploadTexture(ReadOnlySpan<byte> texture, ReadOnlySpan<SubresourceData> subresources, ResourceState state, in Texture destination)
        => this.AsCopyContext().UploadTexture(texture, subresources, state, destination);


        /// <summary>
        /// Mark a resource barrier on the command list
        /// </summary>
        /// <param name="resource">The resource to transition</param>
        /// <param name="transition">The transition</param>
        /// <param name="subresource">The subresource to transition</param>
        public void ResourceTransition(in Buffer resource, ResourceState transition, uint subresource = 0xFFFFFFFF)
            => this.AsCopyContext().ResourceTransition(resource, transition, subresource);


        /// <summary>
        /// Mark a resource barrier on the command list
        /// </summary>
        /// <param name="resource">The resource to transition</param>
        /// <param name="transition">The transition</param>
        /// <param name="subresource">The subresource to transition</param>
        public void ResourceTransition(in Texture resource, ResourceState transition, uint subresource = 0xFFFFFFFF)
            => this.AsCopyContext().ResourceTransition(resource, transition, subresource);

        /// <summary>
        /// Mark a resource barrier on the command list
        /// </summary>
        /// <param name="resource">The resource to transition</param>
        /// <param name="transition">The transition</param>
        /// <param name="subresource">The subresource to transition</param>
        public void BeginResourceTransition(in Buffer resource, ResourceState transition, uint subresource = 0xFFFFFFFF)
            => this.AsCopyContext().BeginResourceTransition(resource, transition, subresource);

        /// <summary>
        /// Mark a resource barrier on the command list
        /// </summary>
        /// <param name="resource">The resource to transition</param>
        /// <param name="transition">The transition</param>
        /// <param name="subresource">The subresource to transition</param>
        public void BeginResourceTransition(in Texture resource, ResourceState transition, uint subresource = 0xFFFFFFFF)
            => this.AsCopyContext().BeginResourceTransition(resource, transition, subresource);


        /// <summary>
        /// Mark a resource barrier on the command list
        /// </summary>
        /// <param name="resource">The resource to transition</param>
        /// <param name="transition">The transition</param>
        /// <param name="subresource">The subresource to transition</param>
        public void EndResourceTransition(in Buffer resource, ResourceState transition, uint subresource = 0xFFFFFFFF)
            => this.AsCopyContext().EndResourceTransition(resource, transition, subresource);

        /// <summary>
        /// Mark a resource barrier on the command list
        /// </summary>
        /// <param name="resource">The resource to transition</param>
        /// <param name="transition">The transition</param>
        /// <param name="subresource">The subresource to transition</param>
        public void EndResourceTransition(in Texture resource, ResourceState transition, uint subresource = 0xFFFFFFFF)
            => this.AsCopyContext().EndResourceTransition(resource, transition, subresource);

        #endregion
    }
}
