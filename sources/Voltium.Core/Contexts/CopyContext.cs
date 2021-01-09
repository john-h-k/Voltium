using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using TerraFX.Interop;
using static TerraFX.Interop.Vulkan;
using Voltium.Common;
using Voltium.Core.Contexts;
using Voltium.Core.Devices;
using Voltium.Core.Exceptions;
using Voltium.Core.Memory;
using Voltium.Core.Pool;
using Voltium.Core.Queries;
using Voltium.TextureLoading;
using Buffer = Voltium.Core.Memory.Buffer;


namespace Voltium.Core
{
    using BarrierType =
#if D3D12
        D3D12_RESOURCE_BARRIER;
#else
        VkMemoryBarrier;
#endif

    /// <summary>
    /// The mode used for <see cref="CopyContext.WriteBufferImmediate(ulong, uint, WriteBufferImmediateMode)"/> or <see cref="CopyContext.WriteBufferImmediate(ReadOnlySpan{ValueTuple{ulong, uint}}, ReadOnlySpan{WriteBufferImmediateMode})"/>
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
    /// Represents a context on which GPU commands can be recorded
    /// </summary>
    public unsafe partial class CopyContext : GpuContext
    {
        [FixedBufferType(typeof(BarrierType), 16)]
        private partial struct BarrierBuffer16
        {
        }

        private BarrierBuffer16 Barriers;
        private uint NumBarriers;


        internal void AddBarrier(in BarrierType barrier)
        {
            if (NumBarriers == BarrierBuffer16.BufferLength)
            {
                FlushBarriers();
            }

            Barriers[NumBarriers++] = barrier;
        }

        internal void AddBarriers(ReadOnlySpan<BarrierType> barriers)
        {
            if (barriers.Length == 0)
            {
                return;
            }

            if (barriers.Length > BarrierBuffer16.BufferLength)
            {
                FlushBarriers();
                fixed (BarrierType* pBarriers = barriers)
                {
#if D3D12
                    List->ResourceBarrier((uint)barriers.Length, pBarriers);
#else
                    vkCmdPipelineBarrier()
#endif
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

            fixed (BarrierType* pBarriers = Barriers)
            {
#if D3D12
                List->ResourceBarrier(NumBarriers, pBarriers);
#else
                vkCmdPipelineBarrier(List, )
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
        /// Writes a 32-bit value to GPU accessible memory
        /// </summary>
        /// <param name="address">The GPU address to write to</param>
        /// <param name="value">The 32 bit value to write to memory</param>
        /// <param name="mode">The <see cref="WriteBufferImmediateMode"/> mode to write with. By default, this is <see cref="WriteBufferImmediateMode.Default"/></param>
        public void WriteBufferImmediate(
            ulong address,
            uint value,
            WriteBufferImmediateMode mode = WriteBufferImmediateMode.Default
        )
        {
            var param = new D3D12_WRITEBUFFERIMMEDIATE_PARAMETER {Dest = address, Value = value};

            FlushBarriers();
#if D3D12
            List->WriteBufferImmediate(1, &param, (D3D12_WRITEBUFFERIMMEDIATE_MODE*)&mode);
#else
        }


        /// <summary>
        /// Writes a 32-bit value to GPU accessible memory
        /// </summary>
        /// <param name="pairs">The GPU address and value pairs to write</param>
        /// <param name="modes">The <see cref="WriteBufferImmediateMode"/> modes to write with. By default, this is <see cref="WriteBufferImmediateMode.Default"/>.
        /// If <see cref="ReadOnlySpan{T}.Empty"/> is passed, <see cref="WriteBufferImmediateMode.Default"/> is used.</param>
        public void WriteBufferImmediate(
            ReadOnlySpan<(ulong Address, uint Value)> pairs,
            ReadOnlySpan<WriteBufferImmediateMode> modes = default
        )
        {
            if (!modes.IsEmpty && modes.Length != pairs.Length)
            {
                ThrowHelper.ThrowArgumentException(nameof(modes));
            }


            fixed (void* pParams = pairs)
            fixed (void* pModes = modes)
            {
                FlushBarriers();
                List->WriteBufferImmediate((uint)modes.Length, (D3D12_WRITEBUFFERIMMEDIATE_PARAMETER*)pParams,
                    (D3D12_WRITEBUFFERIMMEDIATE_MODE*)pModes);
            }
        }

        /// <summary>
        /// Sets the predication value for subsequent operations, which may prevent them executing
        /// </summary>
        /// <param name="predicate">The predicate value which indicates the operation <b>should</b> execute. If this is <see langword="true"/>, then
        /// any non-zero value in <paramref name="buff"/> will cause the operation to execute. Else, if it is <see langword="false"/>, then any zero
        /// value in <paramref name="buff"/> will cause the operation to execute.</param>
        /// <param name="buff">The <see cref="Buffer"/> to read the predication value from</param>
        /// <param name="offset">The offset, in bytes, the predication value</param>
        public void SetPredication(
            bool predicate,
            [RequiresResourceState(ResourceState.Predication)] in Buffer buff,
            uint offset = 0
        )
        {
            FlushBarriers();
#if D3D12
            List->SetPredication(buff.GetResourcePointer(), buff.Offset + offset,
                predicate
                    ? D3D12_PREDICATION_OP.D3D12_PREDICATION_OP_NOT_EQUAL_ZERO
                    : D3D12_PREDICATION_OP.D3D12_PREDICATION_OP_EQUAL_ZERO);
#else
            var info = new VkConditionalRenderingBeginInfoEXT
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_CONDITIONAL_RENDERING_BEGIN_INFO_EXT,
                buffer = buff.GetResourcePointer(),
                flags = (uint)(predicate ? 0 : VkConditionalRenderingFlagBitsEXT.VK_CONDITIONAL_RENDERING_INVERTED_BIT_EXT),
                offset = offset
            };

            vkCmdBeginConditionalRenderingEXT(List, &info);
#endif
        }

        public ref struct Query
        {
            internal CopyContext _context;
#if D3D12
            internal ID3D12QueryHeap* _queryHeap;
#else
            internal ulong _queryHeap;
#endif
            internal D3D12_QUERY_TYPE _query;
            internal uint _index;

            public void EndQuery() => Dispose();

            public void Dispose()
            {
#if D3D12
                _context.List->EndQuery(_queryHeap, _query, _index);
#else
                vkCmdEndQuery(_context.List, _queryHeap, _index);
#endif
            }
        }

        public Query ScopedQuery<TQuery>(in QueryHeap heap, uint index) where TQuery : struct, IQueryType
            => ScopedQuery(heap, default(TQuery).Type, index);

        public Query ScopedQuery(in QueryHeap heap, QueryType type, uint index)
        {
            BeginQuery(heap, type, index);
            return new()
            {
                _context = this, _queryHeap = heap.GetQueryHeap(), _index = index, _query = (D3D12_QUERY_TYPE)type
            };
        }

        public void BeginQuery(in QueryHeap heap, QueryType type, uint index)
        {
#if D3D12
            List->BeginQuery(heap.GetQueryHeap(), (D3D12_QUERY_TYPE)type, index);
#else
            vkCmdBeginQuery(List, heap.GetQueryHeap(), index, (uint)(type == QueryType.Occlusion ? VkQueryControlFlagBits.VK_QUERY_CONTROL_PRECISE_BIT : 0));
#endif
        }

        public void EndQuery(in QueryHeap heap, QueryType type, uint index)
        {
#if D3D12
            List->EndQuery(heap.GetQueryHeap(), (D3D12_QUERY_TYPE)type, index);
#else
            vkCmdEndQuery(List, heap.GetQueryHeap(), index);
#endif
        }

        public void QueryTimestamp(in QueryHeap heap, uint index)
        {
#if D3D12
            List->EndQuery(heap.GetQueryHeap(), D3D12_QUERY_TYPE.D3D12_QUERY_TYPE_TIMESTAMP, index);
#else
            vkCmdWriteTimestamp(List, VkPipelineStageFlagBits.VK_PIPELINE_STAGE_ALL_COMMANDS_BIT, heap.GetQueryHeap(), index);
#endif
        }

        public void ResolveQuery<TQuery>(
            in QueryHeap heap,
            Range queries,
            [RequiresResourceState(ResourceState.CopyDestination)]
            in Buffer dest, uint offset = 0
        ) where TQuery : struct, IQueryType
            => ResolveQuery(heap, default(TQuery).Type, queries, dest, offset);

        public void ResolveQuery(
            in QueryHeap heap,
            QueryType type,
            Range queries,
            [RequiresResourceState(ResourceState.CopyDestination)] in Buffer dest,
            uint offset = 0
        )
        {
            FlushBarriers();
            var (heapOffset, length) = queries.GetOffsetAndLength((int)heap.Length);

#if D3D12
            List->ResolveQueryData(heap.GetQueryHeap(), (D3D12_QUERY_TYPE)type, (uint)heapOffset, (uint)length,
                dest.GetResourcePointer(), dest.Offset + offset);
#else
            vkCmdCopyQueryPoolResults(
                List,
                heap.GetQueryHeap(),
                (uint)heapOffset,
                (uint)length,
                dest.GetResourcePointer(),
                offset,
                sizeof(ulong),
                (uint)(VkQueryResultFlagBits.VK_QUERY_RESULT_64_BIT | VkQueryResultFlagBits.VK_QUERY_RESULT_WAIT_BIT)
            );
        }

        /// <summary>
        /// Transitions a <see cref="Texture"/> for use on a different <see cref="ExecutionContext"/>
        /// </summary>
        /// <param name="tex">The <see cref="Texture"/> to transition</param>
        /// <param name="current">The current <see cref="ResourceState"/> of <paramref name="tex"/></param>
        /// <param name="subresource">The subresource to transition, by default, all subresources</param>
        public void TransitionForCrossContextAccess([RequiresResourceState("current")] in Texture tex,
            ResourceState current, uint subresource = uint.MaxValue)
        {
            Barrier(ResourceBarrier.Transition(tex, current, ResourceState.Common, subresource));
        }

        /// <summary>
        /// Transitions a <see cref="Buffer"/> for use on a different <see cref="ExecutionContext"/>
        /// </summary>
        /// <param name="buffer">The <see cref="Buffer"/> to transition</param>
        /// <param name="current">The current <see cref="ResourceState"/> of <paramref name="buffer"/></param>
        public void TransitionForCrossContextAccess([RequiresResourceState("current")] in Buffer buffer,
            ResourceState current)
        {
            Barrier(ResourceBarrier.Transition(buffer, current, ResourceState.Common));
        }


        public struct BufferFootprint
        {
            internal D3D12_SUBRESOURCE_FOOTPRINT Footprint;

            public DataFormat Format
            {
                get => (DataFormat)Footprint.Format;
                set => Footprint.Format = (DXGI_FORMAT)value;
            }

            public uint Width { get => Footprint.Width; set => Footprint.Width = value; }
            public uint Height { get => Footprint.Height; set => Footprint.Height = value; }
            public uint Depth { get => Footprint.Depth; set => Footprint.Depth = value; }
            public uint RowPitch { get => Footprint.RowPitch; set => Footprint.RowPitch = value; }
        }


        /// <summary>
        /// Copy a subresource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="dest">The resource to copy to</param>
        /// <param name="subresource">The index of the subresource to copy</param>
        public void CopyBufferToTexture(
            [RequiresResourceState(ResourceState.CopySource)]
            in Buffer source,
            [RequiresResourceState(ResourceState.CopyDestination)]
            in Texture dest,
            uint subresource = 0
        )
        {
            Device.GetCopyableFootprint(dest, subresource, out var layout, out var numRow, out var rowSize,
                out var size);
#if D3D12
            var src = new D3D12_TEXTURE_COPY_LOCATION(source.GetResourcePointer(), layout);
            var dst = new D3D12_TEXTURE_COPY_LOCATION(dest.GetResourcePointer(), subresource);

            List->CopyTextureRegion(&dst, 0, 0, 0, &src, null);
#else
            var region = new VkBufferImageCopy
            {
                bufferRowLength = (uint)rowSize,
                bufferImageHeight = layout.Footprint.Height,
                
            };


            vkCmdCopyBufferToImage(
                List,
                source.GetResourcePointer(),
                dest.GetResourcePointer(),
                VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
                1,
                &region
            );
        }

        /// <summary>
        /// Copy a subresource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="dest">The resource to copy to</param>
        /// <param name="subresource">The index of the subresource to copy</param>
        public void CopyTextureToBuffer(
            [RequiresResourceState(ResourceState.CopySource)]
            in Texture source,
            [RequiresResourceState(ResourceState.CopyDestination)]
            in Buffer dest,
            uint subresource = 0
        )
        {
            Device.GetCopyableFootprint(source, subresource, out var layout, out var numRow, out var rowSize, out var size);
#if D3D12
            var dst = new D3D12_TEXTURE_COPY_LOCATION(source.GetResourcePointer(), layout);
            var src = new D3D12_TEXTURE_COPY_LOCATION(dest.GetResourcePointer(), subresource);

            List->CopyTextureRegion(&dst, 0, 0, 0, &src, null);
#else
            var region = new VkBufferImageCopy
            {
                imageOffset = default,
                imageExtent = new VkExtent3D
                {
                    width = (uint)source.Width,
                    height = source.Height,
                    depth = source.Height,
                }
            };

            vkCmdCopyImageToBuffer(List, source.GetResourcePointer(), VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL, dest.GetResourcePointer(), 1, &region);
#endif
        }

        /// <summary>
        /// Copy a subresource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="dest">The resource to copy to</param>
        /// <param name="subresource">The index of the subresource to copy</param>
        public void CopySubresource(
            [RequiresResourceState(ResourceState.CopySource)]
            in Texture source,
            [RequiresResourceState(ResourceState.CopyDestination)]
            in Texture dest,
            uint subresource
        ) => CopySubresource(source, dest, subresource, subresource);

        /// <summary>
        /// Copy a subresource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="dest">The resource to copy to</param>
        /// <param name="sourceSubresource">The index of the subresource to copy from</param>
        /// <param name="destSubresource">The index of the subresource to copy to</param>
        public void CopySubresource(
            [RequiresResourceState(ResourceState.CopySource)]
            in Texture source,
            [RequiresResourceState(ResourceState.CopyDestination)]
            in Texture dest,
            uint sourceSubresource,
            uint destSubresource
        )
        {
#if D3D12
            Unsafe.SkipInit(out D3D12_TEXTURE_COPY_LOCATION sourceDesc);
            Unsafe.SkipInit(out D3D12_TEXTURE_COPY_LOCATION destDesc);

            sourceDesc.pResource = source.GetResourcePointer();
            sourceDesc.Type = D3D12_TEXTURE_COPY_TYPE.D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
            sourceDesc.SubresourceIndex = sourceSubresource;

            destDesc.pResource = dest.GetResourcePointer();
            destDesc.Type = D3D12_TEXTURE_COPY_TYPE.D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
            destDesc.SubresourceIndex = destSubresource;

            FlushBarriers();
            List->CopyTextureRegion(&destDesc, 0, 0, 0, &sourceDesc, null);
#else

            Windows.D3D12DecomposeSubresource(sourceSubresource, source.MipCount, source.IsArray ? source.DepthOrArraySize : 1, out var sMip, out var sArr, out var sPlane);
            Windows.D3D12DecomposeSubresource(destSubresource, dest.MipCount, dest.IsArray ? dest.DepthOrArraySize : 1, out var dMip, out var dArr, out var dPlane);
            var aspectBits = 0u;
            var region = new VkImageCopy
            {
                srcSubresource = new VkImageSubresourceLayers
                {
                    aspectMask = aspectBits,
                    baseArrayLayer = sArr,
                    layerCount = 1,
                    mipLevel = sMip
                },

                dstSubresource = new VkImageSubresourceLayers
                {
                    aspectMask = aspectBits,
                    baseArrayLayer = dArr,
                    layerCount = 1,
                    mipLevel = dMip
                },
                extent = new VkExtent3D
                {
                    width = (uint)source.Width,
                    height = source.Height,
                    depth = source.IsArray ? 1 : source.DepthOrArraySize,
                }
            };

            vkCmdCopyImage(
                List,
                source.GetResourcePointer(),
                VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
                dest.GetResourcePointer(),
                VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
                1,
                &region
            );
#endif
        }

        /// <summary>
        /// Copies a region of a buffer
        /// </summary>
        /// <param name="source">The <see cref="Buffer"/> to copy from</param>
        /// <param name="sourceOffset">The offset, in bytes, to start copying from</param>
        /// <param name="dest">The <see cref="Buffer"/> to copy to</param>
        /// <param name="destOffset">The offset, in bytes, to start copying to</param>
        /// <param name="numBytes">The number of bytes to copy</param>
        public void CopyBufferRegion(
            [RequiresResourceState(ResourceState.CopySource)]
            in Buffer source,
            uint sourceOffset,
            [RequiresResourceState(ResourceState.CopyDestination)]
            in Buffer dest,
            uint destOffset,
            uint numBytes
        )
        {
            FlushBarriers();
#if D3D12
            List->CopyBufferRegion(dest.GetResourcePointer(), dest.Offset + destOffset, source.GetResourcePointer(),
                source.Offset + sourceOffset, numBytes);
#else
            var copy = new VkBufferCopy
            {
                srcOffset = sourceOffset,
                dstOffset = destOffset,
                size = numBytes
            };
            vkCmdCopyBuffer(List, source.GetResourcePointer(), dest.GetResourcePointer(), 1, &copy);
#endif
        }


        /// <summary>
        /// Copies a region of a buffer
        /// </summary>
        /// <param name="source">The <see cref="Buffer"/> to copy from</param>
        /// <param name="dest">The <see cref="Buffer"/> to copy to</param>
        /// <param name="numBytes">The number of bytes to copy</param>
        public void CopyBufferRegion(
            [RequiresResourceState(ResourceState.CopySource)]
            in Buffer source,
            [RequiresResourceState(ResourceState.CopyDestination)]
            in Buffer dest,
            int numBytes
        )
            => CopyBufferRegion(source, dest, (uint)numBytes);

        /// <inheritdoc cref="CopyBufferRegion(in Buffer, in Buffer, uint)"/>
        public void CopyBufferRegion(
            [RequiresResourceState(ResourceState.CopySource)]
            in Buffer source,
            [RequiresResourceState(ResourceState.CopyDestination)]
            in Buffer dest,
            uint numBytes
        )
        {
            FlushBarriers();
#if D3D12
            List->CopyBufferRegion(dest.GetResourcePointer(), dest.Offset, source.GetResourcePointer(), source.Offset,
                numBytes);
#else
            var copy = new VkBufferCopy
            {
                srcOffset = 0,
                dstOffset = 0,
                size = numBytes
            };
            vkCmdCopyBuffer(List, source.GetResourcePointer(), dest.GetResourcePointer(), 1, &copy);
#endif
        }

        /// <summary>
        /// Copy an entire resource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="dest">The resource to copy to</param>
        public void CopyResource(
            [RequiresResourceState(ResourceState.CopySource)]
            in Buffer source,
            [RequiresResourceState(ResourceState.CopyDestination)]
            in Buffer dest
        )
        {
            if (IsWholeResource(source) && IsWholeResource(dest))
            {
                FlushBarriers();
#if D3D12
                List->CopyResource(dest.Resource.GetResourcePointer(), source.Resource.GetResourcePointer());
#else
                vkCmdCopyBuffer(List, source.GetResourcePointer(), dest.GetResourcePointer(), 0, null);
#endif
            }
            else
            {
                if (source.Length != dest.Length)
                {
                    throw new GraphicsException(Device, "Copy Resource buffers must be sized identically");
                }

                CopyBufferRegion(source, dest, source.Length);
            }

            static bool IsWholeResource(in Buffer buff) =>
#if D3D12
                buff.Offset == 0 && buff.GetResourcePointer()->GetDesc().Width == buff.Length;
#else
                true;
#endif
        }

        /// <summary>
        /// Copy an entire resource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="dest">The resource to copy to</param>
        public void CopyResource(
            [RequiresResourceState(ResourceState.CopySource)]
            in Texture source,
            [RequiresResourceState(ResourceState.CopyDestination)]
            in Texture dest
        )
        {
            FlushBarriers();
#if D3D12
            List->CopyResource(dest.GetResourcePointer(), source.GetResourcePointer());
#else
            var aspectBits = 0u;
            var region = new VkImageCopy
            {
                srcSubresource = new VkImageSubresourceLayers
                {
                    aspectMask = aspectBits,
                    baseArrayLayer = 0,
                    layerCount = 1,
                    mipLevel = sMip
                },

                dstSubresource = new VkImageSubresourceLayers
                {
                    aspectMask = aspectBits,
                    baseArrayLayer = dArr,
                    layerCount = 1,
                    mipLevel = dMip
                },
                extent = new VkExtent3D
                {
                    width = (uint)source.Width,
                    height = source.Height,
                    depth = source.IsArray ? 1 : source.DepthOrArraySize,
                }
            };

            vkCmdCopyImage(
                List,
                source.GetResourcePointer(),
                VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
                dest.GetResourcePointer(),
                VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
                1,
                &region
            );
        }

        //public ref struct SplitBarrierSet
        //{
        //    private CopyContext _context;
        //    private ReadOnlySpan<ResourceBarrier> _barriers;

        //    internal SplitBarrierSet(CopyContext context, ReadOnlySpan<ResourceBarrier> barriers)
        //    {
        //        _context = context;
        //        _barriers = barriers;
        //    }

        //    public void Dispose()
        //    {
        //        int newBarrierCount = 0;

        //        Span<D3D12_RESOURCE_BARRIER> newBarriers = default;
        //        using RentedArray<D3D12_RESOURCE_BARRIER> rent = default;

        //        if (StackSentinel.SafeToStackalloc<D3D12_RESOURCE_BARRIER>(_barriers.Length))
        //        {
        //            // avoid stupid stackalloc assignment rules
        //            var p = stackalloc D3D12_RESOURCE_BARRIER[_barriers.Length];
        //            newBarriers = new(p, _barriers.Length);
        //        }
        //        else
        //        {
        //            Unsafe.AsRef(in rent) = RentedArray<D3D12_RESOURCE_BARRIER>.Create(_barriers.Length);
        //            newBarriers = rent.AsSpan();
        //        }

        //        foreach (ref readonly var barrier in _barriers)
        //        {
        //            if (barrier.Barrier.Type == D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_TRANSITION)
        //            {
        //                ref readonly var transition = ref barrier.Barrier.Transition;
        //                if (barrier.Barrier.Flags.HasFlag(D3D12_RESOURCE_BARRIER_FLAGS.D3D12_RESOURCE_BARRIER_FLAG_BEGIN_ONLY))
        //                {
        //                    newBarriers[newBarrierCount++] = D3D12_RESOURCE_BARRIER.InitTransition(transition.pResource, transition.StateBefore, transition.StateAfter, transition.Subresource, D3D12_RESOURCE_BARRIER_FLAGS.D3D12_RESOURCE_BARRIER_FLAG_END_ONLY);
        //                }
        //            }
        //        }

        //        _context.AddBarriers(newBarriers[..newBarrierCount]);
        //    }
        //}

        /// <summary>
        /// Describes a set of barriers scoped over a certain region, created by <see cref="ScopedBarrier(ReadOnlySpan{ResourceBarrier})"/>
        /// </summary>
        public ref struct ScopedBarrierSet
        {
            private CopyContext _context;
            private ResourceBarrier? _single;
            private ReadOnlySpan<ResourceBarrier> _barriers;


            internal ScopedBarrierSet(CopyContext context, in ResourceBarrier barrier)
            {
                _context = context;
                _barriers = default;
                _single = barrier;
            }

            internal ScopedBarrierSet(CopyContext context, ReadOnlySpan<ResourceBarrier> barriers)
            {
                _context = context;
                _barriers = barriers;
                _single = null;
            }

            /// <summary>
            /// Ends the scoped barriers, by reversing all state transitions,
            /// reversing all aliasing barriers,
            /// and reperforming any UAV barriers
            /// </summary>
            public void Dispose()
            {
                if (_single is ResourceBarrier single)
                {
                    _context.AddBarrier(Reverse(single));
                    return;
                }

                int newBarrierCount = 0;

                Span<D3D12_RESOURCE_BARRIER> newBarriers = default;
                using RentedArray<D3D12_RESOURCE_BARRIER> rent = default;

                if (StackSentinel.SafeToStackalloc<D3D12_RESOURCE_BARRIER>(_barriers.Length))
                {
                    // avoid stupid stackalloc assignment rules
                    var p = stackalloc D3D12_RESOURCE_BARRIER[_barriers.Length];
                    newBarriers = new(p, _barriers.Length);
                }
                else
                {
                    Unsafe.AsRef(in rent) = RentedArray<D3D12_RESOURCE_BARRIER>.Create(_barriers.Length);
                    newBarriers = rent.AsSpan();
                }

                foreach (ref readonly var barrier in _barriers)
                {
                    newBarriers[newBarrierCount++] = Reverse(barrier);
                }

                _context.AddBarriers(newBarriers[0..newBarrierCount]);

                static D3D12_RESOURCE_BARRIER Reverse(in ResourceBarrier barrier)
                {
                    D3D12_RESOURCE_BARRIER result;
                    bool isBeginOnly =
                        barrier.Barrier.Flags.HasFlag(D3D12_RESOURCE_BARRIER_FLAGS
                            .D3D12_RESOURCE_BARRIER_FLAG_BEGIN_ONLY);
                    result.Flags = isBeginOnly
                        ? D3D12_RESOURCE_BARRIER_FLAGS.D3D12_RESOURCE_BARRIER_FLAG_END_ONLY
                        : D3D12_RESOURCE_BARRIER_FLAGS.D3D12_RESOURCE_BARRIER_FLAG_NONE;

                    if (barrier.Barrier.Type == D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_TRANSITION)
                    {
                        ref readonly var transition = ref barrier.Barrier.Transition;

                        var (before, after) = isBeginOnly
                            ? (transition.StateBefore, transition.StateAfter)
                            : (transition.StateAfter, transition.StateBefore);
                        result = D3D12_RESOURCE_BARRIER.InitTransition(transition.pResource, before, after,
                            transition.Subresource);
                    }
                    else if (barrier.Barrier.Type == D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_ALIASING)
                    {
                        ref readonly var aliasing = ref barrier.Barrier.Aliasing;
                        ID3D12Resource* before, after;
                        if (isBeginOnly)
                        {
                            before = aliasing.pResourceBefore;
                            after = aliasing.pResourceAfter;
                        }
                        else
                        {
                            before = aliasing.pResourceAfter;
                            after = aliasing.pResourceBefore;
                        }

                        result = D3D12_RESOURCE_BARRIER.InitAliasing(before, after);
                    }
                    else /* D3D12_RESOURCE_BARRIER_TYPE_UAV */
                    {
                        result = D3D12_RESOURCE_BARRIER.InitUAV(barrier.Barrier.UAV.pResource);
                    }

                    return result;
                }
            }
        }

        /// <summary>
        /// Begins a set of scoped barriers which will be reversed when <see cref="ScopedBarrierSet.Dispose"/> is called
        /// </summary>
        /// <param name="barrier">The <see cref="ResourceBarrier"/> to perform and reverse</param>
        /// <returns>A new <see cref="ScopedBarrierSet"/></returns>
        public ScopedBarrierSet ScopedBarrier(in ResourceBarrier barrier)
        {
            Barrier(barrier);
            return new(this, barrier);
        }


        /// <summary>
        /// Begins a set of scoped barriers which will be reversed when <see cref="ScopedBarrierSet.Dispose"/> is called
        /// </summary>
        /// <param name="barriers">The <see cref="ResourceBarrier"/>s to perform and reverse</param>
        /// <returns>A new <see cref="ScopedBarrierSet"/></returns>
        public ScopedBarrierSet ScopedBarrier(ReadOnlySpan<ResourceBarrier> barriers)
        {
            Barrier(barriers);
            return new(this, barriers);
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


