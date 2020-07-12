using System;

namespace Voltium.Core.Devices.Shaders
{
    /// <summary>
    /// Signifies that a type is used as a shader input
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public class ShaderInputAttribute : Attribute
    {

    }

    /// <summary>
    /// Signifies the input slot a vertex field should be bound to
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class InputLayoutAttribute : Attribute
    {
        /// <summary>
        /// The name of the element
        /// </summary>
        public string? Name { get; set; } = null;

        /// <summary>
        /// The name-index of the element, which is appended to <see cref="Name"/>
        /// </summary>
        public uint NameIndex { get; set; } = 0;

        /// <summary>
        /// The offset, in bytes, of this element from the start of the current vertex channel
        /// </summary>
        public uint Offset { get; set; } = 0xFFFFFFFF;

        /// <summary>
        /// The type of the element
        /// </summary>
        public DataFormat Type { get; set; } = DataFormat.Unknown;

        /// <summary>
        /// The channel of the element
        /// </summary>
        public uint Channel { get; set; } = 0;

        /// <summary>
        /// The <see cref="InputClass"/> of this element, either per-vertex or per-intsance
        /// </summary>
        public InputClass InputClass { get; set; } = InputClass.PerVertex;

        /// <summary>
        /// Creates a new instance of <see cref="ShaderInput"/>
        /// </summary>
        public InputLayoutAttribute()
        {
        }
    }

    /// <summary>
    /// Signifies that a type should be ignored by the shader input generator
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ShaderIgnoreAttribute : Attribute
    {

    }
}
