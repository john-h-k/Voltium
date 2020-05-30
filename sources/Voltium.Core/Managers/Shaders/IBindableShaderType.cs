using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Core.Managers.Shaders
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
        public ReadOnlyMemory<ShaderInput> GetShaderInputs();
    }
}
