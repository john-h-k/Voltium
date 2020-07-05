using static TerraFX.Interop.D3D12_BLEND;



namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// Determines the blend factor used in a <see cref="RenderTargetBlendDesc"/>
    /// </summary>
    public enum BlendFactor
    {
        /// <summary>
        /// A value of 0
        /// </summary>
        Zero = D3D12_BLEND_ZERO,

        /// <summary>
        /// A value of 1
        /// </summary>
        One = D3D12_BLEND_ONE,

        /// <summary>
        /// The color from the pixel shader
        /// </summary>
        SourceColor = D3D12_BLEND_SRC_COLOR,


        /// <summary>
        /// 1 - the color from the pixel shader
        /// </summary>
        InvertedSourceColor = D3D12_BLEND_INV_SRC_COLOR,


        /// <summary>
        /// The alpha from the pixel shader
        /// </summary>
        SourceAlpha = D3D12_BLEND_SRC_ALPHA,


        /// <summary>
        /// 1 - the alpha from the pixel shader
        /// </summary>
        InvertedSourceAlpha = D3D12_BLEND_INV_SRC_ALPHA,


        /// <summary>
        /// The alpha from the render target
        /// </summary>
        DestAlpha = D3D12_BLEND_DEST_ALPHA,

        /// <summary>
        /// 1 - the alpha from the render target
        /// </summary>
        InvertedDestAlpha = D3D12_BLEND_INV_DEST_ALPHA,



        /// <summary>
        /// The color from the render target
        /// </summary>
        DestColor = D3D12_BLEND_DEST_COLOR,



        /// <summary>
        /// 1 - the color from the render target
        /// </summary>
        InvertedDestColor = D3D12_BLEND_INV_DEST_COLOR,


        /// <summary>
        /// The alpha from the render target
        /// </summary>
        SourceAlphaSatured = D3D12_BLEND_SRC_ALPHA_SAT,

        /// <summary>
        /// The blend factory provided by <see cref="GraphicsContext.SetBlendFactor"/>
        /// </summary>
        BlendFactor = D3D12_BLEND_BLEND_FACTOR,

        /// <summary>
        /// 1 - the blend factory provided by <see cref="GraphicsContext.SetBlendFactor"/>
        /// </summary>
        InvertedBlendFactor = D3D12_BLEND_INV_BLEND_FACTOR,


        /// <summary>
        /// A color provided by the shader, as SV_Target1 in HLSL
        /// </summary>
        ShaderProvidedColor = D3D12_BLEND_SRC1_COLOR,


        /// <summary>
        /// 1 - a color provided by the shader, as SV_Target1 in HLSL
        /// </summary>
        InvertedShaderProvidedColor = D3D12_BLEND_INV_SRC1_COLOR,


        /// <summary>
        /// An alpha provided by the shader, as SV_Target1 in HLSL
        /// </summary>
        ShaderProvidedAlpha = D3D12_BLEND_SRC1_ALPHA,


        /// <summary>
        /// 1 - an alpha provided by the shader, as SV_Target1 in HLSL
        /// </summary>
        InvertedShaderProvidedAlpha = D3D12_BLEND_INV_SRC1_ALPHA
    }
}
