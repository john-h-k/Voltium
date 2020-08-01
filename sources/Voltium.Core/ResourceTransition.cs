
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
        public readonly ResourceState NewState;

        /// <summary>
        /// Creates a new instance of <see cref="ResourceTransition"/>
        /// </summary>
        public ResourceTransition(ResourceState newState, uint subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES)
        {
            NewState = newState;
            Subresource = subresource;
        }
    }
}
