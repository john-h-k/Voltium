namespace Voltium.Core.Managers
{
    /// <summary>
    /// Represents a define used in an HLSL shader
    /// </summary>
    public readonly struct ShaderDefine
    {
        /// <summary>
        /// The name of the macro
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The definition, or textual replacement element, of the macro
        /// </summary>
        public readonly string Definition;

        /// <summary>
        /// Creates a new instance of <see cref="ShaderDefine"/>
        /// </summary>
        public ShaderDefine(string name, string definition)
        {
            Name = name;
            Definition = definition;
        }
    }
}
