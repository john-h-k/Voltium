using TerraFX.Interop.DirectX;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// Defines the mask used for writing depth data
    /// </summary>
    public enum DepthWriteMask
    {
        /// <summary>
        /// Mask all writes, so nothing is written to the depth buffer
        /// </summary>
        Zero = D3D12_DEPTH_WRITE_MASK.D3D12_DEPTH_WRITE_MASK_ZERO,

        /// <summary>
        /// Mask no writes, so all data is written to the depth buffer
        /// </summary>
        All = D3D12_DEPTH_WRITE_MASK.D3D12_DEPTH_WRITE_MASK_ALL
    }
}
