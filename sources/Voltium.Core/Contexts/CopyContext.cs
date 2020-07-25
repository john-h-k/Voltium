using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Contexts;
using Voltium.Core.Memory;
using Voltium.TextureLoading;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core
{
    /// <summary>
    /// Represents a context on which GPU commands can be recorded
    /// </summary>
    public unsafe partial struct CopyContext : IDisposable
    {
        private GpuContext _context;

        internal CopyContext(in GpuContext context)
        {
            _context = context;
        }

        internal ID3D12GraphicsCommandList* GetListPointer() => _context.List;

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
        /// Copy a subresource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="dest">The resource to copy to</param>
        /// <param name="sourceSubresource">The index of the subresource to copy from</param>
        /// <param name="destSubresource">The index of the subresource to copy to</param>
        public void CopySubresource(in Texture source, in Texture dest, uint sourceSubresource, uint destSubresource)
        {
            ResourceTransition(source, ResourceState.CopySource, sourceSubresource);
            ResourceTransition(dest, ResourceState.CopyDestination, destSubresource);

            Unsafe.SkipInit(out D3D12_TEXTURE_COPY_LOCATION sourceDesc);
            Unsafe.SkipInit(out D3D12_TEXTURE_COPY_LOCATION destDesc);

            sourceDesc.pResource = source.GetResourcePointer();
            sourceDesc.Type = D3D12_TEXTURE_COPY_TYPE.D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
            sourceDesc.Anonymous.SubresourceIndex = sourceSubresource;

            destDesc.pResource = dest.GetResourcePointer();
            destDesc.Type = D3D12_TEXTURE_COPY_TYPE.D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
            destDesc.Anonymous.SubresourceIndex = destSubresource;

            _context.List->CopyTextureRegion(&destDesc, 0, 0, 0, &sourceDesc, null);
        }

        /// <summary>
        /// Copy a subresource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="dest">The resource to copy to</param>
        /// <param name="subresourceIndex">The index of the subresource to copy from</param>
        public void CopySubresource(in Texture source, in Buffer dest, uint subresourceIndex = 0)
        {
            ResourceTransition(source, ResourceState.CopySource, subresourceIndex);
            ResourceTransition(dest, ResourceState.CopyDestination, 0);
            _context.Device.GetCopyableFootprint(source, subresourceIndex, 1, out var layout, out var row, out var numRow, out var size);

            Debug.Assert(dest.Length >= size);

            var sourceDesc = new D3D12_TEXTURE_COPY_LOCATION(source.GetResourcePointer(), subresourceIndex);
            var destDesc = new D3D12_TEXTURE_COPY_LOCATION(dest.GetResourcePointer(), layout);

            _context.FlushBarriers();
            _context.List->CopyTextureRegion(&destDesc, 0, 0, 0, &sourceDesc, null);
        }

        /// <summary>
        /// Copy a subresource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="subresourceIndex">The index of the subresource to copy from</param>
        /// <param name="data"></param>
        public void ReadbackSubresource(in Texture source, uint subresourceIndex, out Buffer data)
        {
            _context.Device.GetCopyableFootprint(source, subresourceIndex, 1, out _, out _, out var rowSize, out var size);
            data = _context.Device.Allocator.AllocateBuffer((long)size, MemoryAccess.CpuReadback, ResourceState.CopyDestination);

            var alignedRowSizes = MathHelpers.AlignUp(rowSize, 256);


            var layout = new D3D12_PLACED_SUBRESOURCE_FOOTPRINT
            {
                Offset = 0,
                Footprint = new D3D12_SUBRESOURCE_FOOTPRINT
                {
                    Depth = source.DepthOrArraySize,
                    Height = source.Height,
                    Width = (uint)source.Width,
                    Format = (DXGI_FORMAT)source.Format,
                    RowPitch = (uint)alignedRowSizes
                }
            };

            var destDesc = new D3D12_TEXTURE_COPY_LOCATION(data.GetResourcePointer(), layout);
            var sourceDesc = new D3D12_TEXTURE_COPY_LOCATION(source.GetResourcePointer(), subresourceIndex);

            _context.FlushBarriers();
            _context.List->CopyTextureRegion(&destDesc, 0, 0, 0, &sourceDesc, null);
        }

        /// <summary>
        /// Copy a subresource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="subresourceIndex">The index of the subresource to copy from</param>
        /// <param name="data"></param>
        public void ReadbackSubresourceToPreexisting(in Texture source, uint subresourceIndex, in Buffer data)
        {
            //ResourceTransition(source, ResourceState.CopySource, subresourceIndex);
            //ResourceTransition(data, ResourceState.CopyDestination, 0);

            _context.Device.GetCopyableFootprint(source, subresourceIndex, 1, out _, out _, out var rowSize, out var size);

            var alignedRowSizes = MathHelpers.AlignUp(rowSize, 256);


            var layout = new D3D12_PLACED_SUBRESOURCE_FOOTPRINT
            {
                Offset = 0,
                Footprint = new D3D12_SUBRESOURCE_FOOTPRINT
                {
                    Depth = source.DepthOrArraySize,
                    Height = source.Height,
                    Width = (uint)source.Width,
                    Format = (DXGI_FORMAT)source.Format,
                    RowPitch = (uint)alignedRowSizes
                }
            };

            var destDesc = new D3D12_TEXTURE_COPY_LOCATION(data.GetResourcePointer(), layout);
            var sourceDesc = new D3D12_TEXTURE_COPY_LOCATION(source.GetResourcePointer(), subresourceIndex);

            _context.FlushBarriers();
            _context.List->CopyTextureRegion(&destDesc, 0, 0, 0, &sourceDesc, null);
        }

        /// <summary>
        /// Copy an entire resource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="dest">The resource to copy to</param>
        public void CopyResource(in Buffer source, in Buffer dest)
        {
            //ResourceTransition(source, ResourceState.CopySource, 0xFFFFFFFF);
            //ResourceTransition(dest, ResourceState.CopyDestination, 0xFFFFFFFF);

            _context.FlushBarriers();
            _context.List->CopyResource(dest.Resource.GetResourcePointer(), source.Resource.GetResourcePointer());
        }

        /// <summary>
        /// Copy an entire resource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="dest">The resource to copy to</param>
        public void CopyResource(in Texture source, in Texture dest)
        {
            //ResourceTransition(source, ResourceState.CopySource, 0xFFFFFFFF);
            //ResourceTransition(dest, ResourceState.CopyDestination, 0xFFFFFFFF);

            _context.FlushBarriers();
            _context.List->CopyResource(dest.Resource.GetResourcePointer(), source.Resource.GetResourcePointer());
        }

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
        {
            var upload = _context.Device.Allocator.AllocateBuffer(buffer.Length * sizeof(T), MemoryAccess.CpuUpload, ResourceState.GenericRead);
            upload.WriteData(buffer);

            CopyResource(upload, destination);
            ResourceTransition(destination, state);
        }

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
        {
            var upload = _context.Device.Allocator.AllocateBuffer(buffer.Length * sizeof(T), MemoryAccess.CpuUpload, ResourceState.GenericRead);
            upload.WriteData(buffer);

            destination = _context.Device.Allocator.AllocateBuffer(buffer.Length * sizeof(T), MemoryAccess.GpuOnly, ResourceState.CopyDestination);
            CopyResource(upload, destination);
            ResourceTransition(destination, state);
        }

        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="subresources"></param>
        /// <param name="tex"></param>
        /// <param name="state"></param>
        /// <param name="destination"></param>
        public void UploadTexture(ReadOnlySpan<byte> texture, ReadOnlySpan<SubresourceData> subresources, in TextureDesc tex, ResourceState state, out Texture destination)
        {
            destination = _context.Device.Allocator.AllocateTexture(tex, ResourceState.CopyDestination);
            UploadTexture(texture, subresources, state, destination);
        }

        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="subresources"></param>
        /// <param name="state"></param>
        /// <param name="destination"></param>
        public void UploadTexture(ReadOnlySpan<byte> texture, ReadOnlySpan<SubresourceData> subresources, ResourceState state, in Texture destination)
        {
            var upload = _context.Device.Allocator.AllocateBuffer(
                (long)Windows.GetRequiredIntermediateSize(destination.Resource.GetResourcePointer(), 0, (uint)subresources.Length),
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
                    destination.Resource.GetResourcePointer(),
                    upload.Resource.GetResourcePointer(),
                    0,
                    0,
                    (uint)subresources.Length,
                    (D3D12_SUBRESOURCE_DATA*)pSubresources
                );

                ResourceTransition(destination, state);
            }
        }


        /// <summary>
        /// Mark a resource barrierson the command list
        /// </summary>
        /// <param name="barrier">The barrier</param>
        public void ResourceBarrier(in ResourceBarrier barrier)
        {
            _context.AddBarrier(barrier.Barrier);
        }

        /// <summary>
        /// Mark a set of resource barriers on the command list
        /// </summary>
        /// <param name="barriers">The barriers</param>
        public void ResourceBarrier(ReadOnlySpan<ResourceBarrier> barriers)
        {
            _context.AddBarriers(MemoryMarshal.Cast<ResourceBarrier, D3D12_RESOURCE_BARRIER>(barriers));
        }

        /// <summary>
        /// Mark a resource barrier on the command list
        /// </summary>
        /// <param name="resource">The resource to transition</param>
        /// <param name="transition">The transition</param>
        /// <param name="subresource">The subresource to transition</param>
        public void ResourceTransition(in Buffer resource, ResourceState transition, uint subresource = 0xFFFFFFFF)
            => ResourceTransition(resource.Resource, transition, subresource);

        /// <summary>
        /// Mark a resource barrier on the command list
        /// </summary>
        /// <param name="resource">The resource to transition</param>
        /// <param name="transition">The transition</param>
        /// <param name="subresource">The subresource to transition</param>
        public void ResourceTransition(in Texture resource, ResourceState transition, uint subresource = 0xFFFFFFFF)
            => ResourceTransition(resource.Resource, transition, subresource);


        /// <summary>
        /// Mark a resource barrier on the command list
        /// </summary>
        /// <param name="resource">The resource to transition</param>
        /// <param name="transition">The transition</param>
        /// <param name="subresource">The subresource to transition</param>
        public void BeginResourceTransition(in Buffer resource, ResourceState transition, uint subresource = 0xFFFFFFFF)
            => BeginResourceTransition(resource.Resource, transition, subresource);

        /// <summary>
        /// Mark a resource barrier on the command list
        /// </summary>
        /// <param name="resource">The resource to transition</param>
        /// <param name="transition">The transition</param>
        /// <param name="subresource">The subresource to transition</param>
        public void BeginResourceTransition(in Texture resource, ResourceState transition, uint subresource = 0xFFFFFFFF)
            => BeginResourceTransition(resource.Resource, transition, subresource);


        /// <summary>
        /// Mark a resource barrier on the command list
        /// </summary>
        /// <param name="resource">The resource to transition</param>
        /// <param name="transition">The transition</param>
        /// <param name="subresource">The subresource to transition</param>
        public void EndResourceTransition(in Buffer resource, ResourceState transition, uint subresource = 0xFFFFFFFF)
            => EndResourceTransition(resource.Resource, transition, subresource);

        /// <summary>
        /// Mark a resource barrier on the command list
        /// </summary>
        /// <param name="resource">The resource to transition</param>
        /// <param name="transition">The transition</param>
        /// <param name="subresource">The subresource to transition</param>
        public void EndResourceTransition(in Texture resource, ResourceState transition, uint subresource = 0xFFFFFFFF)
            => EndResourceTransition(resource.Resource, transition, subresource);

        private static bool IsTransitionNecessary(ResourceState state, ResourceState transition)
            => transition == ResourceState.Common || (state & transition) != transition;

        private void ResourceTransition(GpuResource resource, ResourceState transition, uint subresource)
        {
            // demote a full resource transition to an end-only where applicable because it is more efficient
            if (resource.TransitionBegan)
            {
                EndResourceTransition(resource, transition, subresource);
                return;
            }

            var state = resource.State;

            // don't do unnecessary work
            if (!IsTransitionNecessary(state, transition))
            {
                return;
            }

            Unsafe.SkipInit(out D3D12_RESOURCE_BARRIER barrier);
            {
                barrier.Type = D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
                // If the state isn't flushed, we need to end the previous barrier
                barrier.Flags = 0;
                barrier.Anonymous.Transition = new D3D12_RESOURCE_TRANSITION_BARRIER
                {
                    pResource = resource.GetResourcePointer(),
                    StateBefore = (D3D12_RESOURCE_STATES)state,
                    StateAfter = (D3D12_RESOURCE_STATES)transition,
                    Subresource = subresource
                };
            };

            resource.State = transition;

            _context.AddBarrier(barrier);
        }

        private void BeginResourceTransition(GpuResource resource, ResourceState transition, uint subresource)
        {
            var state = resource.State;

            // don't do unnecessary work
            // end barrier must be dropped if begin barrier is dropped (resource.TransitionBegan tracks this)
            if (!IsTransitionNecessary(state, transition))
            {
                return;
            }

            Unsafe.SkipInit(out D3D12_RESOURCE_BARRIER barrier);
            {
                barrier.Type = D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
                barrier.Flags = D3D12_RESOURCE_BARRIER_FLAGS.D3D12_RESOURCE_BARRIER_FLAG_BEGIN_ONLY;
                barrier.Anonymous.Transition = new D3D12_RESOURCE_TRANSITION_BARRIER
                {
                    pResource = resource.GetResourcePointer(),
                    StateBefore = (D3D12_RESOURCE_STATES)state,
                    StateAfter = (D3D12_RESOURCE_STATES)transition,
                    Subresource = subresource
                };
            };

            resource.TransitionBegan = true;

            _context.AddBarrier(barrier);
        }

        private void EndResourceTransition(GpuResource resource, ResourceState transition, uint subresource)
        {
            var state = resource.State;

            // drop ends with no beginnings, which occurs when a begin resource transition is marked as unnecessary
            // it also happens with invalid end resources (where no begin call has ever been made), but hopefully the debug layer will catch these
            if (!resource.TransitionBegan)
            {
                return;
            }

            Unsafe.SkipInit(out D3D12_RESOURCE_BARRIER barrier);
            {
                barrier.Type = D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
                barrier.Flags = D3D12_RESOURCE_BARRIER_FLAGS.D3D12_RESOURCE_BARRIER_FLAG_END_ONLY;
                barrier.Anonymous.Transition = new D3D12_RESOURCE_TRANSITION_BARRIER
                {
                    pResource = resource.GetResourcePointer(),
                    StateBefore = (D3D12_RESOURCE_STATES)state,
                    StateAfter = (D3D12_RESOURCE_STATES)transition,
                    Subresource = subresource
                };
            };

            resource.State = transition;
            resource.TransitionBegan = false;

            _context.AddBarrier(barrier);
        }

        /// <inheritdoc/>
        public void Dispose() => _context.Dispose();
    }
}
