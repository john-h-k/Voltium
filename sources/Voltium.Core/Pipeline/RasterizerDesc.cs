namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// Represents the state of the rasterization stage
    /// </summary>
    public struct RasterizerDesc
    {
        /// <summary>
        /// The default <see cref="RasterizerDesc"/>. This correspends
        /// to <c>new CD3DX12_RASTERIZER_DESC(DEFAULT)</c>
        /// </summary>
        public static RasterizerDesc Default { get; } = new RasterizerDesc
        {
            EnableWireframe = false,
            FaceCullMode = CullMode.Anticockwise,
            DepthBias = 0,
            MaxDepthBias = 0,
            SlopeScaledDepthBias = 0,
            EnableDepthClip = true,
            EnableMsaa = false,
            EnableConservativerRasterization = false
        };

        /// <summary>
        /// Whether the rasterizer should render wireframe models,
        /// or solid models
        /// </summary>
        public bool EnableWireframe { get; set; }

        /// <summary>
        /// The mode used to cull faces
        /// </summary>
        public CullMode FaceCullMode { get; set; }

        /// <summary>
        /// The value added to every pixel's depth value
        /// </summary>
        public int DepthBias { get; set; }

        /// <summary>
        /// The maximum value added to <see cref="DepthBias"/>,
        /// which clamps it
        /// </summary>
        public float MaxDepthBias { get; set; }

        /// <summary>
        /// The slope value used in calculation of the depth bias using
        /// <see cref="DepthBias"/> and <see cref="MaxDepthBias"/>
        /// </summary>
        public float SlopeScaledDepthBias { get; set; }

        /// <summary>
        /// Whether depth clipping is enabled
        /// </summary>
        public bool EnableDepthClip { get; set; }

        /// <summary>
        /// Whether MSAA (multi-sample anti-aliasing) is enabled.
        /// If this is <see langword="true"/>, the render target
        /// must support MSAA
        /// </summary>
        public bool EnableMsaa { get; set; }

        // not currently exposed
        //public bool EnableAntialiasedLine { get; set; }
        //public uint ForcedSampleCount { get; set; }

        /// <summary>
        /// Whether conservative rasterization is used
        /// </summary>
        public bool EnableConservativerRasterization { get; set; }
    }
}
