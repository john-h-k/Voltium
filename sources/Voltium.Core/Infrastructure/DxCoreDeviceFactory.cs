using System;
using System.Text;
using TerraFX.Interop;
using Voltium.Common;

using static TerraFX.Interop.Windows;

namespace Voltium.Core.Infrastructure
{
    internal sealed unsafe class DxCoreDeviceFactory : DeviceFactory
    {
        private UniqueComPtr<IDXCoreAdapterFactory> _factory;
        private UniqueComPtr<IDXCoreAdapterList> _list;
        private uint _hardwareAdapterSkip;
        private bool _softwareOnly;

        public override Adapter SoftwareAdapter => throw new NotSupportedException("Can't enum WARP on DxCore");

        public DxCoreDeviceFactory(DeviceType types = DeviceType.GraphicsAndCompute)
        {
            using UniqueComPtr<IDXCoreAdapterFactory> factory = default;
            using UniqueComPtr<IDXCoreAdapterList> list = default;

            Guard.ThrowIfFailed(DXCoreCreateAdapterFactory(factory.Iid, (void**)&factory));

            const int MaxNumFilterAttributes = 2;
            Guid* filterAttributes = stackalloc Guid[MaxNumFilterAttributes];

            uint i = 0;
            if (types.HasFlag(DeviceType.ComputeOnly))
            {
                filterAttributes[i++] = DXCORE_ADAPTER_ATTRIBUTE_D3D12_CORE_COMPUTE;
            }
            if (types.HasFlag(DeviceType.GraphicsAndCompute))
            {
                filterAttributes[i++] = DXCORE_ADAPTER_ATTRIBUTE_D3D12_GRAPHICS;
            }

            Guard.ThrowIfFailed(factory.Ptr->CreateAdapterList(i, filterAttributes, list.Iid, (void**)&list));

            _factory = factory.Move();
            _list = list.Move();
        }

        internal override bool TryGetAdapterByIndex(uint index, out Adapter adapter)
        {
            while (true)
            {
                using UniqueComPtr<IDXCoreAdapter> dxcoreAdapter = default;

                // Reached end of list
                if (_list.Ptr->GetAdapterCount() - (index + _hardwareAdapterSkip) == 0)
                {
                    adapter = default;
                    return false;
                }

                Guard.ThrowIfFailed(_list.Ptr->GetAdapter(index, dxcoreAdapter.Iid, (void**)&dxcoreAdapter));

                if (_softwareOnly)
                {
                    bool isSoftware;
                    Guard.ThrowIfFailed(dxcoreAdapter.Ptr->GetProperty(DXCoreAdapterProperty.IsHardware, sizeof(bool), &isSoftware));

                    if (!isSoftware)
                    {
                        _hardwareAdapterSkip++;
                        continue;
                    }
                }

                adapter = CreateAdapter(dxcoreAdapter.Move());
                return true;
            }
        }

        private enum D3DKMDT_GRAPHICS_PREEMPTION_GRANULARITY
        {
            D3DKMDT_GRAPHICS_PREEMPTION_NONE = 0,
            D3DKMDT_GRAPHICS_PREEMPTION_DMA_BUFFER_BOUNDARY = 100,
            D3DKMDT_GRAPHICS_PREEMPTION_PRIMITIVE_BOUNDARY = 200,
            D3DKMDT_GRAPHICS_PREEMPTION_TRIANGLE_BOUNDARY = 300,
            D3DKMDT_GRAPHICS_PREEMPTION_PIXEL_BOUNDARY = 400,
            D3DKMDT_GRAPHICS_PREEMPTION_SHADER_BOUNDARY = 500,
        }

        private enum D3DKMDT_COMPUTE_PREEMPTION_GRANULARITY
        {
            D3DKMDT_COMPUTE_PREEMPTION_NONE = 0,
            D3DKMDT_COMPUTE_PREEMPTION_DMA_BUFFER_BOUNDARY = 100,
            D3DKMDT_COMPUTE_PREEMPTION_DISPATCH_BOUNDARY = 200,
            D3DKMDT_COMPUTE_PREEMPTION_THREAD_GROUP_BOUNDARY = 300,
            D3DKMDT_COMPUTE_PREEMPTION_THREAD_BOUNDARY = 400,
            D3DKMDT_COMPUTE_PREEMPTION_SHADER_BOUNDARY = 500,
        }

        private static Adapter CreateAdapter(UniqueComPtr<IDXCoreAdapter> adapter)
        {
            nuint size;
            Guard.ThrowIfFailed(adapter.Ptr->GetPropertySize(DXCoreAdapterProperty.DriverDescription, &size));

            // we do this because we don't want to overrwrite the mem of the buff
            // this just truncates if necessary
            // but buffer may return >requested size, so we use the size we requested for
            var realSize = (int)size;
            using var buff = RentedArray<byte>.Create(realSize);

            fixed (byte* pBuff = buff.Value)
            {
                Guard.ThrowIfFailed(adapter.Ptr->GetProperty(DXCoreAdapterProperty.DriverDescription, (uint)realSize, pBuff));
            }

            GetProperty<DXCoreHardwareID>(DXCoreAdapterProperty.HardwareID, out var vendor);
            GetProperty<LUID>(DXCoreAdapterProperty.InstanceLuid, out var luid);
            GetProperty<bool>(DXCoreAdapterProperty.IsHardware, out var isHardware);
            GetProperty<ulong>(DXCoreAdapterProperty.DedicatedAdapterMemory, out var dedicatedVideoMemory);
            GetProperty<ulong>(DXCoreAdapterProperty.DedicatedSystemMemory, out var dedicatedSystemMemory);
            GetProperty<ulong>(DXCoreAdapterProperty.SharedSystemMemory, out var sharedSystemMemory);
            GetProperty<ulong>(DXCoreAdapterProperty.DriverVersion, out var driverVersion);
            GetProperty<D3DKMDT_COMPUTE_PREEMPTION_GRANULARITY>(DXCoreAdapterProperty.ComputePreemptionGranularity, out var computePreemption);
            GetProperty<D3DKMDT_GRAPHICS_PREEMPTION_GRANULARITY>(DXCoreAdapterProperty.ComputePreemptionGranularity, out var graphicsPreemption);
            
            DeviceType type;
            Guid graphics = DXCORE_ADAPTER_ATTRIBUTE_D3D12_GRAPHICS;
            Guid compute = DXCORE_ADAPTER_ATTRIBUTE_D3D12_CORE_COMPUTE;
            if (adapter.Ptr->IsAttributeSupported(&graphics))
            {
                type = DeviceType.GraphicsAndCompute;
            }
            else if (adapter.Ptr->IsAttributeSupported(&compute))
            {
                type = DeviceType.ComputeOnly;
            }
            else
            {
                ThrowHelper.ThrowPlatformNotSupportedException("Adapter does not support DXCore compute or D3D12 graphics");
                type = default;
            }

            return new Adapter(
                adapter.AsIUnknown().Move(),
                Encoding.UTF8.GetString(buff.Value),
                (AdapterVendor)vendor.vendorID,
                vendor.deviceID,
                vendor.subSysID,
                vendor.revision,
                dedicatedVideoMemory,
                dedicatedSystemMemory,
                sharedSystemMemory,
                luid,
                driverVersion,
                isSoftware: !isHardware,
                type,
                computePreemption switch
                {
                    D3DKMDT_COMPUTE_PREEMPTION_GRANULARITY.D3DKMDT_COMPUTE_PREEMPTION_NONE => ComputePreemptionGranularity.None,
                    D3DKMDT_COMPUTE_PREEMPTION_GRANULARITY.D3DKMDT_COMPUTE_PREEMPTION_DMA_BUFFER_BOUNDARY => ComputePreemptionGranularity.PerExecution,
                    D3DKMDT_COMPUTE_PREEMPTION_GRANULARITY.D3DKMDT_COMPUTE_PREEMPTION_DISPATCH_BOUNDARY => ComputePreemptionGranularity.PerDispatch,
                    D3DKMDT_COMPUTE_PREEMPTION_GRANULARITY.D3DKMDT_COMPUTE_PREEMPTION_THREAD_GROUP_BOUNDARY => ComputePreemptionGranularity.PerThreadGroup,
                    D3DKMDT_COMPUTE_PREEMPTION_GRANULARITY.D3DKMDT_COMPUTE_PREEMPTION_THREAD_BOUNDARY => ComputePreemptionGranularity.PerThread,
                    D3DKMDT_COMPUTE_PREEMPTION_GRANULARITY.D3DKMDT_COMPUTE_PREEMPTION_SHADER_BOUNDARY => ComputePreemptionGranularity.PerInstruction,
                    _ => 0
                },

                graphicsPreemption switch
                {
                    D3DKMDT_GRAPHICS_PREEMPTION_GRANULARITY.D3DKMDT_GRAPHICS_PREEMPTION_NONE => GraphicsPreemptionGranularity.None,
                    D3DKMDT_GRAPHICS_PREEMPTION_GRANULARITY.D3DKMDT_GRAPHICS_PREEMPTION_DMA_BUFFER_BOUNDARY => GraphicsPreemptionGranularity.PerExecution,
                    D3DKMDT_GRAPHICS_PREEMPTION_GRANULARITY.D3DKMDT_GRAPHICS_PREEMPTION_PRIMITIVE_BOUNDARY => GraphicsPreemptionGranularity.PerDraw,
                    D3DKMDT_GRAPHICS_PREEMPTION_GRANULARITY.D3DKMDT_GRAPHICS_PREEMPTION_TRIANGLE_BOUNDARY => GraphicsPreemptionGranularity.PerTriangle,
                    D3DKMDT_GRAPHICS_PREEMPTION_GRANULARITY.D3DKMDT_GRAPHICS_PREEMPTION_PIXEL_BOUNDARY => GraphicsPreemptionGranularity.PerPixel,
                    D3DKMDT_GRAPHICS_PREEMPTION_GRANULARITY.D3DKMDT_GRAPHICS_PREEMPTION_SHADER_BOUNDARY => GraphicsPreemptionGranularity.PerInstruction,
                    _ => 0
                }
            );

            void GetProperty<T>(DXCoreAdapterProperty property, out T val) where T : unmanaged
            {
                if (!adapter.Ptr->IsPropertySupported(property))
                {
                    val = default;
                    LogHelper.LogInformation($"DXCoreProperty '{property}' not supported by adapter");
                }

                T data;
                Guard.ThrowIfFailed(adapter.Ptr->GetProperty(property, (uint)sizeof(T), &data));
                val = data;
            }
        }

        public override bool TryEnablePreferentialOrdering(DevicePreference preference)
        {
            DXCoreAdapterPreference* pPreferences = stackalloc DXCoreAdapterPreference[3];
            int i = 0;

            _softwareOnly = preference.HasFlag(DevicePreference.Software) && !preference.HasFlag(DevicePreference.Hardware);

            if (!TryAdd(DevicePreference.Hardware, DXCoreAdapterPreference.Hardware))
            {
                return false;
            }
            if (!TryAdd(DevicePreference.LowPower, DXCoreAdapterPreference.MinimumPower))
            {
                return false;
            }
            if (!TryAdd(DevicePreference.HighPerformance, DXCoreAdapterPreference.HighPerformance))
            {
                return false;
            }

            Guard.ThrowIfFailed(_list.Ptr->Sort((uint)i, pPreferences));

            return true;

            bool TryAdd(DevicePreference devicePref, DXCoreAdapterPreference dxCorePref)
            {
                if (!preference.HasFlag(devicePref))
                {
                    return true;
                }

                if (!_list.Ptr->IsAdapterPreferenceSupported(dxCorePref))
                {
                    return false;
                }

                StackSentinel.StackAssert(i < 3);
                pPreferences[i++] = dxCorePref;
                return true;
            }
        }

        public override void Dispose()
        {
            _factory.Dispose();
            _list.Dispose();
        }
    }
}
