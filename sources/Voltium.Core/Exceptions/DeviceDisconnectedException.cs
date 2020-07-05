using System;
using Voltium.Core.Exceptions;

namespace Voltium.Common
{
    /// <summary>
    /// 
    /// </summary>
    public class DeviceDisconnectedException : GraphicsException
    {
        /// <summary>
        /// 
        /// </summary>
        public DeviceDisconnectedException()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public DeviceDisconnectedException(string? message) : base(message)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public DeviceDisconnectedException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public DeviceDisconnectedException(string message, int hr, object? otherData = null)
            : base($"{message} -- Error code: {DebugExtensions.DeviceRemovedMessage(hr)}")
        {
        }
    }
}
