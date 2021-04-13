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
using Voltium.Core.Contexts;
using Voltium.Core.Devices;
using Voltium.Core.Exceptions;
using Voltium.Core.Memory;
using Voltium.Core.Queries;
using Voltium.TextureLoading;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core
{
    /// <summary>
    /// Indicates the given method is illegal within a render pass
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class IllegalRenderPassMethodAttribute : Attribute { }
    /// <summary>
    /// Indicates the given method is illegal within a bundle
    /// </summary>
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
        private protected void FlushBarriers() { }

        /// <summary>
        /// Creates a new <see cref="CopyContext"/>
        /// </summary>
        public CopyContext() : base()
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


            fixed ((ulong Address, uint Value) * pParams = pairs)
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

        /// <summary>
        /// Represents a scoped query
        /// </summary>
        public ref struct Query
        {
            internal CopyContext _context;
            internal QuerySet _queryHeap;
            internal QueryType _query;
            internal uint _index;

            /// <summary>
            /// Ends the scoped query
            /// </summary>
            public void Dispose()
            {
                _context.EndQuery(_queryHeap, _query, _index);
            }
        }

        /// <summary>
        /// Begins a scoped <see cref="Query"/>
        /// </summary>
        /// <typeparam name="TQuery">The type of the query to begin</typeparam>
        /// <param name="heap">The <see cref="QuerySet"/> this query is within</param>
        /// <param name="index">The index within <paramref name="heap"/> that this query is</param>
        /// <returns>A new <see cref="Query"/> which can be disposed to end the scoped query</returns>
        [IllegalBundleMethod]
        public Query ScopedQuery<TQuery>(in QuerySet heap, uint index) where TQuery : struct, IQueryType
            => ScopedQuery(heap, default(TQuery).Type, index);

        /// <summary>
        /// Begins a scoped <see cref="Query"/>
        /// </summary>
        /// <param name="heap">The <see cref="QuerySet"/> this query is within</param>
        /// <param name="type">The type of the query to begin</param>
        /// <param name="index">The index within <paramref name="heap"/> that this query is</param>
        /// <returns>A new <see cref="Query"/> which can be disposed to end the scoped query</returns>
        [IllegalBundleMethod]
        public Query ScopedQuery(in QuerySet heap, QueryType type, uint index)
        {
            BeginQuery(heap, type, index);
            return new() { _context = this, _queryHeap = heap, _index = index, _query = type };
        }

        /// <summary>
        /// Begins a query
        /// </summary>
        /// <param name="heap">The <see cref="QuerySet"/> this query is within</param>
        /// <param name="type">The type of the query to begin</param>
        /// <param name="index">The index within <paramref name="heap"/> that this query is</param>
        [IllegalBundleMethod]
        public void BeginQuery(in QuerySet heap, QueryType type, uint index)
        {
            var command = new CommandBeginQuery
            {
                QueryHeap = heap.Handle,
                QueryType = type,
                Index = index
            };

            _encoder.Emit(&command);
        }

        /// <summary>
        /// Ends a query
        /// </summary>
        /// <param name="heap">The <see cref="QuerySet"/> this query is within</param>
        /// <param name="type">The type of the query to end</param>
        /// <param name="index">The index within <paramref name="heap"/> that this query is</param>
        [IllegalBundleMethod]
        public void EndQuery(in QuerySet heap, QueryType type, uint index)
        {
            var command = new CommandEndQuery
            {
                QueryHeap = heap.Handle,
                QueryType = type,
                Index = index
            };

            _encoder.Emit(&command);
        }

        /// <summary>
        /// Queries the queue timestamp
        /// </summary>
        /// <param name="heap">The <see cref="QuerySet"/> this query is within</param>
        /// <param name="index">The index within <paramref name="heap"/> that this query is</param>
        [IllegalBundleMethod]
        public void QueryTimestamp(in QuerySet heap, uint index)
        {
            var command = new CommandReadTimestamp
            {
                QueryHeap = heap.Handle,
                Index = index
            };

            _encoder.Emit(&command);
        }

        /// <summary>
        /// Resolves opaque query set from a <see cref="QuerySet"/> to a <see cref="Buffer"/>
        /// </summary>
        /// <typeparam name="TQuery">The type of the query to resolve</typeparam>
        /// <param name="heap">The <see cref="QuerySet"/> to resolve from</param>
        /// <param name="queries">The range of queries within <paramref name="heap"/> to resolve</param>
        /// <param name="dest">The buffer to resolve the queries to</param>
        /// <param name="offset">The offset, in bytes, where the queries should be resolved to</param>
        [IllegalBundleMethod]
        public void ResolveQuery<TQuery>(in QuerySet heap, Range queries, [RequiresResourceState(ResourceState.CopyDestination)] in Buffer dest, uint offset = 0) where TQuery : struct, IQueryType
            => ResolveQuery(heap, default(TQuery).Type, queries, dest, offset);


        /// <summary>
        /// Resolves opaque query set from a <see cref="QuerySet"/> to a <see cref="Buffer"/>
        /// </summary>
        /// <param name="heap">The <see cref="QuerySet"/> to resolve from</param>
        /// <param name="type">The type of the query to resolve</param>
        /// <param name="queries">The range of queries within <paramref name="heap"/> to resolve</param>
        /// <param name="dest">The buffer to resolve the queries to</param>
        /// <param name="offset">The offset, in bytes, where the queries should be resolved to</param>
        [IllegalBundleMethod]
        public void ResolveQuery(in QuerySet heap, QueryType type, Range queries, [RequiresResourceState(ResourceState.CopyDestination)] in Buffer dest, uint offset = 0)
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
        public void CopyTexture(
            [RequiresResourceState(ResourceState.CopySource)] in Texture source,
            [RequiresResourceState(ResourceState.CopyDestination)] in Texture dest,
            uint subresource
        ) => CopyTexture(source, dest, subresource, subresource);

        /// <summary>
        /// Copy a subresource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="dest">The resource to copy to</param>
        /// <param name="sourceSubresource">The index of the subresource to copy from</param>
        /// <param name="destSubresource">The index of the subresource to copy to</param>
        [IllegalBundleMethod, IllegalRenderPassMethod]
        public void CopyTexture(
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
        /// Describes a set of barriers scoped over a certain region, created by <see cref="ScopedBarrier(ReadOnlySpan{ResourceTransition})"/>
        /// </summary>
        public ref struct ScopedBarrierSet
        {
            private CopyContext _context;
            private ResourceTransition? _single;
            private ReadOnlySpan<ResourceTransition> _barriers;


            internal ScopedBarrierSet(CopyContext context, in ResourceTransition barrier)
            {
                _context = context;
                _barriers = default;
                _single = barrier;
            }

            internal ScopedBarrierSet(CopyContext context, ReadOnlySpan<ResourceTransition> barriers)
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
                if (_single is ResourceTransition single)
                {
                    _context.Barrier(Reverse(single));
                    return;
                }

                int newBarrierCount = 0;

                Span<ResourceTransition> newBarriers = default;
                using RentedArray<ResourceTransition> rent = default;

                if (StackSentinel.SafeToStackalloc<ResourceTransition>(_barriers.Length))
                {
                    // avoid stupid stackalloc assignment rules
                    var p = stackalloc ResourceTransition[_barriers.Length];
                    newBarriers = new(p, _barriers.Length);
                }
                else
                {
                    Unsafe.AsRef(in rent) = RentedArray<ResourceTransition>.Create(_barriers.Length);
                    newBarriers = rent.AsSpan();
                }

                foreach (ref readonly var barrier in _barriers)
                {
                    newBarriers[newBarrierCount++] = Reverse(barrier);
                }

                _context.Barrier(newBarriers[0..newBarrierCount]);

                static ResourceTransition Reverse(in ResourceTransition barrier)
                {
                    return new ResourceTransition
                    {
                        Transition = new()
                        {
                            Resource = barrier.Transition.Resource,
                            Before = barrier.Transition.After,
                            After = barrier.Transition.Before,
                            Subresource = barrier.Transition.Subresource
                        }
                    };
                }
            }
        }

        /// <summary>
        /// Begins a set of scoped barriers which will be reversed when <see cref="ScopedBarrierSet.Dispose"/> is called
        /// </summary>
        /// <param name="barrier">The <see cref="ResourceTransition"/> to perform and reverse</param>
        /// <returns>A new <see cref="ScopedBarrierSet"/></returns>
        [IllegalBundleMethod]
        public ScopedBarrierSet ScopedBarrier(in ResourceTransition barrier)
        {
            Barrier(barrier);
            return new(this, barrier);
        }


        /// <summary>
        /// Begins a set of scoped barriers which will be reversed when <see cref="ScopedBarrierSet.Dispose"/> is called
        /// </summary>
        /// <param name="barriers">The <see cref="ResourceTransition"/>s to perform and reverse</param>
        /// <returns>A new <see cref="ScopedBarrierSet"/></returns>
        [IllegalBundleMethod]
        public ScopedBarrierSet ScopedBarrier(ReadOnlySpan<ResourceTransition> barriers)
        {
            Barrier(barriers);
            return new(this, barriers);
        }

        /// <summary>
        /// Mark a resource barrierson the command list
        /// </summary>
        /// <param name="barrier">The barrier</param>
        [IllegalBundleMethod]
        public void WriteBarrier(in ResourceWriteBarrier barrier)
        {
            fixed (ResourceWriteBarrier* pBarrier = &barrier)
            {
                var command = new CommandWriteBarrier
                {
                    Count = 1,
                };

                _encoder.EmitVariable(&command, pBarrier, command.Count);
            }
        }
        /// <summary>
        /// Mark a resource barrierson the command list
        /// </summary>
        /// <param name="barriers">The barrier</param>
        [IllegalBundleMethod]
        public void WriteBarrier(ReadOnlySpan<ResourceWriteBarrier> barriers)
        {
            fixed (ResourceWriteBarrier* pBarriers = barriers)
            {
                var command = new CommandWriteBarrier
                {
                    Count = (uint)barriers.Length,
                };

                _encoder.EmitVariable(&command, pBarriers, command.Count);
            }
        }

        /// <summary>
        /// Mark a resource barrierson the command list
        /// </summary>
        /// <param name="barrier">The barrier</param>
        [IllegalBundleMethod]
        public void Barrier(in ResourceTransition barrier)
        {
            fixed (ResourceTransition* pBarrier = &barrier)
            {
                var command = new CommandTransitions
                {
                    Count = 1,
                };

                _encoder.EmitVariable(&command, pBarrier, command.Count);
            }
        }

        /// <summary>
        /// Mark a set of resource barriers on the command list
        /// </summary>
        /// <param name="barriers">The barriers</param>
        [IllegalBundleMethod]
        public void Barrier(ReadOnlySpan<ResourceTransition> barriers)
        {
            fixed (ResourceTransition* pBarriers = barriers)
            {
                var command = new CommandTransitions
                {
                    Count = (uint)barriers.Length,
                };

                _encoder.EmitVariable(&command, pBarriers, command.Count);
            }
        }
    }

    public struct ResourceWriteBarrier
    {
        internal ResourceHandle Handle;

        public static ResourceWriteBarrier Create(in Buffer buff) => new() { Handle = new ResourceHandle { Type = ResourceHandleType.Buffer, Buffer = buff.Handle } };
        public static ResourceWriteBarrier Create(in Texture buff) => new() { Handle = new ResourceHandle { Type = ResourceHandleType.Texture, Texture = buff.Handle } };
        public static ResourceWriteBarrier Create(in RaytracingAccelerationStructure buff) => new() { Handle = new ResourceHandle { Type = ResourceHandleType.RaytracingAccelerationStructure, RaytracingAccelerationStructure = buff.Handle } };
    }

    /// <summary>
    /// Represents a transition of a resource between 2 <see cref="ResourceState"/>s
    /// </summary>
    public struct ResourceTransition
    {
        /// <summary>
        /// Creates a new <see cref="ResourceTransition"/> from a <see cref="Buffer"/>
        /// </summary>
        /// <param name="buff">The <see cref="Buffer"/> to transition</param>
        /// <param name="before">The <see cref="ResourceState"/> of the resource prior to transitioning</param>
        /// <param name="after">The <see cref="ResourceState"/> of the resource after transitioning</param>
        /// <returns>A new <see cref="ResourceTransition"/></returns>
        public static ResourceTransition Create(in Buffer buff, ResourceState before, ResourceState after)
            => new()
            {
                Transition = new()
                {
                    Resource = new()
                    {
                        Type = ResourceHandleType.Buffer,
                        Buffer = buff.Handle
                    },
                    Before = before,
                    After = after
                },
            };


        /// <summary>
        /// Creates a new <see cref="ResourceTransition"/> from a <see cref="Buffer"/>
        /// </summary>
        /// <param name="tex">The <see cref="Buffer"/> to transition</param>
        /// <param name="before">The <see cref="ResourceState"/> of the resource prior to transitioning</param>
        /// <param name="after">The <see cref="ResourceState"/> of the resource after transitioning</param>
        /// <param name="subresource">The subresource to transition</param>
        /// <returns>A new <see cref="ResourceTransition"/></returns>
        public static ResourceTransition Create(in Texture tex, ResourceState before, ResourceState after, uint subresource = uint.MaxValue)
            => new()
            {
                Transition = new()
                {
                    Resource = new()
                    {
                        Type = ResourceHandleType.Texture,
                        Texture = tex.Handle
                    },
                    Before = before,
                    After = after,
                    Subresource = subresource
                },
            };

        internal ResourceTransitionBarrier Transition;
    }
}
