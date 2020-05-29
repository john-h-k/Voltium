using TerraFX.Interop;
using static TerraFX.Interop.D3D12_HEAP_FLAGS;

namespace Voltium.Core.GpuResources.OldStyle
{
    /// <summary>
    /// Represents a strongly-typed GPU upload buffer than can be accessed by the CPU
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    public unsafe class UploadBuffer<TElement> : GpuBuffer<TElement> where TElement : struct
    {
        /// <summary>
        /// Create a new upload buffer
        /// </summary>
        /// <param name="device">The device to create the buffer on</param>
        /// <param name="resourceDesc">The resource to create</param>
        /// <param name="state">The initial state of the buffer</param>
        /// <param name="flags">Any other heap flags</param>
        public UploadBuffer(
            ID3D12Device* device,
            D3D12_RESOURCE_DESC resourceDesc,
            D3D12_RESOURCE_STATES state,
            D3D12_HEAP_FLAGS flags = D3D12_HEAP_FLAG_NONE)
            : base(device, resourceDesc, state, D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_UPLOAD, flags)
        {
        }
    }
}
