using System;
using static TerraFX.Interop.DirectX.DXGI;

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

        /// <summary>
        /// The <see cref="OutputFlags"/> to use
        /// </summary>
        public OutputFlags Flags;
    }

    /// <summary>
    /// Defines flags used for <see cref="OutputConfiguration"/>
    /// </summary>
    [Flags]
    public enum OutputFlags : uint
    {
        /// <summary>
        /// Allow render-target access to the back buffer. This is the same as <see cref="Default"/>
        /// </summary>
        AllowRenderTarget = DXGI_USAGE_RENDER_TARGET_OUTPUT,

        ///// <summary>
        ///// Allow unordered-access to the back buffer
        ///// </summary>
        //AllowUnorderedAccess = Windows.DXGI_USAGE_UNORDERED_ACCESS,

        /// <summary>
        /// Allow shader-resoure access to the back buffer
        /// </summary>
        AllowShaderResorce = DXGI_USAGE_SHADER_INPUT,

        /// <summary>
        /// Preserve the back buffer, so that every n frames it is reused
        /// </summary>
        PreserveBackBuffer = 64,

        /// <summary>W
        /// The default flags. This is the same as <see cref="AllowRenderTarget"/>
        /// </summary>
        Default = AllowRenderTarget
    }

    internal static class OutputFlagExtensions
    {
        public static OutputFlags UsageFlags(this OutputFlags flags) => flags & (OutputFlags.AllowRenderTarget | OutputFlags.AllowShaderResorce);
    }
}
