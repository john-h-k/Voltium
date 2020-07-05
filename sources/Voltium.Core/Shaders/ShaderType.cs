namespace Voltium.Core.Managers
{
    /// <summary>
    /// Indicates the type of a <see cref="CompiledShader"/>
    /// </summary>
    public enum ShaderType
    {
        /// <summary>
        /// A vertex shader, or VS
        /// </summary>
        Vertex,

        /// <summary>
        /// A pixel shader, or PS
        /// </summary>
        Pixel,

        /// <summary>
        /// A domain shader, or DS, used in tesselation
        /// </summary>
        Domain,

        /// <summary>
        /// A hull shader, or HS, used in tesselation
        /// </summary>
        Hull,

        /// <summary>
        /// A geometry shader, or GS
        /// </summary>
        Geometry,

        /// <summary>
        /// A compute shader, or CS
        /// </summary>
        Compute,

        /// <summary>
        /// A set of shaders that can have any and all types
        /// </summary>
        Library
    }

    internal static class ShaderTypeExtensions
    {
        public static bool IsValid(this ShaderType type)
            => type >= ShaderType.Vertex && type <= ShaderType.Library;
    }
}
