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
        public string Description { get; }

        /// <summary>
        /// The PCI ID of the hardware vendor. On feature level 9 graphics hardware, GetDesc1 returns zeros for the PCI ID of the hardware vendor.
        /// </summary>
        public AdapterVendor VendorId { get; }

        /// <summary>
        /// The PCI ID of the hardware device. On feature level 9 graphics hardware, GetDesc1 returns zeros for the PCI ID of the hardware device.
        /// </summary>
        public uint DeviceId { get; }

        /// <summary>
        /// The PCI ID of the sub system. On feature level 9 graphics hardware, GetDesc1 returns zeros for the PCI ID of the sub system.
        /// </summary>
        public uint SubSysId { get; }

        /// <summary>
        /// The PCI ID of the revision number of the adapter. On feature level 9 graphics hardware, GetDesc1 returns zeros for the PCI ID of the revision number of the adapter.
        /// </summary>
        public uint Revision { get; }

        /// <summary>
        /// The size, in bytes, of the video memory for the adapter, and not accessible by the CPU
        /// </summary>
        public ulong DedicatedVideoMemory { get; }

        /// <summary>
        /// The size, in bytes, of the system memory dedicated to the adapter, and not accessible by the CPU
        /// </summary>
        public ulong DedicatedSystemMemory { get; }

        /// <summary>
        /// The size, in bytes, of the maximum amount of shared system memory the adapter can use
        /// </summary>
        public ulong SharedSystemMemory { get; }

        /// <summary>
        /// A locally-unique identifier for the adapter
        /// </summary>
        public ulong AdapterLuid { get; }

        /// <summary>
        /// The driver version for the adapter
        /// </summary>
        public ulong DriverVersion { get; }

        /// <summary>
        ///  <code>true</code> if this adapter is implemented in software, else <code>false</code>
        /// </summary>
        public bool IsSoftware { get; }

        /// <summary>
        /// The <see cref="DeviceType"/> for this adapter, specifying whether it is a <see cref="DeviceType.ComputeOnly"/> device or a general purpose
        /// <see cref="DeviceType.GraphicsAndCompute"/> device
        /// </summary>
        public DeviceType Type { get; }

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
