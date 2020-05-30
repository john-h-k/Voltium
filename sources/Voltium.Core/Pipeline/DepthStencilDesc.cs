using TerraFX.Interop;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// Describes the settings and state of the depth stencil of the pipeline
    /// </summary>
    public struct DepthStencilDesc
    {
        /// <summary>
        /// The default <see cref="DepthStencilDesc"/>. This correspends
        /// to <c>new CD3DX12_DEPTH_STENCIL_DESC(DEFAULT)</c>
        /// </summary>
        public static DepthStencilDesc Default { get; } = new DepthStencilDesc
        {
            EnableDepthTesting = true,
            DepthWriteMask = DepthWriteMask.All,
            DepthComparison = Comparison.LessThan,
            EnableStencilTesting = false,
            StencilReadMask = (byte)Windows.D3D12_DEFAULT_STENCIL_READ_MASK,
            StencilWriteMask = (byte)Windows.D3D12_DEFAULT_STENCIL_WRITE_MASK,
            FrontFace = StencilFuncDesc.Default,
            BackFace = StencilFuncDesc.Default
        };

        /// <summary>
        /// Whether depth testing should occur
        /// </summary>
        public bool EnableDepthTesting;

        /// <summary>
        /// The <see cref="DepthWriteMask"/>
        /// </summary>
        public DepthWriteMask DepthWriteMask;

        /// <summary>
        /// The function to use when comparing depth data against other depth data
        /// </summary>
        public Comparison DepthComparison;

        /// <summary>
        /// Whether stencil testing should occur
        /// </summary>
        public bool EnableStencilTesting;

        /// <summary>
        /// Identify a portion of the depth-stencil buffer for reading stencil data
        /// </summary>
        public byte StencilReadMask;

        /// <summary>
        /// Identify a portion of the depth-stencil buffer for writing stencil data
        /// </summary>
        public byte StencilWriteMask;

        /// <summary>
        /// The <see cref="StencilFuncDesc"/> describing the operations to occur during stenciling for the front face
        /// </summary>
        public StencilFuncDesc FrontFace;

        /// <summary>
        /// The <see cref="StencilFuncDesc"/> describing the operations to occur during stenciling for the back face
        /// </summary>
        public StencilFuncDesc BackFace;
    }
}
