using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using static TerraFX.Interop.Vulkan;
using Voltium.Common;

namespace Voltium.Core.Infrastructure
{
    internal sealed unsafe class VkDeviceFactory : DeviceFactory
    {
        private VkInstance _instance;
        private VkPhysicalDevice[] _devices;

        public override Adapter SoftwareAdapter => ThrowHelper.ThrowPlatformNotSupportedException<Adapter>("Vulkan has no default SoftwareAdapter");

        public override bool TryEnablePreferentialOrdering(DevicePreference preference) => throw new NotImplementedException();
        internal override bool TryGetAdapterByIndex(uint index, out Adapter adapter)
        {
            if (index < _devices.Length)
            {
                adapter = CreateAdapter(_devices[index]);
                return true;
            }

            adapter = default;
            return false;
        }

        private unsafe Adapter CreateAdapter(VkPhysicalDevice device)
        {
            VkPhysicalDeviceProperties props;
            vkGetPhysicalDeviceProperties(device, &props);

            VkPhysicalDeviceMemoryProperties memProps;
            vkGetPhysicalDeviceMemoryProperties(device, &memProps);

            ulong dedicatedVram = 0, dedicatedSysRam = 0, sharedSysRam = 0;

            for (var i = 0; i < memProps.memoryHeapCount; i++)
            {
                if ((memProps.memoryHeaps[i].flags & (uint)VkMemoryHeapFlagBits.VK_MEMORY_HEAP_DEVICE_LOCAL_BIT) != 0)
                {
                    dedicatedVram = memProps.memoryHeaps[i].size;
                }
                else if ((memProps.memoryHeaps[i].flags & (uint)VkMemoryHeapFlagBits.VK_MEMORY_HEAP_DEVICE_LOCAL_BIT) != 0)
                {
                    sharedSysRam = memProps.memoryHeaps[i].size;
                }
            }

            System.Diagnostics.Debug.Assert(VK_UUID_SIZE == sizeof(Guid));

            return new Adapter(
                device,
                new string(props.deviceName, 0, VK_MAX_PHYSICAL_DEVICE_NAME_SIZE),
                (AdapterVendor)props.vendorID,
                props.deviceID,
                0,
                0,
                dedicatedVram,
                dedicatedSysRam,
                sharedSysRam,
                new Guid(new Span<byte>(props.pipelineCacheUUID, VK_UUID_SIZE)),
                props.driverVersion,
                props.deviceType == VkPhysicalDeviceType.VK_PHYSICAL_DEVICE_TYPE_CPU,
                DeviceType.GraphicsAndCompute // TODO
            );
        }

        public override void Dispose()
        {

        }
    }
}
