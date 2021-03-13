using System;
using Voltium.Core.Memory;

namespace Voltium.Core.Devices
{
    /// <summary>
    /// The native queue type used to submit commands to a GPU
    /// </summary>
    public unsafe interface INativeQueue : IDisposable
    {
        /// <summary>
        /// The <see cref="INativeDevice"/> that owns this queue
        /// </summary> 
        public INativeDevice Device { get; }

        /// <summary>
        /// The frequency, in opaque ticks, of this queue
        /// </summary>
        public ulong Frequency { get; }


        /// <summary>
        /// Query the timestamps for the CPU and GPU at the same interval
        /// </summary>
        /// <param name="cpu">The CPU timestamp</param>
        /// <param name="gpu">The GPU timestamp</param>
        public void QueryTimestamps(ulong* cpu, ulong* gpu);

        /// <summary>
        /// Submit commands to the GPU
        /// </summary>
        /// <param name="cmds">The <see cref="CommandBuffer"/>s to execute on the GPU</param>
        /// <param name="dependencies">The <see cref="GpuTask"/>s that must be finished before execution of <paramref name="cmds"/></param>
        /// <returns>A new <see cref="GpuTask"/> representing the end of execution of <paramref name="cmds"/></returns>
        GpuTask Execute(
            ReadOnlySpan<CommandBuffer> cmds,
            ReadOnlySpan<GpuTask> dependencies
        );
    }
}
