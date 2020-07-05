using static TerraFX.Interop.DXGI_FORMAT;

namespace Voltium.Core.Managers.Shaders
{
    /// <summary>
    /// The type of a shader input element
    /// </summary>
    public enum ShaderInputType : uint
    {
        /// <summary>
        /// A signed 8 bit integer type
        /// </summary>
        Int8 = DXGI_FORMAT_R8_SINT,

        /// <summary>
        /// A signed 16 bit integer type
        /// </summary>
        Int16 = DXGI_FORMAT_R16_SINT,

        /// <summary>
        /// A signed 32 bit integer type
        /// </summary>
        Int32 = DXGI_FORMAT_R32_SINT,

        /// <summary>
        /// An unsigned 8 bit integer type
        /// </summary>
        UInt8 = DXGI_FORMAT_R8_SINT,

        /// <summary>
        /// An unsigned 16 bit integer type
        /// </summary>
        UInt16 = DXGI_FORMAT_R16_UINT,

        /// <summary>
        /// An unsigned 32 bit integer type
        /// </summary>
        UInt32 = DXGI_FORMAT_R32_UINT,

        /// <summary>
        /// A single precision 32 bit floating point type
        /// </summary>
        Float = DXGI_FORMAT_R32_FLOAT,

        /// <summary>
        /// Two single precision 32 bit floating point types
        /// </summary>
        Float2 = DXGI_FORMAT_R32G32_FLOAT,

        /// <summary>
        /// Three single precision 32 bit floating point types
        /// </summary>
        Float3 = DXGI_FORMAT_R32G32B32_FLOAT,

        /// <summary>
        /// Four single precision 32 bit floating point types
        /// </summary>
        Float4 = DXGI_FORMAT_R32G32B32A32_FLOAT,

        ///// <summary>
        ///// 6 single precision 32 bit floating point types, as a 3x2 matrix
        ///// </summary>
        //Float3x2,

        ///// <summary>
        ///// 16 single precision 32 bit floating point types, as a 4x4 matrix
        ///// </summary>
        //Float4x4,
    }
}
