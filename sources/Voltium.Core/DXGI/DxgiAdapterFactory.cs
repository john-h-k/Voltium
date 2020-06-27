using System;
using TerraFX.Interop;
using Voltium.Common;

using static TerraFX.Interop.Windows;

namespace Voltium.Core.DXGI
{
    internal sealed unsafe class DxgiAdapterFactory : AdapterFactory
    {
        private ComPtr<IDXGIFactory2> _factory;

        public DxgiAdapterFactory()
        {
            using ComPtr<IDXGIFactory2> factory = default;
            Guard.ThrowIfFailed(CreateDXGIFactory1(factory.Guid, ComPtr.GetVoidAddressOf(&factory)));
            _factory = factory.Move();
        }

        internal override bool TryGetAdapterByIndex(uint index, out Adapter adapter)
        {
            using ComPtr<IDXGIAdapter1> dxgiAdapter = default;

            Guard.ThrowIfFailed(_factory.Get()->EnumAdapters1(index, ComPtr.GetAddressOf(&dxgiAdapter)));

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
                (desc.Flags & (int)DXGI_ADAPTER_FLAG.DXGI_ADAPTER_FLAG_SOFTWARE) != 0
            );
        }

        public override void Dispose()
        {
            _factory.Dispose();
        }
    }
}
