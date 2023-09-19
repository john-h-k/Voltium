using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using static TerraFX.Interop.Vulkan;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Common;

namespace Voltium.Core.NativeApi.Vulkan
{
    public sealed class VulkanNativeQueue : INativeQueue
    {
        private VulkanNativeDevice _device;
        private VkQueue _queue;
        private Queue<InFlightAllocator> _pools;
        private FenceHandle _fenceHandle;
        private VkSemaphore _fence;
        private readonly object _lock = new();
        private ulong _fenceValue = 0;

        private struct InFlightAllocator
        {
            public VkCommandPool Allocator;
            public ulong Finish;
        }

        public INativeDevice Device => _device;

        internal VulkanNativeQueue(VulkanNativeDevice device, ExecutionEngine type)
        {
        }

        public ulong Frequency { get; }

        public void Dispose() => throw new NotImplementedException();

        public unsafe GpuTask Execute(ReadOnlySpan<CommandBuffer> cmds, ReadOnlySpan<GpuTask> dependencies)
        {
            if (cmds.IsEmpty && dependencies.IsEmpty)
            {
                return GpuTask.Completed;
            }

            lock (_lock)
            {
                var list = GetCommandBuffer();

                if (!cmds.IsEmpty)
                {
                    Encode(cmds, list);

                    _device.ThrowIfFailed(vkEndCommandBuffer(list));

                    var fence = _fence;


                    using var waits = RentedArray<VkSemaphore>.Create(dependencies.Length);
                    using var waitStages = RentedArray<VkPipelineStageFlags>.Create(dependencies.Length);

                    waitStages.AsSpan().Fill(VkPipelineStageFlags.VK_PIPELINE_STAGE_ALL_COMMANDS_BIT);

                    int i = 0;
                    foreach (ref readonly var dependency in dependencies)
                    {
                        waits.Value[i] = _device.GetFence(dependency._fence).Fence;
                    }

                    fixed (VkSemaphore* pWaits = waits.Value)
                    fixed (VkPipelineStageFlags* pWaitStages = waitStages.Value)
                    {
                        var submitInfo = new VkSubmitInfo
                        {
                            sType = VkStructureType.VK_STRUCTURE_TYPE_SUBMIT_INFO,
                            commandBufferCount = 1,
                            pCommandBuffers = (IntPtr*)&list,
                            signalSemaphoreCount = 1,
                            pSignalSemaphores = (ulong*)&fence,
                            pWaitDstStageMask = pWaitStages,
                            waitSemaphoreCount = (uint)waits.Length,
                            pWaitSemaphores = (ulong*)pWaits
                        };

                        _device.ThrowIfFailed(vkQueueSubmit(_queue, 1, &submitInfo, VK_NULL_HANDLE));

                        _fenceValue++;
                    }

                    ReturnCommandBuffer(list, _fenceValue);
                }

                return new(_device, _fenceHandle, _fenceValue);
            }
        }
        public unsafe void QueryTimestamps(ulong* cpu, ulong* gpu)
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

            var timestamps = stackalloc ulong[2] { 0, 0 };
            ulong deviation;

            _device.ThrowIfFailed(vkGetCalibratedTimestampsEXT(_device.GetDevice(), 2, infos, timestamps, &deviation));

            *gpu = timestamps[0];
            *cpu = timestamps[1];
        }

        private VkCommandBuffer GetCommandBuffer()
        {
        }

        private void ReturnCommandBuffer(VkCommandBuffer buffer, ulong fenceValue)
        {
            
        }

        private unsafe void Encode(ReadOnlySpan<CommandBuffer> cmdBuffs, VkCommandBuffer encode)
        {


            foreach (var cmdBuff in cmdBuffs)
            {
                var buff = cmdBuff.Buffer.Span;
                fixed (byte* pBuff = &buff[0])
                {
                    byte* pPacketStart = pBuff;
                    byte* pPacketEnd = pPacketStart + buff.Length;

                    while (pPacketStart < pPacketEnd)
                    {
                        var cmd = (Command*)pPacketStart;
                        switch (cmd->Type)
                        {
                        }
                    }
                }
            }
    }
}
