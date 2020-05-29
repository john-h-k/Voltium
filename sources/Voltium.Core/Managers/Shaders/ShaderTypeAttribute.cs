using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Core.Managers.Shaders
{
    /// <summary>
    /// Indicates that a type represents a type used in a shader
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class ShaderTypeAttribute : Attribute
    {
    }
}
