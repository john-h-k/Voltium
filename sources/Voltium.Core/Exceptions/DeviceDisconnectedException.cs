using System;
using TerraFX.Interop;
using Voltium.Core.Devices;
using Voltium.Core.Exceptions;

namespace Voltium.Common
{
    /// <summary>
    /// Represents the reason for a <see cref="DeviceDisconnectedException"/>
    /// </summary>
    public enum DeviceDisconnectReason
    {
        /// <summary>
        /// The reason is unknown
        /// </summary>
        Unknown,

        /// <summary>
        /// The device was removed, either by the driver due to errorneous code or physical removal
        /// </summary>
        Removed,

        /// <summary>
        /// The device was reset
        /// </summary>
        Reset,

        /// <summary>
        /// The device hung, due to errorneous code
        /// </summary>
        Hung,

        /// <summary>
        /// There was an internal driver error
        /// </summary>
        InternalDriverError
    }

    /// <summary>
    /// Indicates the device was disconnected unexpectedly
    /// </summary>
    public class DeviceDisconnectedException : GraphicsException
    {
        /// <summary>
        /// The device which disconnected
        /// </summary>
        public ComputeDevice Device { get; }

        /// <summary>
        /// The <see cref="DeviceDisconnectReason"/> why <see cref="Device"/> was disconnected
        /// </summary>
        public DeviceDisconnectReason Reason { get; }

        /// <summary>
        /// 
        /// </summary>
        public DeviceDisconnectedException(ComputeDevice device, DeviceDisconnectReason reason)
        {
            Device = device;
            Reason = reason;
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
