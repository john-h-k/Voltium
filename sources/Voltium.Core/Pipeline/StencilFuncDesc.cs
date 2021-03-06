using TerraFX.Interop;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// Describes the stencil operations that occur based on the
    /// depth and stencil test
    /// </summary>
    public struct StencilFuncDesc
    {
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
        public StencilFunc StencilTestFailOp;

        /// <summary>
        /// The <see cref="StencilFunc"/> to occur when the stencil test and depth test fails
        /// </summary>
        public StencilFunc StencilTestDepthTestFailOp;

        /// <summary>
        /// The <see cref="StencilFunc"/> to occur when the stencil test passes
        /// </summary>
        public StencilFunc StencilPasslOp;

        /// <summary>
        /// The <see cref="Comparison"/> to occur when comparing existing stencil data
        /// </summary>
        public Comparison ExistingDataOp;
    }
}
