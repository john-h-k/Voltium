using System;
using System.Collections.Generic;

namespace Voltium.Core.Devices.Shaders
{
    /// <summary>
    /// Represents a type which can be bound to a shader.
    /// Apply the <see cref="ShaderInputAttribute"/> to a type instead
    /// of explicitly implementing this interface
    /// </summary>
    public interface IBindableShaderType
    {
        /// <summary>
        /// The inputs to the shader
        /// </summary>
        // array because NS2.0 has no ReadOnlyMemory<T>
        public ShaderInput[] GetShaderInputs();
    }
}
