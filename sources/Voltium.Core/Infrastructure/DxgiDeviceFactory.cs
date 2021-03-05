using System;
using TerraFX.Interop;
using Voltium.Common;

using static TerraFX.Interop.Windows;

namespace Voltium.Core.Infrastructure
{
    internal sealed unsafe class DxgiDeviceFactory : DeviceFactory
    {
        private UniqueComPtr<IDXGIFactory6> _factory;
        private bool _enumByPreference;
        private DevicePreference _preference;

        // used to skip software adapters, by adding to the index everytime we encounter one
        private uint _skipAdapterOffset;


        private Lazy<Adapter> _softwareAdapter;

        public override Adapter SoftwareAdapter => _softwareAdapter.Value;

        public DxgiDeviceFactory()
        {
            using UniqueComPtr<IDXGIFactory6> factory = default;
            Guard.ThrowIfFailed(CreateDXGIFactory2(0, factory.Iid, (void**)&factory));
            _factory = factory.Move();

            _softwareAdapter = new(() =>
            {
                using UniqueComPtr<IDXGIAdapter1> adapter = default;
                Guard.ThrowIfFailed(_factory.Ptr->EnumWarpAdapter(adapter.Iid, (void**)&adapter));
                return CreateAdapter(adapter.QueryInterface<IDXGIAdapter2>());
            });
        }

        internal override bool TryGetAdapterByIndex(uint index, out Adapter adapter)
        {
            // We only set this to true in TryOrderByPreference which checks we have IDXGIFactory6 so we can hard cast _factory
            if (_enumByPreference)
            {
                while (true)
                {
                    using UniqueComPtr<IDXGIAdapter2> dxgiAdapter = default;


                    Guard.ThrowIfFailed(
                        _factory.Ptr->EnumAdapterByGpuPreference(
                                index + _skipAdapterOffset,
                                // DXGI preference doesn't allow preferring hardware/software adapters, so we do that manually after filtering out the other hardware types
                                // We remove the hardware and software flag so DXGI doesn't complain
                                (DXGI_GPU_PREFERENCE)(_preference & ~(DevicePreference.Hardware | DevicePreference.Software)),
                                dxgiAdapter.Iid,
                                (void**)&dxgiAdapter
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
                        Guard.ThrowIfFailed(dxgiAdapter.Ptr->GetDesc1(&desc));
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
                using UniqueComPtr<IDXGIAdapter1> dxgiAdapter = default;
                using UniqueComPtr<IDXGIAdapter2> dxgiAdapter2 = default;

                Guard.ThrowIfFailed(_factory.Ptr->EnumAdapters1(index, ComPtr.GetAddressOf(&dxgiAdapter)));

                if (!dxgiAdapter.TryQueryInterface(out *&dxgiAdapter2))
                {
                    ThrowHelper.ThrowPlatformNotSupportedException("Unexpected");
                }

                adapter = CreateAdapter(dxgiAdapter2.Move());

                return true;
            }
        }

        private static Adapter CreateAdapter(UniqueComPtr<IDXGIAdapter2> dxgiAdapter)
        {
            var p = dxgiAdapter.Ptr;
            DXGI_ADAPTER_DESC2 desc;
            Guard.ThrowIfFailed(p->GetDesc2(&desc));

            LARGE_INTEGER driverVersion;
            Guid iid = IID_IDXGIDevice;
            Guard.ThrowIfFailed(dxgiAdapter.Ptr->CheckInterfaceSupport(&iid, &driverVersion));

            var descText = new string((char*)desc.Description);

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
                DeviceType.GraphicsAndCompute, // DXGI doesn't support enumerating non-graphics adapters
                desc.ComputePreemptionGranularity switch
                {
                    DXGI_COMPUTE_PREEMPTION_GRANULARITY.DXGI_COMPUTE_PREEMPTION_DMA_BUFFER_BOUNDARY => ComputePreemptionGranularity.PerExecution,
                    DXGI_COMPUTE_PREEMPTION_GRANULARITY.DXGI_COMPUTE_PREEMPTION_DISPATCH_BOUNDARY => ComputePreemptionGranularity.PerDispatch,
                    DXGI_COMPUTE_PREEMPTION_GRANULARITY.DXGI_COMPUTE_PREEMPTION_THREAD_GROUP_BOUNDARY => ComputePreemptionGranularity.PerThreadGroup,
                    DXGI_COMPUTE_PREEMPTION_GRANULARITY.DXGI_COMPUTE_PREEMPTION_THREAD_BOUNDARY => ComputePreemptionGranularity.PerThread,
                    DXGI_COMPUTE_PREEMPTION_GRANULARITY.DXGI_COMPUTE_PREEMPTION_INSTRUCTION_BOUNDARY => ComputePreemptionGranularity.PerInstruction,
                    _ => 0
                },

                desc.GraphicsPreemptionGranularity switch
                {
                    DXGI_GRAPHICS_PREEMPTION_GRANULARITY.DXGI_GRAPHICS_PREEMPTION_DMA_BUFFER_BOUNDARY => GraphicsPreemptionGranularity.PerExecution,
                    DXGI_GRAPHICS_PREEMPTION_GRANULARITY.DXGI_GRAPHICS_PREEMPTION_PRIMITIVE_BOUNDARY => GraphicsPreemptionGranularity.PerDraw,
                    DXGI_GRAPHICS_PREEMPTION_GRANULARITY.DXGI_GRAPHICS_PREEMPTION_TRIANGLE_BOUNDARY => GraphicsPreemptionGranularity.PerTriangle,
                    DXGI_GRAPHICS_PREEMPTION_GRANULARITY.DXGI_GRAPHICS_PREEMPTION_PIXEL_BOUNDARY => GraphicsPreemptionGranularity.PerPixel,
                    DXGI_GRAPHICS_PREEMPTION_GRANULARITY.DXGI_GRAPHICS_PREEMPTION_INSTRUCTION_BOUNDARY => GraphicsPreemptionGranularity.PerInstruction,
                    _ => 0
                }
            );
        }

        public override bool TryEnablePreferentialOrdering(DevicePreference preference)
        {
            _enumByPreference = true;
            _preference = preference;
            return true;
        }

        public override void Dispose()
        {
            _factory.Dispose();
        }
    }
}
