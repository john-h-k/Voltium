using TerraFX.Interop;
using static TerraFX.Interop.D3D12_CONSERVATIVE_RASTERIZATION_MODE;
using Voltium.Common;
using System.Runtime.InteropServices;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// Represents the state of the rasterization stage
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct RasterizerDesc : IPipelineStreamElement<RasterizerDesc>
    {
        [FieldOffset(0)]
        internal AlignedSubobjectType<D3D12_RASTERIZER_DESC> Type;

        [FieldOffset(0)]
        internal nuint _Pad;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public void _Initialize() => Type.Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_RASTERIZER;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

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
            EnableMsaa = false,
            EnableConservativerRasterization = false
        };

        /// <summary>
        /// Whether the rasterizer should render wireframe models,
        /// or solid models
        /// </summary>
        public bool EnableWireframe { get => Type.Inner.FillMode == D3D12_FILL_MODE.D3D12_FILL_MODE_WIREFRAME; set => Type.Inner.FillMode = value ? D3D12_FILL_MODE.D3D12_FILL_MODE_WIREFRAME : D3D12_FILL_MODE.D3D12_FILL_MODE_SOLID; }

        /// <summary>
        /// The mode used to cull faces
        /// </summary>
        public CullMode FaceCullMode { get => (CullMode)Type.Inner.CullMode; set => Type.Inner.CullMode = (D3D12_CULL_MODE)value; }

        /// <summary>
        /// The <see cref="FaceType"/> indicating which faces are considered front
        /// </summary>
        public FaceType FrontFaceType { get => Helpers.Int32ToBool(Type.Inner.FrontCounterClockwise) ? FaceType.Anticlockwise : FaceType.Clockwise; set => Type.Inner.FrontCounterClockwise = Helpers.BoolToInt32(value == FaceType.Anticlockwise); }

        /// <summary>
        /// The value added to every pixel's depth value
        /// </summary>
        public int DepthBias { get => Type.Inner.DepthBias; set => Type.Inner.DepthBias = value; }

        /// <summary>
        /// The maximum value added to <see cref="DepthBias"/>,
        /// which clamps it
        /// </summary>
        public float MaxDepthBias { get => Type.Inner.DepthBiasClamp; set => Type.Inner.DepthBiasClamp = value; }

        /// <summary>
        /// The slope value used in calculation of the depth bias using
        /// <see cref="DepthBias"/> and <see cref="MaxDepthBias"/>
        /// </summary>
        public float SlopeScaledDepthBias { get => Type.Inner.SlopeScaledDepthBias; set => Type.Inner.SlopeScaledDepthBias = value; }

        /// <summary>
        /// Whether depth clipping is enabled
        /// </summary>
        public bool EnableDepthClip { get => Helpers.Int32ToBool(Type.Inner.DepthClipEnable); set => Type.Inner.DepthClipEnable = Helpers.BoolToInt32(value); }

        /// <summary>
        /// Whether MSAA (multi-sample anti-aliasing) is enabled.
        /// If this is <see langword="true"/>, the render target
        /// must support MSAA
        /// </summary>
        public bool EnableMsaa { get => Helpers.Int32ToBool(Type.Inner.MultisampleEnable); set => Type.Inner.MultisampleEnable = Helpers.BoolToInt32(value); }

        // not currently exposed
        //public bool EnableAntialiasedLine { get; set; }
        //public uint ForcedSampleCount { get; set; }

        /// <summary>
        /// Whether conservative rasterization is used
        /// </summary>
        public bool EnableConservativerRasterization
        {
            get => Type.Inner.ConservativeRaster == D3D12_CONSERVATIVE_RASTERIZATION_MODE_ON;
            set => Type.Inner.ConservativeRaster = value ? D3D12_CONSERVATIVE_RASTERIZATION_MODE_ON : D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF;
        }
    }
}
