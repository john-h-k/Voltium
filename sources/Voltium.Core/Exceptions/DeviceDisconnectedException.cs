using System;
using TerraFX.Interop;
using Voltium.Core.Devices;
using Voltium.Core.Exceptions;

namespace Voltium.Common
{
    /// <summary>
    /// 
    /// </summary>
    public class DeviceDisconnectedException : GraphicsException
    {
        /// <summary>
        /// The device which disconnected
        /// </summary>
        public ComputeDevice Device { get; }

        /// <summary>
        /// 
        /// </summary>
        public DeviceDisconnectedException(ComputeDevice device)
        {
            Device = device;
        }

        /// <summary>
        /// 
        /// </summary>
        public DeviceDisconnectedException(ComputeDevice device, string? message) : base(message)
        {
            Device = device;
        }

        /// <summary>
        /// 
        /// </summary>
        public DeviceDisconnectedException(ComputeDevice device, string? message, Exception? innerException) : base(message, innerException)
        {
            Device = device;
        }

        /// <summary>
        /// 
        /// </summary>
        public DeviceDisconnectedException(ComputeDevice device, string message, int hr, object? otherData = null)
            : base($"{message} -- Error code: {DebugExtensions.DeviceRemovedMessage(hr)}")
        {
            Device = device;
        }
    }
}
