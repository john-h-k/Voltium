using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.GpuResources;

using static TerraFX.Interop.Windows;

namespace Voltium.Core
{
    /// <summary>
    /// Represents a transition between resource states
    /// </summary>
    public readonly struct ResourceTransition
    {
        /// <summary>
        /// The index of the subresource to transition, or 0xFFFFFFFF
        /// to transition all subresources
        /// </summary>
        public readonly uint Subresource;

        /// <summary>
        /// The state to transition to
        /// </summary>
        public readonly D3D12_RESOURCE_STATES NewState;

        /// <summary>
        /// Creates a new instance of <see cref="ResourceTransition"/>
        /// </summary>
        public ResourceTransition(D3D12_RESOURCE_STATES newState, uint subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES)
        {
            NewState = newState;
            Subresource = subresource;
        }

        /// <summary>
        /// The implicit conversion between <see cref="D3D12_RESOURCE_STATES"/> and <see cref="ResourceTransition"/>, which
        /// applies the transition to all subresources
        /// </summary>
        /// <param name="newState">The state to transition the resource to</param>
        public static implicit operator ResourceTransition(D3D12_RESOURCE_STATES newState) => new ResourceTransition(newState);
    }
}
