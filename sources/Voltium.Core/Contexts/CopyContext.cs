using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Contexts;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.Pool;
using Voltium.TextureLoading;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core
{
    /// <summary>
    /// Represents a context on which GPU commands can be recorded
    /// </summary>
    public unsafe partial class CopyContext : GpuContext
    {
        [FixedBufferType(typeof(D3D12_RESOURCE_BARRIER), 16)]
        private partial struct BarrierBuffer16 { }

        private BarrierBuffer16 Barriers;
        private uint NumBarriers;


        internal void AddBarrier(in D3D12_RESOURCE_BARRIER barrier)
        {
            if (NumBarriers == BarrierBuffer16.BufferLength)
            {
                FlushBarriers();
            }

            Barriers[NumBarriers++] = barrier;
        }

        internal void AddBarriers(ReadOnlySpan<D3D12_RESOURCE_BARRIER> barriers)
        {
            if (barriers.Length == 0)
            {
                return;
            }

            if (barriers.Length > BarrierBuffer16.BufferLength)
            {
                FlushBarriers();
                fixed (D3D12_RESOURCE_BARRIER* pBarriers = barriers)
                {
                    List->ResourceBarrier((uint)barriers.Length, pBarriers);
                }
                return;
            }

            if (NumBarriers + barriers.Length >= BarrierBuffer16.BufferLength)
            {
                FlushBarriers();
            }

            barriers.CopyTo(Barriers.AsSpan((int)NumBarriers));
            NumBarriers += (uint)barriers.Length;
        }

        internal void FlushBarriers()
        {
            if (NumBarriers == 0)
            {
                return;
            }

            fixed (D3D12_RESOURCE_BARRIER* pBarriers = Barriers)
            {
                List->ResourceBarrier(NumBarriers, pBarriers);
            }

            NumBarriers = 0;
        }

        internal CopyContext(in ContextParams @params) : base(@params)
        {

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
        /// Transitions a <see cref="Texture"/> for use on a different <see cref="ExecutionContext"/>
        /// </summary>
        /// <param name="tex">The <see cref="Texture"/> to transition</param>
        /// <param name="subresource">The subresource to transition, by default, all subresources</param>
        public void TransitionForCrossContextAccess(in Texture tex, uint subresource = uint.MaxValue)
        {
            ResourceTransition(tex, ResourceState.Common, subresource);
        }

        /// <summary>
        /// Transitions a <see cref="Buffer"/> for use on a different <see cref="ExecutionContext"/>
        /// </summary>
        /// <param name="tex">The <see cref="Buffer"/> to transition</param>
        public void TransitionForCrossContextAccess(in Buffer tex)
        {
            ResourceTransition(tex, ResourceState.Common);
        }

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

            List->CopyTextureRegion(&destDesc, 0, 0, 0, &sourceDesc, null);
        }





#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public void CopyBufferRegion(in BufferRegion source, in BufferRegion dest)
            => CopyBufferRegion(source.Buffer, source.Offset, dest.Buffer, dest.Offset, source.Length);

        public void CopyBufferRegion(in Buffer source, in BufferRegion dest)
            => CopyBufferRegion(source, 0, dest.Buffer, dest.Offset, source.Length);

        public void CopyBufferRegion(in BufferRegion source, in Buffer dest)
            => CopyBufferRegion(source.Buffer, source.Offset, dest, 0, source.Length);

        public void CopyBufferRegion(in Buffer source, uint sourceOffset, in Buffer dest, uint destOffset, uint numBytes)
            => List->CopyBufferRegion(dest.GetResourcePointer(), destOffset, source.GetResourcePointer(), sourceOffset, numBytes);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

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
            Device.GetCopyableFootprint(source, subresourceIndex, 1, out var layout, out var row, out var numRow, out var size);

            Debug.Assert(dest.Length >= size);

            var sourceDesc = new D3D12_TEXTURE_COPY_LOCATION(source.GetResourcePointer(), subresourceIndex);
            var destDesc = new D3D12_TEXTURE_COPY_LOCATION(dest.GetResourcePointer(), layout);

            FlushBarriers();
            List->CopyTextureRegion(&destDesc, 0, 0, 0, &sourceDesc, null);
        }

        /// <summary>
        /// Copy a subresource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="subresourceIndex">The index of the subresource to copy from</param>
        /// <param name="dest"></param>
        /// <param name="layout"></param>
        public void CopySubresource(in Texture source, uint subresourceIndex, out Buffer dest, out SubresourceLayout layout)
        {
            Device.GetCopyableFootprint(source, subresourceIndex, 1, out var d3d12Layout, out var numRows, out var rowSize, out var size);
            dest = Device.Allocator.AllocateBuffer((long)size, MemoryAccess.CpuReadback, ResourceState.CopyDestination);

            var sourceDesc = new D3D12_TEXTURE_COPY_LOCATION(source.GetResourcePointer(), subresourceIndex);
            var destDesc = new D3D12_TEXTURE_COPY_LOCATION(dest.GetResourcePointer(), d3d12Layout);

            layout = new SubresourceLayout { NumRows = numRows, RowSize = rowSize };

            FlushBarriers();
            List->CopyTextureRegion(&destDesc, 0, 0, 0, &sourceDesc, null);
        }

        /// <summary>
        /// Copy a subresource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="subresourceIndex">The index of the subresource to copy from</param>
        /// <param name="data"></param>
        public void CopySubresource(in Texture source, uint subresourceIndex, in Buffer data)
        {
            //ResourceTransition(source, ResourceState.CopySource, subresourceIndex);
            //ResourceTransition(data, ResourceState.CopyDestination, 0);

            Device.GetCopyableFootprint(source, subresourceIndex, 1, out _, out _, out var rowSize, out var size);

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

            FlushBarriers();
            List->CopyTextureRegion(&destDesc, 0, 0, 0, &sourceDesc, null);
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

            FlushBarriers();
            List->CopyResource(dest.Resource.GetResourcePointer(), source.Resource.GetResourcePointer());
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

            FlushBarriers();
            List->CopyResource(dest.Resource.GetResourcePointer(), source.Resource.GetResourcePointer());
        }


        /// <summary>
        /// Mark a resource barrierson the command list
        /// </summary>
        /// <param name="barrier">The barrier</param>
        public void ResourceBarrier(in ResourceBarrier barrier)
        {
            AddBarrier(barrier.Barrier);
        }

        /// <summary>
        /// Mark a set of resource barriers on the command list
        /// </summary>
        /// <param name="barriers">The barriers</param>
        public void ResourceBarrier(ReadOnlySpan<ResourceBarrier> barriers)
        {
            AddBarriers(MemoryMarshal.Cast<ResourceBarrier, D3D12_RESOURCE_BARRIER>(barriers));
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

            AddBarrier(barrier);
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

            AddBarrier(barrier);
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

            AddBarrier(barrier);
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            FlushBarriers();
            base.Dispose();
        }
    }
}
