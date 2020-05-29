using TerraFX.Interop;
using static TerraFX.Interop.D3D12_HEAP_FLAGS;

namespace Voltium.Core.GpuResources.OldStyle
{
    /// <summary>
    /// Represents a strongly-typed GPU default buffer than cannot be accessed by the CPU.
    /// By convention, a <typeparamref name="TElement"/> of <see cref="byte"/> means the buffer
    /// is not strongly typed
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    public unsafe class DefaultResource<TElement> : GpuBuffer<TElement> where TElement : struct
    {
        /// <summary>
        /// Create a new default buffer
        /// </summary>
        /// <param name="device">The device to create the buffer on</param>
        /// <param name="resourceDesc">The resource to create</param>
        /// <param name="state">The initial state of the buffer</param>
        /// <param name="flags">Any other heap flags</param>
        /// <param name="optimizedClearValue">If not <code>null</code>, the optimized clear value for the buffer</param>
        public DefaultResource(
            ID3D12Device* device,
            D3D12_RESOURCE_DESC resourceDesc,
            D3D12_RESOURCE_STATES state,
            D3D12_HEAP_FLAGS flags = D3D12_HEAP_FLAG_NONE,
            D3D12_CLEAR_VALUE? optimizedClearValue = null
        )
        : base(device, resourceDesc, state, D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_DEFAULT, flags, optimizedClearValue)
        {
        }
    }
}
