using System;
using Voltium.Core.Devices;
using Voltium.Core.Managers;

namespace Voltium.Core.DXGI
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
        /// <see cref="GraphicsDevice"/>
        /// </summary>
        GraphicsAndCompute = 1 << 1
    }
}
