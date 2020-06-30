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
        private uint _skipSoftwareAdapterOffset;

        public DxgiDeviceFactory()
        {
            using ComPtr<IDXGIFactory2> factory = default;
            Guard.ThrowIfFailed(CreateDXGIFactory1(factory.Guid, ComPtr.GetVoidAddressOf(&factory)));
            _factory = factory.Move();
        }

        internal override bool TryGetAdapterByIndex(uint index, out Adapter adapter)
        {
            using ComPtr<IDXGIAdapter1> dxgiAdapter = default;

            // We only set this to true in TryOrderByPreference which checks we have IDXGIFactory6 so we can hard cast _factory
            if (_enumByPreference)
            {
                while (true)
                {
                    Guard.ThrowIfFailed(
                        _factory.AsBase<IDXGIFactory6>()
                        .Get()->EnumAdapterByGpuPreference(
                                index + _skipSoftwareAdapterOffset,
                                // DXGI preference doesn't allow preferring hardware adapters, so we do that manually after filtering out the other hardware types
                                // We remove the hardware flag so DXGI doesn't complain
                                (DXGI_GPU_PREFERENCE)(_preference & ~DevicePreference.Hardware),
                                dxgiAdapter.Guid,
                                ComPtr.GetVoidAddressOf(&dxgiAdapter)
                        )
                    );


                    if (_preference.HasFlag(DevicePreference.Hardware))
                    {
                        DXGI_ADAPTER_DESC1 desc;
                        Guard.ThrowIfFailed(dxgiAdapter.Get()->GetDesc1(&desc));

                        if ((desc.Flags & (int)DXGI_ADAPTER_FLAG.DXGI_ADAPTER_FLAG_SOFTWARE) != 0)
                        {
                            _skipSoftwareAdapterOffset++;
                            continue;
                        }
                    }
                }
            }
            else
            {
                Guard.ThrowIfFailed(_factory.Get()->EnumAdapters1(index, ComPtr.GetAddressOf(&dxgiAdapter)));
            }

            if (!dxgiAdapter.Exists)
            {
                adapter = default;
                return false;
            }

            adapter = CreateAdapter(dxgiAdapter.Move());

            return true;
        }

        private static Adapter CreateAdapter(ComPtr<IDXGIAdapter1> dxgiAdapter)
        {
            var p = dxgiAdapter.Get();
            DXGI_ADAPTER_DESC1 desc;
            Guard.ThrowIfFailed(p->GetDesc1(&desc));

            var descText = new ReadOnlySpan<char>(desc.Description, 128).ToString();

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
