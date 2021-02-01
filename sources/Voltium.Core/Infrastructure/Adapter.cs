using System;
using System.Runtime.CompilerServices;
using System.Text;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Infrastructure
{
    /// <summary>
    /// Represents a DXGI adapter
    /// </summary>
    [GenerateEquality]
    public partial struct Adapter : IEquatable<Adapter>, IDisposable
    {
        /// <summary>
        /// Whether the <see cref="Adapter"/> is made by AMD
        /// </summary>
        public bool IsAmd => VendorId == AdapterVendor.Amd;


        /// <summary>
        /// Whether the <see cref="Adapter"/> is made by NVidia
        /// </summary>
        public bool IsNVidia => VendorId == AdapterVendor.NVidia;


        /// <summary>
        /// Whether the <see cref="Adapter"/> is made by Intel
        /// </summary>
        public bool IsIntel => VendorId == AdapterVendor.Intel;


        /// <summary>
        /// Whether the <see cref="Adapter"/> is a discrete (seperate) rather than integrated GPU
        /// </summary>
        public bool IsDiscrete => DedicatedVideoMemory > 0;


        /// <summary>
        /// The value of the <see cref="IDXGIAdapter1"/>
        /// </summary>
        internal unsafe IUnknown* GetAdapterPointer() => _adapter.Ptr;

        internal UniqueComPtr<IUnknown> _adapter;

        /// <summary>
        /// A string that contains the adapter description
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The PCI ID of the hardware vendor
        /// </summary>
        public AdapterVendor VendorId { get; }

        /// <summary>
        /// The PCI ID of the hardware device
        /// </summary>
        public uint DeviceId { get; }

        /// <summary>
        /// The PCI ID of the sub system
        /// </summary>
        public uint SubSysId { get; }

        /// <summary>
        /// The PCI ID of the revision number of the adapter
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
        ///  <see langword="true"/> if this adapter is implemented in software, else <see langword="false"/>
        /// </summary>
        public bool IsSoftware { get; }

        /// <summary>
        /// The <see cref="DeviceType"/> for this adapter, specifying whether it is a <see cref="DeviceType.ComputeOnly"/> device or a general purpose
        /// <see cref="DeviceType.GraphicsAndCompute"/> device
        /// </summary>
        public DeviceType Type { get; }

        public OutputEnumerator Outputs => new(this);

        public struct OutputEnumerator
        {
            internal OutputEnumerator(in Adapter adapter)
            { 

            }
        }

        /// <summary>
        /// Create a new instance of <see cref="Adapter"/>
        /// </summary>
        internal unsafe Adapter(
            UniqueComPtr<IUnknown> adapter,
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
            using var builder = StringHelpers.RentStringBuilder();

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

        /// <inheritdoc />
        public override int GetHashCode() => AdapterLuid.GetHashCode();

        /// <inheritdoc />
        public bool Equals(Adapter other) => AdapterLuid == other.AdapterLuid;

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose() => _adapter.Dispose();
    }


    public struct AdapterOutput
    {
        private UniqueComPtr<IDXGIOutput1> _output;

        public ColorSpace ColorSpace { get; }
    }

    public enum ColorSpace
    {
        Sdr,
        HdrScRgb,
        Hdr10
    }

    /// <summary>
    /// The memory info for an adapter
    /// </summary>
    public readonly struct AdapterMemoryInfo
    {
        /// <summary>
        /// Specifies the OS-provided video memory budget, in bytes,
        /// that the application should target.
        /// If CurrentUsage is greater than Budget,
        /// the application may incur stuttering or performance penalties due to background activity by the OS to provide other applications with a fair usage of video memory
        /// </summary>
        public ulong Budget { get; init; }

        /// <summary>
        /// Specifies the application’s current video memory usage, in bytes
        /// </summary>
        public ulong CurrentUsage { get; init; }

        /// <summary>
        /// The amount of video memory, in bytes, that the application has available for reservation
        /// </summary>
        public ulong AvailableForReservation { get; init; }

        /// <summary>
        /// The amount of video memory, in bytes, that is reserved by the application.
        /// The OS uses the reservation as a hint to determine the application’s minimum working set.
        /// Applications should attempt to ensure that their video memory usage can be trimmed to meet this requirement
        /// </summary>
        public ulong CurrentReservation { get; init; }
    }


    /// <summary>
    /// Defines the memory segment for an adapter
    /// </summary>
    public enum MemorySegment : uint
    {
        /// <summary>
        /// The local memory segment, which is closest to the adapter and fastest to work with
        /// </summary>
        Local = DXCoreSegmentGroup.Local,


        /// <summary>
        /// The nonlocal memory segment, which is CPU accessible and slower to access from the adapter
        /// </summary>
        NonLocal = DXCoreSegmentGroup.NonLocal,
    }
}
