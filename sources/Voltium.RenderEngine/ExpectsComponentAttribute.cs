using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.RenderEngine
{
    /// <summary>
    /// Indicates a render pass relies on the existence of certain components
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ExpectsComponentAttribute : Attribute
    {
        /// <summary>
        /// Constructs a new <see cref="ExpectsComponentAttribute"/>
        /// </summary>
        /// <param name="components">The components this render pass expects</param>
        public ExpectsComponentAttribute(params Type[] components)
        {

        }
    }
}
