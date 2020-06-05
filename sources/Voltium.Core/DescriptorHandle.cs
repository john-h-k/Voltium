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
        public CpuHandle CpuHandle;

        /// <summary>
        /// The GPU handle for the descriptor
        /// </summary>
        public GpuHandle GpuHandle;

        private int IncrementSize;

        /// <summary>
        /// Offset the descriptor by a fixed offset
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static DescriptorHandle operator +(DescriptorHandle handle, int offset)
        {
            handle.CpuHandle += offset * handle.IncrementSize;
            handle.GpuHandle += offset * handle.IncrementSize;
            return handle;
        }

        /// <summary>
        /// Offset the descriptor by 1
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public static DescriptorHandle operator ++(DescriptorHandle handle)
        {
            handle.CpuHandle += handle.IncrementSize;
            handle.GpuHandle += handle.IncrementSize;
            return handle;
        }

        /// <summary>
        /// Creates a new <see cref="DescriptorHandle"/>
        /// </summary>
        public DescriptorHandle(GraphicsDevice device, CpuHandle cpuHandle, GpuHandle gpuHandle, D3D12_DESCRIPTOR_HEAP_TYPE type)
        {
            CpuHandle = cpuHandle;
            GpuHandle = gpuHandle;
            IncrementSize = device.GetDescriptorSizeForType(type);
        }
    }
}
