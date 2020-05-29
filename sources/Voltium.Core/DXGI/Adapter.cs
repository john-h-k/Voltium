using System;
using System.Collections;
using System.Collections.Generic;
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
        public unsafe IDXGIAdapter1* UnderlyingAdapter => _adapter.Get();

        private ComPtr<IDXGIAdapter1> _adapter;

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
        public readonly LUID AdapterLuid;

        /// <summary>
        ///  <code>true</code> if this adapter is implemented in software, else <code>false</code>
        /// </summary>
        public readonly bool IsSoftware;

        /// <summary>
        /// Create a new instance of <see cref="Adapter"/>
        /// </summary>
        public Adapter(
            ComPtr<IDXGIAdapter1> adapter,
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
            AdapterLuid = adapterLuid;
            IsSoftware = isSoftware;
        }

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose() => _adapter.Dispose();

        /// <summary>
        /// Enumerate adapters for a given factory
        /// </summary>
        /// <param name="factory">The factory used to enumerate</param>
        /// <returns>An <see cref="AdapterEnumerator"/></returns>
        public static AdapterEnumerator EnumerateAdapters(ComPtr<IDXGIFactory2> factory) => new AdapterEnumerator(factory);
    }

    /// <summary>
    ///
    /// </summary>
    public unsafe struct AdapterEnumerator : IEnumerator<Adapter>, IEnumerable<Adapter>
    {
        private ComPtr<IDXGIFactory2> _factory;
        private uint _index;

        /// <summary>
        /// Create a new <see cref="AdapterEnumerator"/>
        /// </summary>
        /// <param name="factory">The factory used to enumerate adapters</param>
        public AdapterEnumerator(ComPtr<IDXGIFactory2> factory)
        {
            _factory = factory;
            _index = 0;
            Current = default;
        }

        /// <inheritdoc cref="IEnumerator.MoveNext"/>
        public bool MoveNext()
        {
            Current.Dispose();

            IDXGIAdapter1* p;
            Guard.ThrowIfFailed(_factory.Get()->EnumAdapters1(_index++, &p));

            Current = CreateAdapter(p);
            return p != null;
        }

        private static Adapter CreateAdapter(ComPtr<IDXGIAdapter1> dxgiAdapter)
        {
            var p = dxgiAdapter.Get();
            DXGI_ADAPTER_DESC1 desc;
            Guard.ThrowIfFailed(p->GetDesc1(&desc));

            var descText = new ReadOnlySpan<char>(desc.Description, 128).ToString();

            return new Adapter(
                dxgiAdapter,
                descText,
                (AdapterVendor)desc.VendorId,
                desc.DeviceId,
                desc.SubSysId,
                desc.Revision,
                (ulong)desc.DedicatedVideoMemory,
                (ulong)desc.DedicatedSystemMemory,
                (ulong)desc.SharedSystemMemory,
                desc.AdapterLuid,
                (desc.Flags & (int)DXGI_ADAPTER_FLAG.DXGI_ADAPTER_FLAG_SOFTWARE) != 0
            );
        }

        /// <inheritdoc cref="IEnumerator.Reset"/>
        public void Reset() => _index = 0;

        /// <inheritdoc cref="IEnumerator{T}.Current"/>
        public Adapter Current { get; private set; }

        object? IEnumerator.Current => Current;

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose()
        {
            _factory.Dispose();
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public AdapterEnumerator GetEnumerator() => this;

        IEnumerator<Adapter> IEnumerable<Adapter>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
