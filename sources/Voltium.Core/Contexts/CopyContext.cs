using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.CommandBuffer;
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
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class IllegalRenderPassMethodAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class IllegalBundleMethodAttribute : Attribute { }

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

        public void FlushBarriers() { }

        public CopyContext(GraphicsDevice device) : base(device)
        {

        }
        
        /// <summary>
        /// Writes a 32-bit value to GPU accessible memory
        /// </summary>
        /// <param name="address">The GPU address to write to</param>
        /// <param name="value">The 32 bit value to write to memory</param>
        /// <param name="mode">The <see cref="WriteBufferImmediateMode"/> mode to write with. By default, this is <see cref="WriteBufferImmediateMode.Default"/></param>
        [IllegalBundleMethod]
        public void WriteBufferImmediate(ulong address, uint value, WriteBufferImmediateMode mode = WriteBufferImmediateMode.Default)
        {
            var command = new CommandWriteConstants
            {
                Count = 1
            };

            var param = new WriteConstantParameters
            {
                Address = address,
                Value = value
            };

            _encoder.EmitVariable(&command, &param, &mode, 1);
        }


        /// <summary>
        /// Writes a 32-bit value to GPU accessible memory
        /// </summary>
        /// <param name="pairs">The GPU address and value pairs to write</param>
        /// <param name="modes">The <see cref="WriteBufferImmediateMode"/> modes to write with. By default, this is <see cref="WriteBufferImmediateMode.Default"/>.
        /// If <see cref="ReadOnlySpan{T}.Empty"/> is passed, <see cref="WriteBufferImmediateMode.Default"/> is used.</param>
        [IllegalBundleMethod]
        public void WriteBufferImmediate(ReadOnlySpan<(ulong Address, uint Value)> pairs, ReadOnlySpan<WriteBufferImmediateMode> modes = default)
        {
            if (!modes.IsEmpty && modes.Length != pairs.Length)
            {
                ThrowHelper.ThrowArgumentException(nameof(modes));
            }


            fixed ((ulong Address, uint Value)* pParams = pairs)
            fixed (WriteBufferImmediateMode* pModes = modes)
            {
                var command = new CommandWriteConstants
                {
                    Count = (uint)pairs.Length
                };

                _encoder.EmitVariable(&command, pParams, pModes, (uint)command.Count);
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
        [IllegalBundleMethod]
        public void BeginConditionalRendering(bool predicate, [RequiresResourceState(ResourceState.Predication)] in Buffer buff, uint offset = 0)
        {
            var command = new CommandBeginConditionalRendering
            {
                Buffer = buff.Handle,
                Offset = offset,
                Predicate = predicate
            };

            _encoder.Emit(&command);
        }

        /// <summary>
        /// Ends predication for subsequent operations
        /// </summary>
        [IllegalBundleMethod]
        public void EndConditionalRendering()
        {
            _encoder.EmitEmpty(CommandType.EndConditionalRendering);
        }

        public ref struct Query
        {
            internal CopyContext _context;
            internal QueryHeap _queryHeap;
            internal QueryType _query;
            internal uint _index;

            public void EndQuery() => Dispose();
            public void Dispose()
            {
                _context.EndQuery(_queryHeap, _query, _index);
            }
        }

        [IllegalBundleMethod]
        public Query ScopedQuery<TQuery>(in QueryHeap heap, uint index) where TQuery : struct, IQueryType
            => ScopedQuery(heap, default(TQuery).Type, index);

        [IllegalBundleMethod]
        public Query ScopedQuery(in QueryHeap heap, QueryType type, uint index)
        {
            BeginQuery(heap, type, index);
            return new() { _context = this, _queryHeap = heap, _index = index, _query = type };
        }

        [IllegalBundleMethod]
        public void BeginQuery(in QueryHeap heap, QueryType type, uint index)
        {
            var command = new CommandBeginQuery
            {
                QueryHeap = heap.Handle,
                QueryType = type,
                Index = index
            };

            _encoder.Emit(&command);
        }

        [IllegalBundleMethod]
        public void EndQuery(in QueryHeap heap, QueryType type, uint index)
        {
            var command = new CommandEndQuery
            {
                QueryHeap = heap.Handle,
                QueryType = type,
                Index = index
            };

            _encoder.Emit(&command);
        }

        [IllegalBundleMethod]
        public void QueryTimestamp(in QueryHeap heap, uint index)
        {
            var command = new CommandReadTimestamp
            {
                QueryHeap = heap.Handle,
                Index = index
            };

            _encoder.Emit(&command);
        }

        [IllegalBundleMethod]
        public void ResolveQuery<TQuery>(in QueryHeap heap, Range queries, [RequiresResourceState(ResourceState.CopyDestination)] in Buffer dest, uint offset = 0) where TQuery : struct, IQueryType
            => ResolveQuery(heap, default(TQuery).Type, queries, dest, offset);

        [IllegalBundleMethod]
        public void ResolveQuery(in QueryHeap heap, QueryType type, Range queries, [RequiresResourceState(ResourceState.CopyDestination)] in Buffer dest, uint offset = 0)
        {
            FlushBarriers();

            var command = new CommandResolveQuery
            {
                QueryHeap = heap.Handle,
                QueryType = type,
                Queries = queries,
                Dest = dest.Handle,
                Offset = offset
            };

            _encoder.Emit(&command);
        }

        /// <summary>
        /// Transitions a <see cref="Texture"/> for use on a different <see cref="ExecutionContext"/>
        /// </summary>
        /// <param name="tex">The <see cref="Texture"/> to transition</param>
        /// <param name="current">The current <see cref="ResourceState"/> of <paramref name="tex"/></param>
        /// <param name="subresource">The subresource to transition, by default, all subresources</param>
        [IllegalBundleMethod]
        public void TransitionForCrossContextAccess([RequiresResourceState("current")] in Texture tex, ResourceState current, uint subresource = uint.MaxValue)
        {
            Barrier(ResourceBarrier.Transition(tex, current, ResourceState.Common, subresource));
        }

        /// <summary>
        /// Transitions a <see cref="Buffer"/> for use on a different <see cref="ExecutionContext"/>
        /// </summary>
        /// <param name="buffer">The <see cref="Buffer"/> to transition</param>
        /// <param name="current">The current <see cref="ResourceState"/> of <paramref name="buffer"/></param>
        [IllegalBundleMethod]
        public void TransitionForCrossContextAccess([RequiresResourceState("current")] in Buffer buffer, ResourceState current)
        {
            Barrier(ResourceBarrier.Transition(buffer, current, ResourceState.Common));
        }



        public struct BufferFootprint
        {
            internal D3D12_SUBRESOURCE_FOOTPRINT Footprint;

            public DataFormat Format { get => (DataFormat)Footprint.Format; set => Footprint.Format = (DXGI_FORMAT)value; }
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
        [IllegalBundleMethod, IllegalRenderPassMethod]
        public void CopyBufferToTexture(
            [RequiresResourceState(ResourceState.CopySource)] in Buffer source,
            [RequiresResourceState(ResourceState.CopyDestination)] in Texture dest,
            uint subresource = 0
        )
        {
        }

        /// <summary>
        /// Copy a subresource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="dest">The resource to copy to</param>
        /// <param name="subresource">The index of the subresource to copy</param>
        [IllegalBundleMethod, IllegalRenderPassMethod]
        public void CopyTextureToBuffer(
            [RequiresResourceState(ResourceState.CopySource)] in Texture source,
            [RequiresResourceState(ResourceState.CopyDestination)] in Buffer dest,
            uint subresource = 0
        )
        {
        }

        /// <summary>
        /// Copy a subresource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="dest">The resource to copy to</param>
        /// <param name="subresource">The index of the subresource to copy</param>
        [IllegalBundleMethod, IllegalRenderPassMethod]
        public void CopySubresource(
            [RequiresResourceState(ResourceState.CopySource)] in Texture source,
            [RequiresResourceState(ResourceState.CopyDestination)] in Texture dest,
            uint subresource
        ) => CopySubresource(source, dest, subresource, subresource);

        /// <summary>
        /// Copy a subresource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="dest">The resource to copy to</param>
        /// <param name="sourceSubresource">The index of the subresource to copy from</param>
        /// <param name="destSubresource">The index of the subresource to copy to</param>
        [IllegalBundleMethod, IllegalRenderPassMethod]
        public void CopySubresource(
            [RequiresResourceState(ResourceState.CopySource)] in Texture source,
            [RequiresResourceState(ResourceState.CopyDestination)] in Texture dest,
            uint sourceSubresource,
            uint destSubresource
        )
        {
            FlushBarriers();

            var command = new CommandTextureCopy
            {
                Source = source.Handle,
                Dest = dest.Handle,
                SourceSubresource = sourceSubresource,
                DestSubresource = destSubresource
            };

            _encoder.Emit(&command);
        }

        /// <summary>
        /// Copies a region of a buffer
        /// </summary>
        /// <param name="source">The <see cref="Buffer"/> to copy from</param>
        /// <param name="sourceOffset">The offset, in bytes, to start copying from</param>
        /// <param name="dest">The <see cref="Buffer"/> to copy to</param>
        /// <param name="destOffset">The offset, in bytes, to start copying to</param>
        /// <param name="numBytes">The number of bytes to copy</param>
        [IllegalBundleMethod, IllegalRenderPassMethod]
        public void CopyBufferRegion(
            [RequiresResourceState(ResourceState.CopySource)] in Buffer source,
            uint sourceOffset,
            [RequiresResourceState(ResourceState.CopyDestination)] in Buffer dest,
            uint destOffset,
            uint numBytes
        )
        {
            FlushBarriers();

            var command = new CommandBufferCopy
            {
                Source = source.Handle,
                Dest = dest.Handle,
                SourceOffset = sourceOffset,
                DestOffset = destOffset,
                Length = numBytes
            };

            _encoder.Emit(&command);
        }


        /// <summary>
        /// Copies a region of a buffer
        /// </summary>
        /// <param name="source">The <see cref="Buffer"/> to copy from</param>
        /// <param name="dest">The <see cref="Buffer"/> to copy to</param>
        /// <param name="numBytes">The number of bytes to copy</param>
        [IllegalBundleMethod, IllegalRenderPassMethod]
        public void CopyBufferRegion(
            [RequiresResourceState(ResourceState.CopySource)] in Buffer source,
            [RequiresResourceState(ResourceState.CopyDestination)] in Buffer dest,
            int numBytes
        )
            => CopyBufferRegion(source, dest, (uint)numBytes);

        /// <inheritdoc cref="CopyBufferRegion(in Buffer, in Buffer, uint)"/>
        [IllegalBundleMethod, IllegalRenderPassMethod]
        public void CopyBufferRegion(
            [RequiresResourceState(ResourceState.CopySource)] in Buffer source,
            [RequiresResourceState(ResourceState.CopyDestination)] in Buffer dest,
            uint numBytes
        ) => CopyBufferRegion(source, 0, dest, 0, numBytes);

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
                    bool isBeginOnly = barrier.Barrier.Flags.HasFlag(D3D12_RESOURCE_BARRIER_FLAGS.D3D12_RESOURCE_BARRIER_FLAG_BEGIN_ONLY);
                    result.Flags = isBeginOnly ? D3D12_RESOURCE_BARRIER_FLAGS.D3D12_RESOURCE_BARRIER_FLAG_END_ONLY : D3D12_RESOURCE_BARRIER_FLAGS.D3D12_RESOURCE_BARRIER_FLAG_NONE;

                    if (barrier.Barrier.Type == D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_TRANSITION)
                    {
                        ref readonly var transition = ref barrier.Barrier.Transition;

                        var (before, after) = isBeginOnly ? (transition.StateBefore, transition.StateAfter) : (transition.StateAfter, transition.StateBefore);
                        result = D3D12_RESOURCE_BARRIER.InitTransition(transition.pResource, before, after, transition.Subresource);
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
        [IllegalBundleMethod]
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
        [IllegalBundleMethod]
        public ScopedBarrierSet ScopedBarrier(ReadOnlySpan<ResourceBarrier> barriers)
        {
            Barrier(barriers);
            return new(this, barriers);
        }

        /// <summary>
        /// Mark a resource barrierson the command list
        /// </summary>
        /// <param name="barrier">The barrier</param>
        [IllegalBundleMethod]
        public void Barrier(in ResourceBarrier barrier)
        {
            AddBarrier(barrier.Barrier);
        }

        /// <summary>
        /// Mark a set of resource barriers on the command list
        /// </summary>
        /// <param name="barriers">The barriers</param>
        [IllegalBundleMethod]
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
