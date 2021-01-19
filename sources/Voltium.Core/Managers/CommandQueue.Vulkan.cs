using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using static TerraFX.Interop.Vulkan;
using Voltium.Core.Memory;
using Voltium.Core.Pool;
using Voltium.Common;
using System.Runtime.CompilerServices;

#if !D3D12

namespace Voltium.Core.Devices
{
    internal unsafe partial class CommandQueue
    {
        private VkQueue _queue;
        private VkSemaphore _fence;

        internal ulong GetQueue() => _queue;

        public partial GpuTask ExecuteCommandLists(ReadOnlySpan<ContextParams> lists, ReadOnlySpan<GpuTask> dependencies)
        {
            using RentedArray<VkCommandBuffer> rentedArray = default;
            Span<VkCommandBuffer> buff = default;
            if (StackSentinel.SafeToStackalloc<VkCommandBuffer>(lists.Length))
            {
                var ptr = stackalloc VkCommandBuffer[lists.Length];
                buff = new(ptr, lists.Length);
            }
            else
            {
                // fuck safety
                Unsafe.AsRef(in rentedArray) = RentedArray<VkCommandBuffer>.Create(lists.Length);
                buff = rentedArray.AsSpan();
            }

            int i = 0;
            foreach (ref readonly var list in lists)
            {
                buff[i++] = list.List;
            }

            fixed (VkCommandBuffer* pLists = buff)
            {
                var info = new VkSubmitInfo
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_GROUP_SUBMIT_INFO,

                };
            }
        }


        private struct CalibratedTimestamp { public ulong Gpu, Cpu; };

        public partial bool TryQueryTimestamps(ulong* gpu, ulong* cpu)
        {

            var infos = stackalloc VkCalibratedTimestampInfoEXT[2]
            {
                new VkCalibratedTimestampInfoEXT
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_CALIBRATED_TIMESTAMP_INFO_EXT,
                    timeDomain = VkTimeDomainEXT.VK_TIME_DOMAIN_DEVICE_EXT
                },
                new VkCalibratedTimestampInfoEXT
                {
                    sType = VkStructureType.VK_STRUCTURE_TYPE_CALIBRATED_TIMESTAMP_INFO_EXT,
                    timeDomain = VkTimeDomainEXT.VK_TIME_DOMAIN_QUERY_PERFORMANCE_COUNTER_EXT
                }
            };

            Windows.QueryPerformanceCounter((LARGE_INTEGER*)cpu);

            CalibratedTimestamp timestamps;
            ulong deviation;

            var success = vkGetCalibratedTimestampsEXT(_device.DevicePointer, 2, infos, (ulong*)&timestamps, &deviation) >= 0;

            *cpu = timestamps.Cpu;
            *gpu = timestamps.Gpu;

            return success;
        }

        internal partial GpuTask GetSynchronizerForIdle() => Signal();
        internal partial void Idle() => _device.ThrowIfFailed(vkQueueWaitIdle(_queue));

        public partial void Wait(in GpuTask waitable)
        {
            vkQueu
        }

        public partial GpuTask Signal();

        public partial void Dispose();
    }
}

#endif
