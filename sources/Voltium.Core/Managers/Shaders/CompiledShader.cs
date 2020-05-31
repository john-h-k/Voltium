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
        /// Represents a <see cref="CompiledShader"/> containing no data
        /// </summary>
        public static CompiledShader Empty => default;

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
}