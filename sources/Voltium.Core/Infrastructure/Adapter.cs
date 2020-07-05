using System;
using System.Text;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Infrastructure
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
        /// The driver version for the adapter
        /// </summary>
        public readonly ulong DriverVersion;

        /// <summary>
        ///  <code>true</code> if this adapter is implemented in software, else <code>false</code>
        /// </summary>
        public readonly bool IsSoftware;

        /// <summary>
        /// The <see cref="DeviceType"/> for this adapter, specifying whether it is a <see cref="DeviceType.ComputeOnly"/> device or a general purpose
        /// <see cref="DeviceType.GraphicsAndCompute"/> device
        /// </summary>
        public readonly DeviceType Type;

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
            ulong driverVersion,
            bool isSoftware,
            DeviceType type
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
            DriverVersion = driverVersion;
            IsSoftware = isSoftware;
            Type = type;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            using var builder = StringHelper.RentStringBuilder();

            builder.Append("Vendor: ").AppendLine(VendorId);
            builder.Append("Description: ").AppendLine(Description);
            builder.Append("DeviceId: ").AppendLine(DeviceId);
            builder.Append("SubSysId: ").AppendLine(SubSysId);
            builder.Append("Revision: ").AppendLine(Revision);
            builder.Append("DedicatedVideoMemory: ").AppendLine(DedicatedVideoMemory);
            builder.Append("DedicatedSystemMemory: ").AppendLine(DedicatedSystemMemory);
            builder.Append("SharedSystemMemory: ").AppendLine(SharedSystemMemory);
            builder.Append("AdapterLuid: ").AppendLine(AdapterLuid);
            builder.Append("DriverVersion: ").AppendLine(DriverVersion);
            builder.Append("IsSoftware: ").AppendLine(IsSoftware);
            builder.Append("Type: ").AppendLine(Type);

            return builder.ToString();
        }

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose() => _adapter.Dispose();
    }
}
