namespace Voltium.Core.Devices
{
    /// <summary>
    /// Describes a <see cref="TextureOutput"/>
    /// </summary>
    public struct OutputConfiguration
    {
        /// <summary>
        /// The default <see cref="OutputConfiguration"/>, with 3 BGRA backbuffers and no vsync
        /// </summary>
        public static OutputConfiguration Default { get; } = new OutputConfiguration { BackBufferCount = 3, BackBufferFormat = BackBufferFormat.B8G8R8A8UnsignedNormalized, SyncInterval = 0 };

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
