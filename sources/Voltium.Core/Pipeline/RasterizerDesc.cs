using TerraFX.Interop;
using static TerraFX.Interop.DirectX.D3D12_CONSERVATIVE_RASTERIZATION_MODE;
using Voltium.Common;
using System.Runtime.InteropServices;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// Represents the state of the rasterization stage
    /// </summary>
    [Fluent]
    public partial struct RasterizerDesc
    {
        /// <summary>
        /// The default <see cref="RasterizerDesc"/>. This correspends
        /// to <c>CD3DX12_RASTERIZER_DESC(DEFAULT)</c>
        /// </summary>
        public static RasterizerDesc Default { get; } = new RasterizerDesc
        {
            EnableWireframe = false,
            FaceCullMode = CullMode.Back,
            FrontFaceType = FaceType.Clockwise,
            DepthBias = 0,
            MaxDepthBias = 0,
            SlopeScaledDepthBias = 0,
            EnableDepthClip = true,
            EnableConservativerRasterization = false
        };

        /// <summary>
        /// Whether the rasterizer should render wireframe models,
        /// or solid models
        /// </summary>
        public bool EnableWireframe;

        /// <summary>
        /// The mode used to cull faces
        /// </summary>
        public CullMode FaceCullMode;

        /// <summary>
        /// The <see cref="FaceType"/> indicating which faces are considered front
        /// </summary>
        public FaceType FrontFaceType;

        /// <summary>
        /// The value added to every pixel's depth value
        /// </summary>
        public int DepthBias;

        /// <summary>
        /// The maximum value added to <see cref="DepthBias"/>,
        /// which clamps it
        /// </summary>
        public float MaxDepthBias;

        /// <summary>
        /// The slope value used in calculation of the depth bias using
        /// <see cref="DepthBias"/> and <see cref="MaxDepthBias"/>
        /// </summary>
        public float SlopeScaledDepthBias;

        /// <summary>
        /// Whether depth clipping is enabled
        /// </summary>
        public bool EnableDepthClip;

        /// <summary>
        /// Whether conservative rasterization is used
        /// </summary>
        public bool EnableConservativerRasterization;

        public uint ForcedSampleCount;

        /// <summary>
        /// The <see cref="LineRenderAlgorithm"/> if this is used for rendering lines
        /// </summary>
        public LineRenderAlgorithm LineRenderAlgorithm;
    }

    public enum LineRenderAlgorithm
    {
        Aliased,
        AlphaAntiAliased,
        Quadrilateral
    }
}
