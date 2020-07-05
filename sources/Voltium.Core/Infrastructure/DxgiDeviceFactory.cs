using System;
using TerraFX.Interop;
using Voltium.Common;

using static TerraFX.Interop.Windows;

namespace Voltium.Core.Infrastructure
{
    internal sealed unsafe class DxgiDeviceFactory : DeviceFactory
    {
        private ComPtr<IDXGIFactory2> _factory;
        private bool _enumByPreference;
        private DevicePreference _preference;

        // used to skip software adapters, by adding to the index everytime we encounter one
        private uint _skipAdapterOffset;

        public DxgiDeviceFactory()
        {
            using ComPtr<IDXGIFactory2> factory = default;
            Guard.ThrowIfFailed(CreateDXGIFactory1(factory.Iid, ComPtr.GetVoidAddressOf(&factory)));
            _factory = factory.Move();
        }

        internal override bool TryGetAdapterByIndex(uint index, out Adapter adapter)
        {

            // We only set this to true in TryOrderByPreference which checks we have IDXGIFactory6 so we can hard cast _factory
            if (_enumByPreference)
            {
                while (true)
                {
                    using ComPtr<IDXGIAdapter1> dxgiAdapter = default;


                    Guard.ThrowIfFailed(
                        _factory.AsBase<IDXGIFactory6>()
                        .Get()->EnumAdapterByGpuPreference(
                                index + _skipAdapterOffset,
                                // DXGI preference doesn't allow preferring hardware/software adapters, so we do that manually after filtering out the other hardware types
                                // We remove the hardware and software flag so DXGI doesn't complain
                                (DXGI_GPU_PREFERENCE)(_preference & ~(DevicePreference.Hardware | DevicePreference.Software)),
                                dxgiAdapter.Iid,
                                ComPtr.GetVoidAddressOf(&dxgiAdapter)
                        )
                    );

                    // null adapter means we have reached end of list
                    if (!dxgiAdapter.Exists)
                    {
                        adapter = default;
                        return false;
                    }

                    // if it only supports hardware of software, we have to filter them out. If both or neither are set, we allow all adapters through
                    if (_preference.HasFlag(DevicePreference.Hardware) != _preference.HasFlag(DevicePreference.Software))
                    {
                        DXGI_ADAPTER_DESC1 desc;
                        Guard.ThrowIfFailed(dxgiAdapter.Get()->GetDesc1(&desc));
                        bool isHardware = (desc.Flags & (int)DXGI_ADAPTER_FLAG.DXGI_ADAPTER_FLAG_SOFTWARE) == 0;

                        // If they want hardware but we don't have it, or they want software and we don't have it, skip this adapter
                        if (_preference.HasFlag(DevicePreference.Hardware) != isHardware)
                        {
                            _skipAdapterOffset++;
                            continue;
                        }
                    }

                    adapter = CreateAdapter(dxgiAdapter.Move());

                    return true;
                }
            }
            else
            {
                using ComPtr<IDXGIAdapter1> dxgiAdapter = default;

                Guard.ThrowIfFailed(_factory.Get()->EnumAdapters1(index, ComPtr.GetAddressOf(&dxgiAdapter)));
                adapter = CreateAdapter(dxgiAdapter.Move());

                return true;
            }
        }

        private static Adapter CreateAdapter(ComPtr<IDXGIAdapter1> dxgiAdapter)
        {
            var p = dxgiAdapter.Get();
            DXGI_ADAPTER_DESC1 desc;
            Guard.ThrowIfFailed(p->GetDesc1(&desc));

            LARGE_INTEGER driverVersion;
            Guid iid = IID_IDXGIDevice;
            Guard.ThrowIfFailed(dxgiAdapter.Get()->CheckInterfaceSupport(&iid, &driverVersion));


            var nullChar = new Span<char>(desc.Description, 128).IndexOf('\0');
            var descText = new string((char*)desc.Description, 0, nullChar);

            return new Adapter(
                dxgiAdapter.AsIUnknown(),
                descText,
                (AdapterVendor)desc.VendorId,
                desc.DeviceId,
                desc.SubSysId,
                desc.Revision,
                desc.DedicatedVideoMemory,
                desc.DedicatedSystemMemory,
                desc.SharedSystemMemory,
                desc.AdapterLuid,
                (ulong)driverVersion.QuadPart,
                (desc.Flags & (int)DXGI_ADAPTER_FLAG.DXGI_ADAPTER_FLAG_SOFTWARE) != 0,
                DeviceType.GraphicsAndCompute // DXGI doesn't support enumerating non-graphics adapters
            );
        }

        public override bool TryEnablePreferentialOrdering(DevicePreference preference)
        {
            _preference = preference;
            // Can only enum by preference if we have IDXGIFactory6 
            _enumByPreference = _factory.TryQueryInterface<IDXGIFactory6>(out var factory);
            factory.Dispose();
            return _enumByPreference;
        }

        public override void Dispose()
        {
            _factory.Dispose();
        }
    }
}
