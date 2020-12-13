using TerraFX.Interop;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// Describes the stencil operations that occur based on the
    /// depth and stencil test
    /// </summary>
    public struct StencilFuncDesc
    {
        internal D3D12_DEPTH_STENCILOP_DESC Desc;

        /// <summary>
        /// The default <see cref="StencilFuncDesc"/>
        /// </summary>
        public static StencilFuncDesc Default { get; } = new StencilFuncDesc
        {
            ExistingDataOp = Comparison.True,
            StencilTestDepthTestFailOp = StencilFunc.Keep,
            StencilPasslOp = StencilFunc.Keep,
            StencilTestFailOp = StencilFunc.Keep
        };

        /// <summary>
        /// The <see cref="StencilFunc"/> to occur when the stencil test fails
        /// </summary>
        public StencilFunc StencilTestFailOp { get => (StencilFunc)Desc.StencilFailOp; set => Desc.StencilFailOp = (D3D12_STENCIL_OP)value; }

        /// <summary>
        /// The <see cref="StencilFunc"/> to occur when the stencil test and depth test fails
        /// </summary>
        public StencilFunc StencilTestDepthTestFailOp { get => (StencilFunc)Desc.StencilDepthFailOp; set => Desc.StencilDepthFailOp = (D3D12_STENCIL_OP)value; }

        /// <summary>
        /// The <see cref="StencilFunc"/> to occur when the stencil test passes
        /// </summary>
        public StencilFunc StencilPasslOp { get => (StencilFunc)Desc.StencilPassOp; set => Desc.StencilPassOp = (D3D12_STENCIL_OP)value; }

        /// <summary>
        /// The <see cref="Comparison"/> to occur when comparing existing stencil data
        /// </summary>
        public Comparison ExistingDataOp { get => (Comparison)Desc.StencilFunc; set => Desc.StencilFunc = (D3D12_COMPARISON_FUNC)value; }
    }
}
