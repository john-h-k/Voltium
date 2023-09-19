using static TerraFX.Interop.DirectX.D3D12_COMPARISON_FUNC;

namespace Voltium.Core
{
    /// <summary>
    /// Defines the comparison function used by the sampler. If you have
    /// no idea what this means, you probably want <see cref="True"/>
    /// </summary>
    public enum SampleComparisonFunc
    {
        /// <summary>
        /// The comparison is always <see langword="false"/>
        /// </summary>
        False = D3D12_COMPARISON_FUNC_NEVER,

        /// <summary>
        /// The comparison is less than
        /// </summary>
        LessThan = D3D12_COMPARISON_FUNC_LESS,

        /// <summary>
        /// The comparison is equality
        /// </summary>
        Equal = D3D12_COMPARISON_FUNC_EQUAL,

        /// <summary>
        /// The comparison is less than or equal to
        /// </summary>
        LessThanOrEqual = D3D12_COMPARISON_FUNC_LESS_EQUAL,

        /// <summary>
        /// The comparison is greater than
        /// </summary>
        GreaterThan = D3D12_COMPARISON_FUNC_GREATER,

        /// <summary>
        /// The comparison is inequality
        /// </summary>
        NotEqual = D3D12_COMPARISON_FUNC_NOT_EQUAL,

        /// <summary>
        /// The comparison is greater than or equal to
        /// </summary>
        GreaterThanOrEqual = D3D12_COMPARISON_FUNC_GREATER_EQUAL,

        /// <summary>
        /// The comparison is always <see langword="true"/>
        /// </summary>
        True = D3D12_COMPARISON_FUNC_ALWAYS
    }
}
