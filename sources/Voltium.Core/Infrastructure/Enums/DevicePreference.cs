using System;

namespace Voltium.Core.Infrastructure
{
    /// <summary>
    /// Defines the application preferences when enumerating devices
    /// </summary>
    [Flags]
    public enum DevicePreference
    {
        /// <summary>
        /// The app has no preference
        /// </summary>
        NoPreference = 0,

        /// <summary>
        /// The app prefers low power consumption devices
        /// </summary>
        LowPower = 1,

        /// <summary>
        /// The app prefers high performance devices
        /// </summary>
        HighPerformance = 2,

        /// <summary>
        /// The app prefers hardware devices, rather than software drivers
        /// </summary>
        Hardware = 4,
    }
}
