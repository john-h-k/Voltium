﻿namespace Voltium.Core.DXGI
{
    /// <summary>
    /// Defines the IHV (Independent Hardware Vendor) of an <see cref="Adapter"/>
    /// </summary>
    public enum AdapterVendor : uint
    {
        /// <summary>
        /// NVidia
        /// </summary>
        NVidia = 0x10DE,

        /// <summary>
        /// AMD
        /// </summary>
        Amd = 0x1002,

        /// <summary>
        /// Intel
        /// </summary>
        Intel = 0x8086,
    }
}
