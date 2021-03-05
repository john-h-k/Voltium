using System;
using Voltium.Core.Memory;

namespace Voltium.Core.Devices
{
    public unsafe interface INativeQueue : IDisposable
    {
        public INativeDevice Device { get; }

        public ulong Frequency { get; }

        public void QueryTimestamps(ulong* cpu, ulong* gpu);

        GpuTask Execute(
            ReadOnlySpan<CommandBuffer> cmds,
            ReadOnlySpan<GpuTask> dependencies
        );
    }
}
