using System;
using System.Runtime.Serialization;
using Voltium.Core.Devices;

namespace Voltium.Core.Exceptions
{
    /// <summary>
    /// Indicates an exception has occurred in the graphics layer
    /// </summary>
    public class GraphicsException : Exception
    {
        public ComputeDevice Device { get; private set; }
        public GraphicsExceptionMessageType MessageType { get; set; }

        public GraphicsException(ComputeDevice device)
        {
            Device = device;
        }

        /// <inheritdoc/>
        public GraphicsException(ComputeDevice device, string? message) : base(message)
        {
            Device = device;
        }

        /// <inheritdoc/>
        public GraphicsException(ComputeDevice device, string? message, Exception? innerException) : base(message, innerException)
        {
            Device = device;
        }
    }
}
