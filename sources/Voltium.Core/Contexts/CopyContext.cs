using System;
using System.Collections.Generic;
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


        //public record WriteBufferParams(uint Value, ulong Address);

        /// <summary>
        /// The mode used for <see cref="WriteBufferImmediate(ulong, uint, WriteBufferImmediateMode)"/> or <see cref="WriteBufferImmediate(ReadOnlySpan{ValueTuple{ulong, uint}}, ReadOnlySpan{WriteBufferImmediateMode})"/>
        /// </summary>
        public enum WriteBufferImmediateMode
        {
            /// <summary>
            /// The same ordering occurs as with a standard copy operation
            /// </summary>
            Default = D3D12_WRITEBUFFERIMMEDIATE_MODE.D3D12_WRITEBUFFERIMMEDIATE_MODE_DEFAULT,

            /// <summary>
            /// The write is guaranteed to occur after all previous commands have BEGAN execution on the GPU
            /// </summary>
            In = D3D12_WRITEBUFFERIMMEDIATE_MODE.D3D12_WRITEBUFFERIMMEDIATE_MODE_MARKER_IN,

            /// <summary>
            /// The write is guaranteed to occur after all previous commands have COMPLETED execution on the GPU
            /// </summary>
            Out = D3D12_WRITEBUFFERIMMEDIATE_MODE.D3D12_WRITEBUFFERIMMEDIATE_MODE_MARKER_OUT
        }

        /// <summary>
        /// Writes a 32-bit value to GPU accessible memory
        /// </summary>
        /// <param name="address">The GPU address to write to</param>
        /// <param name="value">The 32 bit value to write to memory</param>
        /// <param name="mode">The <see cref="WriteBufferImmediateMode"/> mode to write with. By default, this is <see cref="WriteBufferImmediateMode.Default"/></param>
        public void WriteBufferImmediate(ulong address, uint value, WriteBufferImmediateMode mode = WriteBufferImmediateMode.Default)
        {
            var param = new D3D12_WRITEBUFFERIMMEDIATE_PARAMETER
            {
                Dest = address,
                Value = value
            };

            FlushBarriers();
            List->WriteBufferImmediate(1, &param, (D3D12_WRITEBUFFERIMMEDIATE_MODE*)&mode);
        }


        /// <summary>
        /// Writes a 32-bit value to GPU accessible memory
        /// </summary>
        /// <param name="pairs">The GPU address and value pairs to write</param>
        /// <param name="modes">The <see cref="WriteBufferImmediateMode"/> modes to write with. By default, this is <see cref="WriteBufferImmediateMode.Default"/>.
        /// If <see cref="ReadOnlySpan{T}.Empty"/> is passed, <see cref="WriteBufferImmediateMode.Default"/> is used.</param>
        public void WriteBufferImmediate(ReadOnlySpan<(ulong Address, uint Value)> pairs, ReadOnlySpan<WriteBufferImmediateMode> modes = default)
        {
            if (!modes.IsEmpty && modes.Length != pairs.Length)
            {
                ThrowHelper.ThrowArgumentException(nameof(modes));
            }


            fixed (void* pParams = pairs)
            fixed (void* pModes = modes)
            {
                FlushBarriers();
                List->WriteBufferImmediate((uint)modes.Length, (D3D12_WRITEBUFFERIMMEDIATE_PARAMETER*)pParams, (D3D12_WRITEBUFFERIMMEDIATE_MODE*)pModes);
            }
        }

        /// <summary>
        /// Transitions a <see cref="Texture"/> for use on a different <see cref="ExecutionContext"/>
        /// </summary>
        /// <param name="tex">The <see cref="Texture"/> to transition</param>
        /// <param name="current">The current <see cref="ResourceState"/> of <paramref name="tex"/></param>
        /// <param name="subresource">The subresource to transition, by default, all subresources</param>
        public void TransitionForCrossContextAccess(in Texture tex, ResourceState current, uint subresource = uint.MaxValue)
        {
            Barrier(ResourceBarrier.Transition(tex, current, ResourceState.Common, subresource));
        }

        /// <summary>
        /// Transitions a <see cref="Buffer"/> for use on a different <see cref="ExecutionContext"/>
        /// </summary>
        /// <param name="buffer">The <see cref="Buffer"/> to transition</param>
        /// <param name="current">The current <see cref="ResourceState"/> of <paramref name="buffer"/></param>
        public void TransitionForCrossContextAccess(in Buffer buffer, ResourceState current)
        {
            Barrier(ResourceBarrier.Transition(buffer, current, ResourceState.Common));
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
            Unsafe.SkipInit(out D3D12_TEXTURE_COPY_LOCATION sourceDesc);
            Unsafe.SkipInit(out D3D12_TEXTURE_COPY_LOCATION destDesc);

            sourceDesc.pResource = source.GetResourcePointer();
            sourceDesc.Type = D3D12_TEXTURE_COPY_TYPE.D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
            sourceDesc.Anonymous.SubresourceIndex = sourceSubresource;

            destDesc.pResource = dest.GetResourcePointer();
            destDesc.Type = D3D12_TEXTURE_COPY_TYPE.D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
            destDesc.Anonymous.SubresourceIndex = destSubresource;

            FlushBarriers();
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
        {
            FlushBarriers();
            List->CopyBufferRegion(dest.GetResourcePointer(), destOffset, source.GetResourcePointer(), sourceOffset, numBytes);
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Copy a subresource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="dest">The resource to copy to</param>
        /// <param name="subresourceIndex">The index of the subresource to copy from</param>
        public void CopySubresource(in Texture source, in Buffer dest, uint subresourceIndex = 0)
        {
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
        public void Barrier(in ResourceBarrier barrier)
        {
            AddBarrier(barrier.Barrier);
        }

        /// <summary>
        /// Mark a set of resource barriers on the command list
        /// </summary>
        /// <param name="barriers">The barriers</param>
        public void Barrier(ReadOnlySpan<ResourceBarrier> barriers)
        {
            AddBarriers(MemoryMarshal.Cast<ResourceBarrier, D3D12_RESOURCE_BARRIER>(barriers));
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            FlushBarriers();
            base.Dispose();
        }
    }
}
