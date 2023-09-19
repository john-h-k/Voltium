using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using TerraFX.Interop.DirectX;
using Voltium.Common;
using Voltium.Core.Pipeline;

namespace Voltium.Core.Devices
{
    public unsafe struct ShaderExport
    {
        public string Name { get; set; }
        public string ExportRename { get; set; }
    }

    /// <summary>
    /// Represents a compiled shader
    /// </summary>
    public readonly unsafe partial struct CompiledShader : IDisposable
    {
        /// <summary>
        /// Represents a <see cref="CompiledShader"/> containing no data
        /// </summary>
        public static CompiledShader Empty => default;

        /// <summary>
        /// The type of the shader
        /// </summary>
        public readonly ShaderType Type;

        /// <summary>
        /// A pointer to the shader data
        /// </summary>
        public readonly unsafe void* Pointer;

        /// <summary>
        /// The size, in bytes, of the shader
        /// </summary>
        public readonly nuint Length;

        /// <summary>
        /// The pointer to the beginning of the shader data
        /// </summary>
        public readonly ReadOnlySpan<byte> Span => new ReadOnlySpan<byte>(Pointer, (int)Length);

        // Internal ctors because the pointer must be from Helpers.Alloc to pair with the Helpers.Free
        // Can't use a IMemoryOwner because we need a specific native layout for this type


        /// <summary>
        /// Creates a new instance of a <see cref="CompiledShader"/>
        /// </summary>
        internal CompiledShader(D3D12_SHADER_BYTECODE bytecode, ShaderType type) : this(bytecode.pShaderBytecode, bytecode.BytecodeLength, type) { }

        /// <summary>
        /// Creates a new instance of a <see cref="CompiledShader"/>
        /// </summary>
        internal CompiledShader(void* pointer, nint size, ShaderType type) : this(pointer, (nuint)size, type) { }


        /// <summary>
        /// Creates a new instance of a <see cref="CompiledShader"/>
        /// </summary>
        internal CompiledShader(void* pointer, nuint size, ShaderType type)
        {
            Type = type;
            Pointer = pointer;
            Length = size;
        }


        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public bool Equals(CompiledShader other) => Type == other.Type && Span.SequenceEqual(other.Span);

        /// <summary>
        /// Returns a <see langword="ref readonly"/> <see cref="byte"/> to the start of the shader data
        /// </summary>
        public ref readonly byte GetPinnableReference() => ref *(byte*)Pointer;

        /// <summary>
        /// Frees the shader bytes
        /// </summary>
        public void Dispose() => Helpers.Free(Pointer);
    }
}
