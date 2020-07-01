using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Core.Exceptions
{
    /// <summary>
    /// Indicates an exception has occurred in the graphics layer
    /// </summary>
    public class GraphicsException : Exception
    {
        /// <inheritdoc/>
        public GraphicsException()
        {
        }

        /// <inheritdoc/>
        public GraphicsException(string? message) : base(message)
        {
        }

        /// <inheritdoc/>
        public GraphicsException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        /// <inheritdoc/>
        protected GraphicsException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
