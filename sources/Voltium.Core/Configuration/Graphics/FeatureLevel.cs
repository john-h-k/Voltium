using TerraFX.Interop;

namespace Voltium.Core.Managers
{
    /// <summary>
    /// The feature levels supported by a given app
    /// </summary>
    public enum FeatureLevel
    {
        /// <summary>
        /// Feature level 11.0
        /// </summary>
        Level11_0 = D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0,

        /// <summary>
        /// Feature level 11.1
        /// </summary>
        Level11_1 = D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_1,

        /// <summary>
        /// Feature level 12.0
        /// </summary>
        Level12_0 = D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_12_0,

        /// <summary>
        /// Feature level 12.1
        /// </summary>
        Level12_1 = D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_12_1
    }
}
