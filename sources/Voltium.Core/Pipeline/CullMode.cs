using static TerraFX.Interop.D3D12_CULL_MODE;

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
        /// Faces specified in clockwise order should be culled
        /// </summary>
        Clockwise = D3D12_CULL_MODE_BACK,

        /// <summary>
        /// Faces specified in anticlockwise order should be culled
        /// </summary>
        Anticockwise = D3D12_CULL_MODE_FRONT
    }
}
