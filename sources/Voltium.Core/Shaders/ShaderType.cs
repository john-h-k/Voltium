using static TerraFX.Interop.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE;

namespace Voltium.Core.Devices
{
    /// <summary>
    /// Indicates the type of a <see cref="CompiledShader"/>
    /// </summary>
    public enum ShaderType
    {
        /// <summary>
        /// A vertex shader, or VS
        /// </summary>
        Vertex = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_VS,

        /// <summary>
        /// A pixel shader, or PS
        /// </summary>
        Pixel = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_PS,

        /// <summary>
        /// A domain shader, or DS, used in tesselation
        /// </summary>
        Domain = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_DS,

        /// <summary>
        /// A hull shader, or HS, used in tesselation
        /// </summary>
        Hull = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_HS,

        /// <summary>
        /// A geometry shader, or GS
        /// </summary>
        Geometry = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_GS,

        /// <summary>
        /// A compute shader, or CS
        /// </summary>
        Compute = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_CS,

        /// <summary>
        /// A mesh shader, or MS
        /// </summary>
        Mesh = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_MS,

        /// <summary>
        /// An amplification shader, or AS
        /// </summary>
        Amplification = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_AS,

        /// <summary>
        /// A set of shaders that can have any and all types
        /// </summary>
        Library,

        /// <summary>
        /// An unspecified shader type
        /// </summary>
        Unspecified = -1 
    }

    internal static class ShaderTypeExtensions
    {
        public static bool IsValid(this ShaderType type)
            => type is ShaderType.Vertex
                    or ShaderType.Pixel
                    or ShaderType.Domain
                    or ShaderType.Hull
                    or ShaderType.Geometry
                    or ShaderType.Compute
                    or ShaderType.Mesh
                    or ShaderType.Amplification
                    or ShaderType.Library
                    or ShaderType.Unspecified;
    }
}
