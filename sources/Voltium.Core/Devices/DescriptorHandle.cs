using TerraFX.Interop.DirectX;

namespace Voltium.Core
{
    /// <summary>
    /// Represents a GPU descriptor
    /// </summary>
    public struct DescriptorHandle
    {
        /// <summary>
        /// The GPU handle for the descriptor
        /// </summary>
        public D3D12_GPU_DESCRIPTOR_HANDLE GpuHandle;

        /// <summary>
        /// Creates a new <see cref="DescriptorHandle"/>
        /// </summary>
        internal DescriptorHandle(D3D12_GPU_DESCRIPTOR_HANDLE gpuHandle)
        {
            GpuHandle = gpuHandle;
        }
    }
}
