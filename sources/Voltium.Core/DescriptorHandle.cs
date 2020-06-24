using TerraFX.Interop;
using Voltium.Core.Managers;

namespace Voltium.Core
{
    /// <summary>
    /// Represents a GPU descriptor
    /// </summary>
    public struct DescriptorHandle
    {
        /// <summary>
        /// The CPU handle for the descriptor
        /// </summary>
        internal D3D12_CPU_DESCRIPTOR_HANDLE CpuHandle;

        /// <summary>
        /// The GPU handle for the descriptor
        /// </summary>
        public D3D12_GPU_DESCRIPTOR_HANDLE GpuHandle;

        private uint IncrementSize;

        /// <summary>
        /// Offset the descriptor by a fixed offset
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static DescriptorHandle operator +(DescriptorHandle handle, int offset)
        {
            handle.CpuHandle.Offset(offset, handle.IncrementSize);
            handle.GpuHandle.Offset(offset, handle.IncrementSize);
            return handle;
        }

        /// <summary>
        /// Offset the descriptor by a fixed offset
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static DescriptorHandle operator +(DescriptorHandle handle, uint offset)
            => handle + (int)offset;

        /// <summary>
        /// Offset the descriptor by 1
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public static DescriptorHandle operator ++(DescriptorHandle handle)
            => handle + 1;

        /// <summary>
        /// Creates a new <see cref="DescriptorHandle"/>
        /// </summary>
        internal DescriptorHandle(GraphicsDevice device, D3D12_CPU_DESCRIPTOR_HANDLE cpuHandle, D3D12_GPU_DESCRIPTOR_HANDLE gpuHandle, D3D12_DESCRIPTOR_HEAP_TYPE type)
        {
            CpuHandle = cpuHandle;
            GpuHandle = gpuHandle;
            IncrementSize = device.GetDescriptorSizeForType(type);
        }
    }
}
