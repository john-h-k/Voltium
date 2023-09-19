using static TerraFX.Interop.DirectX.D3D12_BLEND_OP;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// The function used during blending
    /// </summary>
    public enum BlendFunc
    {
        /// <summary>
        /// No blend operation occurs
        /// </summary>
        None = -1,

        /// <summary>
        /// Addition
        /// </summary>
        Add = D3D12_BLEND_OP_ADD,

        /// <summary>
        /// Subtraction
        /// </summary>
        Subtract = D3D12_BLEND_OP_SUBTRACT,

        /// <summary>
        /// Subtraction, reversed, so that the first operand is
        /// subtracted from the second operand
        /// </summary>
        ReverseSubtract = D3D12_BLEND_OP_REV_SUBTRACT,

        /// <summary>
        /// The minimum of the 2 provided values
        /// </summary>
        Min = D3D12_BLEND_OP_MIN,

        /// <summary>
        /// The maximum of the 2 provided values
        /// </summary>
        Max = D3D12_BLEND_OP_MAX,
    }
}
