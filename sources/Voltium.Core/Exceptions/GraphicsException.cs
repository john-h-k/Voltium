using System;
using System.Runtime.Serialization;

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
