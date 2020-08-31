using System.Runtime.CompilerServices;
using TerraFX.Interop;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// Describes the blend operation for a render target
    /// </summary>
    public struct RenderTargetBlendDesc
    {
        internal D3D12_RENDER_TARGET_BLEND_DESC Desc;

        /// <summary>
        /// The default <see cref="RenderTargetBlendDesc"/>
        /// </summary>
        public static RenderTargetBlendDesc Default { get; }

        static RenderTargetBlendDesc()
        {
            Default = NoBlend;
        }

        /// <summary>
        /// Indicates which blend function should be used during RGB blending, or <see cref="BlendFunc.None"/>
        /// to indicate RGB blending is disabled
        /// </summary>
        public BlendFunc BlendOp { get => (BlendFunc)Desc.BlendOp; set => Desc.BlendOp = (D3D12_BLEND_OP)value; }

        /// <summary>
        /// Indicates which blend function should be used during RGB blending, or <see cref="BlendFunc.None"/>
        /// to indicate alpha blending is disabled. 
        /// </summary>
        public BlendFunc AlphaBlendOp { get => (BlendFunc)Desc.BlendOpAlpha; set => Desc.BlendOpAlpha = (D3D12_BLEND_OP)value; }

        /// <summary>
        /// Indicates which logical blend function should be used during logical render target blending, or <see cref="BlendFuncLogical.None"/>
        /// to indicate alpha blending is disabled. It is invalid for this value to be anything other than <see cref="BlendFuncLogical.None"/> 
        /// if <see cref="BlendOp"/> or <see cref="AlphaBlendOp"/> are not both <see cref="BlendFunc.None"/>
        /// </summary>
        public BlendFuncLogical LogicalBlendOp { get => (BlendFuncLogical)Desc.LogicOp; set => Desc.LogicOp = (D3D12_LOGIC_OP)value; }

        /// <summary>
        /// The <see cref="BlendFactor"/> used as the source component in RGB blending
        /// </summary>
        public BlendFactor SrcBlend { get => (BlendFactor)Desc.SrcBlend; set => Desc.SrcBlend = (D3D12_BLEND)value; }

        /// <summary>
        /// The <see cref="BlendFactor"/> used as the dest component in RGB blending
        /// </summary>
        public BlendFactor DestBlend { get => (BlendFactor)Desc.DestBlend; set => Desc.DestBlend = (D3D12_BLEND)value; }

        /// <summary>
        /// The <see cref="BlendFactor"/> used as the source component in alpha blending
        /// </summary>
        public BlendFactor SrcBlendAlpha { get => (BlendFactor)Desc.SrcBlendAlpha; set => Desc.SrcBlendAlpha = (D3D12_BLEND)value; }

        /// <summary>
        /// The <see cref="BlendFactor"/> used as the dest component in alpha blending
        /// </summary>
        public BlendFactor DestBlendAlpha { get => (BlendFactor)Desc.DestBlendAlpha; set => Desc.DestBlendAlpha = (D3D12_BLEND)value; }

        /// <summary>
        /// The flags used to mask which RGBA channels are written to the render target
        /// </summary>s
        public ColorWriteFlags RenderTargetWriteMask { get => (ColorWriteFlags)Desc.RenderTargetWriteMask; set => Desc.RenderTargetWriteMask = (byte)value; }

        /// <summary>
        /// Represents a <see cref="RenderTargetBlendDesc"/> where RGBA and logical blending is disabled
        /// </summary>
        public static RenderTargetBlendDesc NoBlend { get; } = new RenderTargetBlendDesc(
                                                                    ColorWriteFlags.All,
                                                                    blendOp: BlendFunc.None,
                                                                    alphaBlendOp: BlendFunc.None,
                                                                    logicalBlendOp: BlendFuncLogical.None
                                                                );

        /// <summary>
        /// Creates a new <see cref="RenderTargetBlendDesc"/> representing an RGB and alpha blend
        /// </summary>
        /// <param name="rgbBlendOp">The <see cref="BlendFunc"/> to use for RGB blending</param>
        /// <param name="rgbBlendFactorSrc">The <see cref="BlendFactor"/> indicating the first source for the rgb blend</param>
        /// <param name="rgbBlendFactorDest">The <see cref="BlendFactor"/> indicating the second source for the alpha blend</param>
        /// <param name="alphaBlendOp">The <see cref="BlendFunc"/> to use for alpha blending</param>
        /// <param name="alphaBlendFactorSrc">The <see cref="BlendFactor"/> indicating the first source for the rgb blend</param>
        /// <param name="alphaBlendFactorDest">The <see cref="BlendFactor"/> indicating the second source for the alpha blend</param>
        /// <param name="flags"><see cref="ColorWriteFlags"/> indicating which RGBA channels will be written to the render target</param>
        /// <returns>A new <see cref="RenderTargetBlendDesc"/> representing an RGB and alpha blend</returns>
        public static RenderTargetBlendDesc CreateBlend(
            BlendFunc rgbBlendOp,
            BlendFactor rgbBlendFactorSrc,
            BlendFactor rgbBlendFactorDest,
            BlendFunc alphaBlendOp,
            BlendFactor alphaBlendFactorSrc,
            BlendFactor alphaBlendFactorDest,
            ColorWriteFlags flags = ColorWriteFlags.All
        )
            => new RenderTargetBlendDesc(
                blendOp: rgbBlendOp,
                srcBlend: rgbBlendFactorSrc,
                destBlend: rgbBlendFactorDest,
                alphaBlendOp: alphaBlendOp,
                srcBlendAlpha: alphaBlendFactorSrc,
                destBlendAlpha: alphaBlendFactorDest,
                renderTargetWriteMask: flags
            );

        /// <summary>
        /// Creates a new <see cref="RenderTargetBlendDesc"/> representing a logical blend
        /// </summary>
        /// <param name="logicalOp">The logical operation to use</param>
        /// <param name="flags"><see cref="ColorWriteFlags"/> indicating which RGBA channels will be written to the render target</param>
        /// <returns>A new <see cref="RenderTargetBlendDesc"/> representing a logical blend</returns>
        public static RenderTargetBlendDesc CreateLogicalBlend(
            BlendFuncLogical logicalOp,
            ColorWriteFlags flags = ColorWriteFlags.All)
            => new RenderTargetBlendDesc(
                logicalBlendOp: logicalOp,
                renderTargetWriteMask: flags
            );

        private RenderTargetBlendDesc(
            ColorWriteFlags renderTargetWriteMask,
            BlendFunc blendOp = BlendFunc.Add,
            BlendFunc alphaBlendOp = BlendFunc.Add,
            BlendFuncLogical logicalBlendOp = BlendFuncLogical.Nop,
            BlendFactor srcBlend = BlendFactor.One,
            BlendFactor destBlend = BlendFactor.Zero,
            BlendFactor srcBlendAlpha = BlendFactor.One,
            BlendFactor destBlendAlpha = BlendFactor.Zero
        )
        {
            Unsafe.SkipInit(out this);
            BlendOp = blendOp;
            AlphaBlendOp = alphaBlendOp;
            LogicalBlendOp = logicalBlendOp;
            SrcBlend = srcBlend;
            DestBlend = destBlend;
            SrcBlendAlpha = srcBlendAlpha;
            DestBlendAlpha = destBlendAlpha;
            RenderTargetWriteMask = renderTargetWriteMask;
        }
    }
}
