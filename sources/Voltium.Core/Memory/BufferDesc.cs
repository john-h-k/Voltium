namespace Voltium.Core.Memory
{
    /// <summary>
    /// Describes a buffer, for use by the <see cref="GpuAllocator"/>
    /// </summary>
    public struct BufferDesc
    {
        /// <summary>
        /// The size of the buffer, in bytes
        /// </summary>
        public long Length;

        /// <summary>
        /// Any addition resource flags
        /// </summary>
        public ResourceFlags ResourceFlags;
    }
}
