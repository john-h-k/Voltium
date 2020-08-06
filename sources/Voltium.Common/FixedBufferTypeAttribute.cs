using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Common
{
    /// <summary>
    /// Indicates a type is a fixed buffer
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
    public sealed class FixedBufferTypeAttribute : Attribute
    {
        /// <summary>
        /// Constructs a new instance of <see cref="FixedBufferTypeAttribute"/>
        /// </summary>
        /// <param name="elementType">The <see cref="Type"/> of the element of the fixed buffer</param>
        /// <param name="elementCount">The number of elements of the buffer</param>
        public FixedBufferTypeAttribute(Type elementType, int elementCount)
        {

        }
    }
}
