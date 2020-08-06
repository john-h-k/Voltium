using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Common
{
    /// <summary>
    /// Indicates a type should have variadic generic overloads generated
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class VariadicGenericAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of <see cref="VariadicGenericAttribute"/>
        /// </summary>
        /// <param name="template">The template expression to use to generate the variadic expressions.
        /// Use the special character '%t%' </param>
        /// <param name="minNumberArgs"></param>
        /// <param name="maxNumberArgs"></param>
        public VariadicGenericAttribute(string template, int minNumberArgs = 1, int maxNumberArgs = 16)
        {

        }

        /// <summary>
        /// Indicates the templated expressions should be inserted directly after this 
        /// </summary>
        [Conditional("_NEVER_DEFINE_")]
        public static void InsertExpressionsHere() { }
    }
}
