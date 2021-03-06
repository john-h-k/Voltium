using System;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Extensions;

namespace Voltium.Core.Devices.Shaders
{
    /// <summary>
    /// Represents an input element for a shader
    /// </summary>
    // !!! WARNING !!!
    // changing this type breaks IAInputDescGenerator in Voltium.Analyzers. If you change, make sure to fix generator too
    public struct ShaderInput
    {
        /// <summary>
        /// The name of the element
        /// </summary>
        public unsafe string Name;

        /// <summary>
        /// The name-index of the element, which is appended to <see cref="Name"/>
        /// </summary>
        public uint NameIndex;

        /// <summary>
        /// The offset, in bytes, of this element from the start of the current vertex channel
        /// </summary>
        public uint Offset;

        /// <summary>
        /// The type of the element
        /// </summary>
        public DataFormat Type;

        /// <summary>
        /// The channel of the element
        /// </summary>
        public uint Channel;

        /// <summary>
        /// The <see cref="InputClass"/> of this element, either per-vertex or per-intsance
        /// </summary>
        public InputClass InputClass;

        public uint VerticesPerInstanceChange;

        /// <summary>
        /// Creates a new instance of <see cref="ShaderInput"/>
        /// </summary>
        public ShaderInput(
            string name,
            DataFormat type,
            uint offset = 0xFFFFFFFF,
            uint nameIndex = 0,
            uint channel = 0,
            InputClass inputClass = InputClass.PerVertex,
            uint verticesPerInstanceChange = 0)
        {
            // offset = 0xFFFFFFFF causes D3D12 to append as next element
            this = default;
            Name = name;
            Offset = offset;
            Type = type;
            NameIndex = nameIndex;
            Channel = channel;
            InputClass = inputClass;
            VerticesPerInstanceChange = verticesPerInstanceChange;
        }
    }
}
