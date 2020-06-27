using System;
using System.Text;
using TerraFX.Interop;
using Voltium.Common;

using static TerraFX.Interop.Windows;

namespace Voltium.Core.DXGI
{
    internal sealed unsafe class DxCoreAdapterFactory : AdapterFactory
    {
        private ComPtr<IDXCoreAdapterFactory> _factory;
        private ComPtr<IDXCoreAdapterList> _list;

        public DxCoreAdapterFactory()
        {
            using ComPtr<IDXCoreAdapterFactory> factory = default;
            using ComPtr<IDXCoreAdapterList> list = default;

            Guard.ThrowIfFailed(DXCoreCreateAdapterFactory(factory.Guid, ComPtr.GetVoidAddressOf(&factory)));

            const int numFilterAttributes = 1;
            Guid* filterAttributes = stackalloc Guid[numFilterAttributes] { DXCORE_ADAPTER_ATTRIBUTE_D3D12_GRAPHICS };
            Guard.ThrowIfFailed(factory.Get()->CreateAdapterList(numFilterAttributes, filterAttributes, list.Guid, ComPtr.GetVoidAddressOf(&list)));

            _factory = factory.Move();
            _list = list.Move();
        }

        internal override bool TryGetAdapterByIndex(uint index, out Adapter adapter)
        {
            using ComPtr<IDXCoreAdapter> dxcoreAdapter = default;

            Guard.ThrowIfFailed(_list.Get()->GetAdapter(index, dxcoreAdapter.Guid, ComPtr.GetVoidAddressOf(&dxcoreAdapter)));

            if (!dxcoreAdapter.Exists)
            {
                adapter = default;
                return false;
            }

            adapter = CreateAdapter(dxcoreAdapter.Move());

            return true;
        }

        private static Adapter CreateAdapter(ComPtr<IDXCoreAdapter> adapter)
        {
            nuint size;
            Guard.ThrowIfFailed(adapter.Get()->GetPropertySize(DXCoreAdapterProperty.DriverDescription, &size));

            // we do this because we don't want to overrwrite the mem of the buff
            // this just truncates if necessary
            // but buffer may return >requested size, so we use the size we requested for
            var realSize = (int)size;
            using var buff = RentedArray<byte>.Create(realSize);

            fixed (byte* pBuff = buff.Value)
            {
                Guard.ThrowIfFailed(adapter.Get()->GetProperty(DXCoreAdapterProperty.DriverDescription, (uint)realSize, pBuff));
            }

            GetProperty<DXCoreHardwareID>(DXCoreAdapterProperty.HardwareID, out var vendor);
            GetProperty<LUID>(DXCoreAdapterProperty.InstanceLuid, out var luid);
            GetProperty<bool>(DXCoreAdapterProperty.IsHardware, out var isHardware);
            GetProperty<ulong>(DXCoreAdapterProperty.DedicatedAdapterMemory, out var dedicatedVideoMemory);
            GetProperty<ulong>(DXCoreAdapterProperty.DedicatedSystemMemory, out var dedicatedSystemMemory);
            GetProperty<ulong>(DXCoreAdapterProperty.SharedSystemMemory, out var sharedSystemMemory);

            return new Adapter(
                adapter.AsIUnknown(),
                Encoding.UTF8.GetString(buff.Value),
                (AdapterVendor)vendor.vendorID,
                vendor.deviceID,
                vendor.subSysID,
                vendor.revision,
                dedicatedVideoMemory,
                dedicatedSystemMemory,
                sharedSystemMemory,
                luid,
                !isHardware
            );

            void GetProperty<T>(DXCoreAdapterProperty property, out T val) where T : unmanaged
            {
                T data;
                Guard.ThrowIfFailed(adapter.Get()->GetProperty(property, (uint)sizeof(T), &data));
                val = data;
            }
        }

        public override void Dispose()
        {
            _factory.Dispose();
            _list.Dispose();
        }
    }
}
