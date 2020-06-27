using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.DXGI
{
    /// <summary>
    /// Represents a DXGI adapter
    /// </summary>
    public struct Adapter : IDisposable
    {
        /// <summary>
        /// The value of the <see cref="IDXGIAdapter1"/>
        /// </summary>
        internal unsafe IUnknown* UnderlyingAdapter => _adapter.Get();

        private ComPtr<IUnknown> _adapter;

        /// <summary>
        /// A string that contains the adapter description. On feature level 9 graphics hardware, GetDesc1 returns “Software Adapter” for the description string.
        /// </summary>
        public readonly string Description;

        /// <summary>
        /// The PCI ID of the hardware vendor. On feature level 9 graphics hardware, GetDesc1 returns zeros for the PCI ID of the hardware vendor.
        /// </summary>
        public readonly AdapterVendor VendorId;

        /// <summary>
        /// The PCI ID of the hardware device. On feature level 9 graphics hardware, GetDesc1 returns zeros for the PCI ID of the hardware device.
        /// </summary>
        public readonly uint DeviceId;

        /// <summary>
        /// The PCI ID of the sub system. On feature level 9 graphics hardware, GetDesc1 returns zeros for the PCI ID of the sub system.
        /// </summary>
        public readonly uint SubSysId;

        /// <summary>
        /// The PCI ID of the revision number of the adapter. On feature level 9 graphics hardware, GetDesc1 returns zeros for the PCI ID of the revision number of the adapter.
        /// </summary>
        public readonly uint Revision;

        /// <summary>
        /// The size, in bytes, of the video memory for the adapter, and not accessible by the CPU
        /// </summary>
        public readonly ulong DedicatedVideoMemory;

        /// <summary>
        /// The size, in bytes, of the system memory dedicated to the adapter, and not accessible by the CPU
        /// </summary>
        public readonly ulong DedicatedSystemMemory;

        /// <summary>
        /// The size, in bytes, of the maximum amount of shared system memory the adapter can use
        /// </summary>
        public readonly ulong SharedSystemMemory;

        /// <summary>
        /// A locally-unique identifier for the adapter
        /// </summary>
        public readonly ulong AdapterLuid;

        /// <summary>
        ///  <code>true</code> if this adapter is implemented in software, else <code>false</code>
        /// </summary>
        public readonly bool IsSoftware;

        /// <summary>
        /// Create a new instance of <see cref="Adapter"/>
        /// </summary>
        internal unsafe Adapter(
            ComPtr<IUnknown> adapter,
            string description,
            AdapterVendor vendorId,
            uint deviceId,
            uint subSysId,
            uint revision,
            ulong dedicatedVideoMemory,
            ulong dedicatedSystemMemory,
            ulong sharedSystemMemory,
            LUID adapterLuid,
            bool isSoftware
        )
        {
            _adapter = adapter;
            Description = description;
            VendorId = vendorId;
            DeviceId = deviceId;
            SubSysId = subSysId;
            Revision = revision;
            DedicatedVideoMemory = dedicatedVideoMemory;
            DedicatedSystemMemory = dedicatedSystemMemory;
            SharedSystemMemory = sharedSystemMemory;
            AdapterLuid = adapterLuid.LowPart | ((uint)adapterLuid.HighPart << 32);
            IsSoftware = isSoftware;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.Append("Vendor: ").Append(VendorId).AppendLine();
            builder.Append("Description: ").Append(Description).AppendLine();
            builder.Append("DeviceId: ").Append(DeviceId).AppendLine();
            builder.Append("SubSysId: ").Append(SubSysId).AppendLine();
            builder.Append("Revision: ").Append(Revision).AppendLine();
            builder.Append("DedicatedVideoMemory: ").Append(DedicatedVideoMemory).AppendLine();
            builder.Append("DedicatedSystemMemory: ").Append(DedicatedSystemMemory).AppendLine();
            builder.Append("SharedSystemMemory: ").Append(SharedSystemMemory).AppendLine();
            builder.Append("AdapterLuid: ").Append(AdapterLuid).AppendLine();
            builder.Append("IsSoftware: ").Append(IsSoftware).AppendLine();

            return builder.ToString();
        }

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose() => _adapter.Dispose();
    }
}
