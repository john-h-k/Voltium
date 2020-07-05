using System;
using Voltium.Core.Devices;
using Voltium.Core.Managers;

namespace Voltium.Core.Infrastructure
{
    /// <summary>
    /// The type of a device
    /// </summary>
    [Flags]
    public enum DeviceType
    {
        /// <summary>
        /// The device only supports compute work, and can be used to create a
        /// <see cref="ComputeDevice"/>
        /// </summary>
        ComputeOnly = 1,

        /// <summary>
        /// The device supports compute and graphics work, and can be used to create a
        /// <see cref="GraphicsDevice"/> or a <see cref="ComputeDevice"/>
        /// </summary>
        GraphicsAndCompute = 1 << 1
    }
}
