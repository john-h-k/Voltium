using TerraFX.Interop;

namespace Voltium.Core.Managers
{
    /// <summary>
    /// The feature levels supported by a given app
    /// </summary>
    public enum FeatureLevel
    {
        /// <summary>
        /// Compute feature level 1.0, otherwise known as Compute level 1.0 Core
        /// </summary>
        ComputeLevel1_0 = D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_1_0_CORE,

        /// <summary>
        /// Graphics feature level 11.0
        /// </summary>
        GraphicsLevel11_0 = D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0,

        /// <summary>
        /// Graphics feature level 11.1
        /// </summary>
        GraphicsLevel11_1 = D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_1,

        /// <summary>
        /// Graphics feature level 12.0
        /// </summary>
        GraphicsLevel12_0 = D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_12_0,

        /// <summary>
        /// Graphics feature level 12.1
        /// </summary>
        GraphicsLevel12_1 = D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_12_1
    }
}
