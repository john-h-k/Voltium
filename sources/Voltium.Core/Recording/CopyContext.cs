using System;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Core.GpuResources;
using Voltium.Core.Memory.GpuResources;
using Voltium.TextureLoading;
using Buffer = Voltium.Core.Memory.GpuResources.Buffer;

namespace Voltium.Core
{
    /// <summary>
    /// Represents a context on which GPU commands can be recorded
    /// </summary>
    public unsafe partial struct CopyContext : IDisposable
    {
        private GpuContext _context;

        internal CopyContext(GpuContext context)
        {
            _context = context;
        }

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

        /// <summary>
        /// Copy an entire resource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="dest">The resource to copy to</param>
        public void CopyResource(Buffer source, Buffer dest)
        {
            if (!source.Resource.State.HasFlag(ResourceState.CopySource))
            {
                ResourceTransition(source, ResourceState.CopySource, 0xFFFFFFFF);
            }
            if (!dest.Resource.State.HasFlag(ResourceState.CopyDestination))
            {
                ResourceTransition(dest, ResourceState.CopyDestination, 0xFFFFFFFF);
            }

            _context.FlushBarriers();
            _context.List->CopyResource(dest.Resource.UnderlyingResource, source.Resource.UnderlyingResource);
        }

        /// <summary>
        /// Copy an entire resource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="dest">The resource to copy to</param>
        public void CopyResource(Texture source, Texture dest)
        {
            ResourceTransition(source, ResourceState.CopySource, 0xFFFFFFFF);
            ResourceTransition(dest, ResourceState.CopyDestination, 0xFFFFFFFF);

            _context.FlushBarriers();
            _context.List->CopyResource(dest.Resource.UnderlyingResource, source.Resource.UnderlyingResource);
        }

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
        {
            var upload = allocator.AllocateBuffer(buffer.Length * sizeof(T), MemoryAccess.CpuUpload, ResourceState.GenericRead);
            upload.WriteData(buffer);

            CopyResource(upload, destination);
        }

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
        {
            var upload = allocator.AllocateBuffer(buffer.Length * sizeof(T), MemoryAccess.CpuUpload, ResourceState.GenericRead);
            upload.WriteData(buffer);

            destination = allocator.AllocateBuffer(buffer.Length * sizeof(T), MemoryAccess.GpuOnly, ResourceState.CopyDestination);
            CopyResource(upload, destination);
        }

        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="allocator"></param>
        /// <param name="texture"></param>
        /// <param name="subresources"></param>
        /// <param name="tex"></param>
        /// <param name="destination"></param>
        public void UploadTexture(GpuAllocator allocator, ReadOnlySpan<byte> texture, ReadOnlySpan<SubresourceData> subresources, TextureDesc tex, out Texture destination)
        {
            destination = allocator.AllocateTexture(tex, ResourceState.CopyDestination);
            UploadTexture(allocator, texture, subresources, destination);
        }

        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="allocator"></param>
        /// <param name="texture"></param>
        /// <param name="subresources"></param>
        /// <param name="destination"></param>
        public void UploadTexture(GpuAllocator allocator, ReadOnlySpan<byte> texture, ReadOnlySpan<SubresourceData> subresources, Texture destination)
        {
            var upload = allocator.AllocateBuffer(
                (long)Windows.GetRequiredIntermediateSize(destination.Resource.UnderlyingResource, 0, (uint)subresources.Length),
                MemoryAccess.CpuUpload,
                ResourceState.GenericRead
            );

            fixed (byte* pTextureData = texture)
            fixed (SubresourceData* pSubresources = subresources)
            {
                // D3D12_SUBRESOURCE_DATA and SubresourceData are blittable, just SubresourceData contains an offset past the pointer rather than the pointer
                // Fix that here
                for (var i = 0; i < subresources.Length; i++)
                {
                    ((D3D12_SUBRESOURCE_DATA*)&pSubresources[i])->pData = pTextureData + pSubresources[i].DataOffset;
                }

                _context.FlushBarriers();
                _ = Windows.UpdateSubresources(
                    _context.List,
                    destination.Resource.UnderlyingResource,
                    upload.Resource.UnderlyingResource,
                    0,
                    0,
                    (uint)subresources.Length,
                    (D3D12_SUBRESOURCE_DATA*)pSubresources
                );
            }
        }



        /// <summary>
        /// Mark a resource barrier on the command list
        /// </summary>
        /// <param name="resource">The resource to transition</param>
        /// <param name="transition">The transition</param>
        /// <param name="subresource">The subresource to transition</param>
        public void ResourceTransition(Buffer resource, ResourceState transition, uint subresource = 0xFFFFFFFF)
            => ResourceTransition(resource.Resource, transition, subresource);

        /// <summary>
        /// Mark a resource barrier on the command list
        /// </summary>
        /// <param name="resource">The resource to transition</param>
        /// <param name="transition">The transition</param>
        /// <param name="subresource">The subresource to transition</param>
        public void ResourceTransition(Texture resource, ResourceState transition, uint subresource = 0xFFFFFFFF)
            => ResourceTransition(resource.Resource, transition, subresource);

        private void ResourceTransition(GpuResource resource, ResourceState transition, uint subresource = 0xFFFFFFFF)
        {
            // don't do unnecessary work
            // ResourceState.Common is 0 and must be transitioned to. Same applies for present (also 0)
            if (transition != ResourceState.Common && (resource.State & transition) == transition)
            {
                return;
            }

            Unsafe.SkipInit(out D3D12_RESOURCE_BARRIER barrier);
            {
                barrier.Type = D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
                barrier.Flags = D3D12_RESOURCE_BARRIER_FLAGS.D3D12_RESOURCE_BARRIER_FLAG_NONE;
                barrier.Anonymous.Transition = new D3D12_RESOURCE_TRANSITION_BARRIER
                {
                    pResource = resource.UnderlyingResource,
                    StateBefore = (D3D12_RESOURCE_STATES)resource.State,
                    StateAfter = (D3D12_RESOURCE_STATES)transition,
                    Subresource = subresource
                };
            };

            resource.State = transition;
            _context.AddBarrier(barrier);
        }

        /// <inheritdoc/>
        public void Dispose() => _context.Dispose();
    }
}
