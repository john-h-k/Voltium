using System.Drawing;
using Voltium.Core.Configuration.Graphics;

namespace Voltium.Core.Managers
{
    /// <summary>
    /// Describes configurable elements of the graphical engine
    /// </summary>
    public sealed class GraphicalConfiguration
    {
        /// <summary>
        /// The number of buffers (usually 2 or 3) used by the swapchain
        /// </summary>
        public uint SwapChainBufferCount { get; set; } = 2;

        /// <summary>
        /// The format of the back buffer
        /// </summary>
        public BackBufferFormat BackBufferFormat { get; set; } = BackBufferFormat.R8G8B8A8UnsignedNormalized;

        /// <summary>
        /// The minimum feature level required for execution of the app
        /// </summary>
        public FeatureLevel RequiredFeatureLevel { get; set; } = FeatureLevel.Level11_0;

        /// <summary>
        /// The number of frames rendered before presenting
        /// </summary>
        public uint VSyncCount { get; set; }
    }
}
