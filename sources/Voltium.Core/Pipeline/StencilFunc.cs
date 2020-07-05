using static TerraFX.Interop.D3D12_STENCIL_OP;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// Defines the operation used by the stencil
    /// </summary>
    public enum StencilFunc
    {
        /// <summary>
        /// Keep the existing stencil data
        /// </summary>
        Keep = D3D12_STENCIL_OP_KEEP,

        /// <summary>
        /// Replace the stencil data with <c>0</c>
        /// </summary>
        Zero = D3D12_STENCIL_OP_ZERO,

        /// <summary>
        /// Use the stencil data set by <see cref="GraphicsContext.SetStencilRef"/>
        /// </summary>
        UseState = D3D12_STENCIL_OP_REPLACE,

        /// <summary>
        /// Increment the stencil data until it reaches max value, then clamp it
        /// </summary>
        ClampedIncrement = D3D12_STENCIL_OP_INCR_SAT,

        /// <summary>
        /// Decrement the stencil data until it reaches min value, then clamp it
        /// </summary>
        ClampedDecrement = D3D12_STENCIL_OP_DECR_SAT,

        /// <summary>
        /// Invert the stencil data
        /// </summary>
        Invert = D3D12_STENCIL_OP_INVERT,

        /// <summary>
        /// Increment the stencil data until it reaches max value, then let it overflow
        /// </summary>
        Increment = D3D12_STENCIL_OP_INCR,

        /// <summary>
        /// Decrement the stencil data until it reaches min value, then let it overflow
        /// </summary>
        Decrement = D3D12_STENCIL_OP_DECR
    }
}
