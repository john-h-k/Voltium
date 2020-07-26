namespace Voltium.Core.Devices
{
    /// <summary>
    /// Describes a <see cref="TextureOutput"/>
    /// </summary>
    public struct OutputConfiguration
    {
        /// <summary>
        /// The number of buffers the swapchain should contain
        /// </summary>
        public uint BackBufferCount;

        /// <summary>
        /// The <see cref="BackBufferFormat"/> for the back buffer
        /// </summary>
        public BackBufferFormat BackBufferFormat;

        /// <summary>
        /// The sync interval to use when presenting
        /// </summary>
        public uint SyncInterval;
    }
}
