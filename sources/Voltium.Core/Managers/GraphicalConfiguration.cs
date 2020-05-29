using TerraFX.Interop;

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
        public uint BufferCount => 2;

        /// <summary>
        /// The format of the back buffer
        /// </summary>
        public DXGI_FORMAT BackBufferFormat { get; set; }

        /// <summary>
        /// The format of the depth stencil
        /// </summary>
        public DXGI_FORMAT DepthStencilFormat { get; set;  }

        /// <summary>
        /// The minimum feature level required for execution of the app
        /// </summary>
        public D3D_FEATURE_LEVEL RequiredDirect3DLevel { get; set;  } = D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_1;

        /// <summary>
        /// The description of the multisampling state
        /// </summary>
        public DXGI_SAMPLE_DESC MultiSamplingStrategy { get; set;  }

        /// <summary>
        /// The scaling mode used
        /// </summary>
        public DXGI_SCALING ScalingStrategy { get; set;  }

        /// <summary>
        /// The scaling mode used in fullscreen
        /// </summary>
        public DXGI_MODE_SCALING FullscreenScalingStrategy { get; set;  }

        /// <summary>
        /// The swap effect used in the swapchain
        /// </summary>
        public DXGI_SWAP_EFFECT SwapEffect { get; set;  }

        /// <summary>
        /// The scan-line ordering used
        /// </summary>
        public DXGI_MODE_SCANLINE_ORDER ScanlineOrdering { get; set;  }

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
