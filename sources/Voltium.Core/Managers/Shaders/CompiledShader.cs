using System;
using TerraFX.Interop;

namespace Voltium.Core.Managers
{
    /// <summary>
    /// Represents a compiled shader
    /// </summary>
    public readonly unsafe struct CompiledShader
    {
        /// <summary>
        /// The pointer to the beginning of the shader data
        /// </summary>
        public readonly ReadOnlyMemory<byte> ShaderData;

        /// <summary>
        /// The size of the shader data, in bytes
        /// </summary>
        public int Length => ShaderData.Length;

        /// <summary>
        /// The type of the shader
        /// </summary>
        public readonly ShaderType Type;

        /// <summary>
        /// Creates a new instance of a <see cref="CompiledShader"/>
        /// </summary>
        public CompiledShader(ReadOnlyMemory<byte> shaderData, ShaderType type)
        {
            ShaderData = shaderData;
            Type = type;
        }

        /// <summary>
        /// Returns a <c>readonly ref byte</c> to the start of the shader data
        /// </summary>
        public ref readonly byte GetPinnableReference() => ref ShaderData.Span.GetPinnableReference();
    }

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
}