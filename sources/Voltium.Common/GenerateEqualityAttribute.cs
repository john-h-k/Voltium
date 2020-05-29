using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Common
{
    /// <summary>
    /// Indicates that equality members should be generated for a type
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class GenerateEqualityAttribute : Attribute
    {
    }
}
