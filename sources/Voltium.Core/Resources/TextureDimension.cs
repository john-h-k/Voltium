using TerraFX.Interop;

namespace Voltium.Core.Memory
{
    /// <summary>
    /// Represents the allowed dimensions of a GPU texture
    /// </summary>
    public enum TextureDimension
    {
        /// <summary>
        /// The texture has 1 dimension
        /// </summary>
        Tex1D = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE1D,

        /// <summary>
        /// The texture has 2 dimensions
        /// </summary>
        Tex2D = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE2D,

        /// <summary>
        /// The texture has 3 dimensions. 3 dimensional textures cannot
        /// be used as texture arrays
        /// </summary>
        Tex3D = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE3D
    }
}
