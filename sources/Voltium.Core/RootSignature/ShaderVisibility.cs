using static TerraFX.Interop.D3D12_SHADER_VISIBILITY;

namespace Voltium.Core
{
    /// <summary>
    /// Defines which shaders a resource is visible to
    /// </summary>
    public enum ShaderVisibility
    {
        /// <summary>
        /// The resource is visible to all shader stages
        /// </summary>
        All = D3D12_SHADER_VISIBILITY_ALL,

        /// <summary>
        /// The resource is visible to the vertex shader, or VS
        /// </summary>
        Vertex = D3D12_SHADER_VISIBILITY_VERTEX,

        /// <summary>
        /// The resource is visible to the pixel shader, or PS
        /// </summary>
        Pixel = D3D12_SHADER_VISIBILITY_PIXEL,

        /// <summary>
        /// The resource is visible to the domain shader, or DS
        /// </summary>
        Domain = D3D12_SHADER_VISIBILITY_DOMAIN,

        /// <summary>
        /// The resource is visible to the hull shader, or HS
        /// </summary>
        Hull = D3D12_SHADER_VISIBILITY_HULL,

        /// <summary>
        /// The resource is visible to the geometry shader, or GS
        /// </summary>
        Geometry = D3D12_SHADER_VISIBILITY_GEOMETRY,
    }
}
