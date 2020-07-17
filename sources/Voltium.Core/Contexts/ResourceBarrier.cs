using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Core.Memory;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core.Contexts
{
    /// <summary>
    /// Options used in a <see cref="ResourceBarrier"/>
    /// </summary>
    public enum ResourceBarrierOptions
    {
        /// <summary>
        /// This is a full barrier
        /// </summary>
        Full = D3D12_RESOURCE_BARRIER_FLAGS.D3D12_RESOURCE_BARRIER_FLAG_NONE,

        /// <summary>
        /// This barrier only begins the barrier
        /// </summary>
        Begin = D3D12_RESOURCE_BARRIER_FLAGS.D3D12_RESOURCE_BARRIER_FLAG_BEGIN_ONLY,

        /// <summary>
        /// This barrier ends an already began barrier
        /// </summary>
        End = D3D12_RESOURCE_BARRIER_FLAGS.D3D12_RESOURCE_BARRIER_FLAG_END_ONLY
    }


    /// <summary>
    /// Represents a single resource barrier, either a UAV, alias, or transition barriers
    /// </summary>
    public readonly unsafe partial struct ResourceBarrier
    {
        internal readonly D3D12_RESOURCE_BARRIER Barrier;

        private ResourceBarrier(
            D3D12_RESOURCE_BARRIER_FLAGS flags,
            GpuResource? resource
        )
        {
            Unsafe.SkipInit(out Barrier);
            Barrier.Type = D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_UAV;
            Barrier.Flags = flags;
            Barrier.Anonymous.UAV.pResource = resource is null ? null : resource.GetResourcePointer();
        }

        private ResourceBarrier(
            D3D12_RESOURCE_BARRIER_FLAGS flags,
            GpuResource resource,
            ResourceState before,
            ResourceState after
        )
        { 
            Unsafe.SkipInit(out Barrier);
            Barrier.Type = D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
            Barrier.Flags = flags;
            Barrier.Anonymous.Transition.pResource = resource.GetResourcePointer();
            Barrier.Anonymous.Transition.StateBefore = (D3D12_RESOURCE_STATES)before;
            Barrier.Anonymous.Transition.StateAfter = (D3D12_RESOURCE_STATES)after;
        }

        private ResourceBarrier(
            D3D12_RESOURCE_BARRIER_FLAGS flags,
            GpuResource? srcResource,
            GpuResource? destResource
        )
        {
            Unsafe.SkipInit(out Barrier);
            Barrier.Type = D3D12_RESOURCE_BARRIER_TYPE.D3D12_RESOURCE_BARRIER_TYPE_ALIASING;
            Barrier.Flags = flags;
            Barrier.Anonymous.Aliasing.pResourceBefore = srcResource is null ? null : srcResource.GetResourcePointer();
            Barrier.Anonymous.Aliasing.pResourceAfter = destResource is null ? null : destResource.GetResourcePointer();
        }

        /// <summary>
        /// Creates a new <see cref="ResourceBarrier"/> representing a resource state transition
        /// </summary>
        /// <param name="buffer">The <see cref="Buffer"/> to transition</param>
        /// <param name="before">The before <see cref="ResourceState"/> of <paramref name="buffer"/></param>
        /// <param name="after">The after <see cref="ResourceState"/> of <paramref name="buffer"/></param>
        /// <param name="options">The <see cref="ResourceBarrierOptions"/> for the barrier</param>
        /// <returns>A new <see cref="ResourceBarrier"/> representing a resource state transition</returns>
        public static ResourceBarrier Transition(in Buffer buffer, ResourceState before, ResourceState after, ResourceBarrierOptions options = ResourceBarrierOptions.Full)
            => Transition(buffer.Resource, before, after, options);

        /// <summary>
        /// Creates a new <see cref="ResourceBarrier"/> representing a resource state transition
        /// </summary>
        /// <param name="tex">The <see cref="Texture"/> to transition</param>
        /// <param name="before">The before <see cref="ResourceState"/> of <paramref name="tex"/></param>
        /// <param name="after">The after <see cref="ResourceState"/> of <paramref name="tex"/></param>
        /// <param name="options">The <see cref="ResourceBarrierOptions"/> for the barrier</param>
        /// <returns>A new <see cref="ResourceBarrier"/> representing a resource state transition</returns>
        public static ResourceBarrier Transition(in Texture tex, ResourceState before, ResourceState after, ResourceBarrierOptions options = ResourceBarrierOptions.Full)
            => Transition(tex.Resource, before, after, options);

        internal static ResourceBarrier Transition(GpuResource resource, ResourceState before, ResourceState after, ResourceBarrierOptions options)
            => new ResourceBarrier((D3D12_RESOURCE_BARRIER_FLAGS)options, resource, before, after);

        /// <summary>
        /// Creates a new <see cref="ResourceBarrier"/> representing a UAV read/writer barrier
        /// </summary>
        /// <param name="buffer">The <see cref="Buffer"/> to barrier</param>
        /// <param name="options">The <see cref="ResourceBarrierOptions"/> for the barrier</param>
        /// <returns>A new <see cref="ResourceBarrier"/> representing a UAV read/writer barrier</returns>
        public static ResourceBarrier Uav(in Buffer buffer, ResourceBarrierOptions options = ResourceBarrierOptions.Full)
            => Uav(buffer.Resource, options);

        /// <summary>
        /// Creates a new <see cref="ResourceBarrier"/> representing a UAV read/writer barrier
        /// </summary>
        /// <param name="tex">The <see cref="Texture"/> to barrier</param>
        /// <param name="options">The <see cref="ResourceBarrierOptions"/> for the barrier</param>
        /// <returns>A new <see cref="ResourceBarrier"/> representing a UAV read/writer barrier</returns>
        public static ResourceBarrier Uav(in Texture tex, ResourceBarrierOptions options = ResourceBarrierOptions.Full)
            => Uav(tex.Resource, options);

        internal static ResourceBarrier Uav(GpuResource resource, ResourceBarrierOptions options)
            => new ResourceBarrier((D3D12_RESOURCE_BARRIER_FLAGS)options, resource);
    }
}
