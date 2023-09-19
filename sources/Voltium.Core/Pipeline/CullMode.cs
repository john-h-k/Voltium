using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.DirectX.D3D12_CULL_MODE;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// Indicates what culling mode the pipeline should use
    /// </summary>
    public enum CullMode
    {
        /// <summary>
        /// No faces should be culled
        /// </summary>
        None = D3D12_CULL_MODE_NONE,

        /// <summary>
        /// Back faces should be culled
        /// </summary>
        Back = D3D12_CULL_MODE_BACK,

        /// <summary>
        /// Front faces should be culled
        /// </summary>
        Front = D3D12_CULL_MODE_FRONT
    }

    /// <summary>
    /// Indicates whether the face is clockwise or anticlockwise
    /// </summary>
    public enum FaceType
    {
        /// <summary>
        /// The faces vertices are clockwise
        /// </summary>
        Clockwise = FALSE,


        /// <summary>
        /// The faces vertices are anticlockwise
        /// </summary>
        Anticlockwise = TRUE
    }
}
