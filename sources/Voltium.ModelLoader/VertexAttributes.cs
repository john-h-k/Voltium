using System.Numerics;

namespace Voltium.Interactive
{
    /// <summary>
    /// Defines vertex attributes
    /// </summary>
    public enum VertexAttributes
    {
        /// <summary>
        /// The position of a vertex, of type <see cref="Vector4"/>
        /// </summary>
        Position,

        /// <summary>
        /// The color of a vertex, of type <see cref="Vector4"/>.
        /// This is the first of up to eight color values
        /// </summary>
        Color0,

        /// <summary>
        /// The color of a vertex, of type <see cref="Vector4"/>.
        /// This is the second of up to eight color values
        /// </summary>
        Color1,

        /// <summary>
        /// The color of a vertex, of type <see cref="Vector4"/>.
        /// This is the third of up to eight color values
        /// </summary>
        Color2,

        /// <summary>
        /// The color of a vertex, of type <see cref="Vector4"/>.
        /// This is the fourth of up to eight color values
        /// </summary>
        Color3,

        /// <summary>
        /// The color of a vertex, of type <see cref="Vector4"/>.
        /// This is the fifth of up to eight color values
        /// </summary>
        Color4,

        /// <summary>
        /// The color of a vertex, of type <see cref="Vector4"/>.
        /// This is the sixth of up to eight color values
        /// </summary>
        Color5,

        /// <summary>
        /// The color of a vertex, of type <see cref="Vector4"/>.
        /// This is the seventh of up to eight color values
        /// </summary>
        Color6,

        /// <summary>
        /// The color of a vertex, of type <see cref="Vector4"/>.
        /// This is the eighth of up to eight color values
        /// </summary>
        Color7,

        /// <summary>
        /// The texture coord of a vertex, of type <see cref="Vector4"/>.
        /// This is the first of up to eight color values
        /// </summary>
        TexCoord0,

        /// <summary>
        /// The texture coord of a vertex, of type <see cref="Vector4"/>.
        /// This is the second of up to eight tex coord values
        /// </summary>
        TexCoord1,

        /// <summary>
        /// The texture coord of a vertex, of type <see cref="Vector4"/>.
        /// This is the third of up to eight tex coord values
        /// </summary>
        TexCoord2,

        /// <summary>
        /// The texture coord of a vertex, of type <see cref="Vector4"/>.
        /// This is the fourth of up to eight tex coord values
        /// </summary>
        TexCoord3,

        /// <summary>
        /// The texture coord of a vertex, of type <see cref="Vector4"/>.
        /// This is the fifth of up to eight tex coord values
        /// </summary>
        TexCoord4,

        /// <summary>
        /// The texture coord of a vertex, of type <see cref="Vector4"/>.
        /// This is the sixth of up to eight tex coord values
        /// </summary>
        TexCoord5,

        /// <summary>
        /// The texture coord of a vertex, of type <see cref="Vector4"/>.
        /// This is the seventh of up to eight tex coord values
        /// </summary>
        TexCoord6,

        /// <summary>
        /// The texture coord of a vertex, of type <see cref="Vector4"/>.
        /// This is the eighth of up to eight tex coord values
        /// </summary>
        TexCoord7,

        /// <summary>
        /// The normal vector of the vertex, of type <see cref="Vector3"/>
        /// </summary>
        Normal,

        /// <summary>
        /// The tangent vector of the vertex, of type <see cref="Vector3"/>
        /// </summary>
        Tangent,

        /// <summary>
        /// The bitangent vector of the vertex, of type <see cref="Vector3"/>
        /// </summary>
        BiTangent,
    }
}
