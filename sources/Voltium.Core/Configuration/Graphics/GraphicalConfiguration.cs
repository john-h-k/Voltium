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
        public uint SwapChainBufferCount { get; set; }

        /// <summary>
        /// The format of the back buffer
        /// </summary>
        public DataFormat BackBufferFormat { get; set; }

        /// <summary>
        /// The minimum feature level required for execution of the app
        /// </summary>
        public FeatureLevel RequiredFeatureLevel { get; set;  }

        /// <summary>
        /// The description of the multisampling state
        /// </summary>
        public MsaaDesc MultiSamplingStrategy { get; set;  }

        /// <summary>
        /// Whether to force the fullscreen app to be windowed
        /// </summary>
        public bool ForceFullscreenAsWindowed { get; set; }

        /// <summary>
        /// The number of frames rendered before presenting
        /// </summary>
        public uint VSyncCount { get; set; }
    }
}
