using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Core.Managers.Shaders
{
    /// <summary>
    /// Represents an input element for a shader
    /// </summary>
    public readonly struct ShaderInput
    {
        /// <summary>
        /// The name of the element
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The type of the element
        /// </summary>
        public readonly ShaderInputType Type;

        /// <summary>
        /// Creates a new instance of <see cref="ShaderInput"/>
        /// </summary>
        public ShaderInput(string name, ShaderInputType type)
        {
            Name = name;
            Type = type;
        }
    }
}
