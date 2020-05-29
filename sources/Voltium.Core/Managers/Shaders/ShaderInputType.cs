namespace Voltium.Core.Managers.Shaders
{
    /// <summary>
    /// The type of a shader input element
    /// </summary>
    public enum ShaderInputType
    {
        /// <summary>
        /// A signed 8 bit integer type
        /// </summary>
        Int8,

        /// <summary>
        /// A signed 16 bit integer type
        /// </summary>
        Int16,

        /// <summary>
        /// A signed 32 bit integer type
        /// </summary>
        Int32,

        /// <summary>
        /// An unsigned 8 bit integer type
        /// </summary>
        UInt8,

        /// <summary>
        /// An unsigned 16 bit integer type
        /// </summary>
        UInt16,

        /// <summary>
        /// An unsigned 32 bit integer type
        /// </summary>
        UInt32,

        /// <summary>
        /// A single precision 32 bit floating point type
        /// </summary>
        Float,

        /// <summary>
        /// Two single precision 32 bit floating point types
        /// </summary>
        Float2,

        /// <summary>
        /// Three single precision 32 bit floating point types
        /// </summary>
        Float3,

        /// <summary>
        /// Four single precision 32 bit floating point types
        /// </summary>
        Float4,

        /// <summary>
        /// 6 single precision 32 bit floating point types, as a 3x2 matrix
        /// </summary>
        Float3x2,

        /// <summary>
        /// 16 single precision 32 bit floating point types, as a 4x4 matrix
        /// </summary>
        Float4x4,
    }
}
