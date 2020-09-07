using TerraFX.Interop;

namespace Voltium.Core.Devices
{
    /// <summary>
    /// Describes a <see cref="Output"/>
    /// </summary>
    public struct OutputConfiguration
    {
        /// <summary>
        /// The default <see cref="OutputConfiguration"/>, with 3 BGRA backbuffers and no vsync, and the back buffer not being preserved
        /// </summary>
        public static OutputConfiguration Default { get; } = new OutputConfiguration { Flags = OutputFlags.Default, BackBufferCount = 3, BackBufferFormat = BackBufferFormat.R8G8B8A8UnsignedNormalized, SyncInterval = 0 };

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

        public OutputFlags Flags;
    }

    public enum OutputFlags : uint
    {
        AllowRenderTarget = Windows.DXGI_USAGE_RENDER_TARGET_OUTPUT,
        AllowUnorderedAccess = Windows.DXGI_USAGE_UNORDERED_ACCESS,
        AllowShaderResorce = Windows.DXGI_USAGE_SHADER_INPUT,

        PreserveBackBuffer,

        Default = AllowRenderTarget
    }

    internal static class OutputFlagExtensions
    {
        public static OutputFlags UsageFlags(this OutputFlags flags) => flags & (OutputFlags.AllowRenderTarget | OutputFlags.AllowShaderResorce | OutputFlags.AllowUnorderedAccess);
    }
}
