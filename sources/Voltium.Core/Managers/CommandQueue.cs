using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Common.Threading;
using Voltium.Core.Contexts;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.Pool;
using static TerraFX.Interop.Windows;

namespace Voltium.Core.Devices
{
    

    public struct Heap
    {
        internal HeapHandle Handle;

        public readonly ulong Length;
        public HeapInfo Info;

        private Disposal<HeapHandle> _disposal;

        public Heap(HeapHandle handle, ulong length, HeapInfo info, Disposal<HeapHandle> disposal)
        {
            Handle = handle;
            Length = length;
            Info = info;
            _disposal = disposal;
        }
    }

    public unsafe sealed class CommandQueue : IDisposable
    {
        public ulong? QueueFrequency { get; }
        public ulong CpuFrequency { get; }
        public DeviceContext Context { get; }

        private INativeQueue _queue;

        public INativeQueue Native => _queue;

        public CommandQueue(INativeQueue queue, DeviceContext context, ArrayPool<CommandBuffer>? cmdBufPool = null)
        {
            _queue = queue;
            Context = context;
            _cmdBuffPool = cmdBufPool ?? ArrayPool<CommandBuffer>.Shared;
        }

        public void QueryTimestamps(out TimeSpan cpu, out TimeSpan gpu)
        {
            ulong gpuTick, cpuTick;

            QueryTimestamps(&cpuTick, &gpuTick);

            gpu = TimeSpan.FromSeconds(gpuTick / (double)QueueFrequency!.Value);
            cpu = TimeSpan.FromSeconds(cpuTick / (double)CpuFrequency);
        }

        public void QueryTimestamps(out ulong cpu, out ulong gpu)
        {
            fixed (ulong* pGpu = &gpu)
            fixed (ulong* pCpu = &cpu)
            {
                QueryTimestamps(pCpu, pGpu);
            }
        }

        [MemberNotNull(nameof(QueueFrequency))]
        public void QueryTimestamps(ulong* cpu, ulong* gpu)
        {
            if (QueueFrequency is null)
            {
                ThrowHelper.ThrowPlatformNotSupportedException($"Queue type '{Context}' does not support timestamps on this device");
            }

            _queue.QueryTimestamps(cpu, gpu);
        }

        private readonly object _lock = new();
        private readonly CommandBuffer[] _cmdBuffCache = new CommandBuffer[1];
        private readonly ArrayPool<CommandBuffer> _cmdBuffPool;

        public GpuTask Execute(GpuContext context) => Execute(context, ReadOnlySpan<GpuTask>.Empty);
        public GpuTask Execute(GpuContext context, in GpuTask dependency) => Execute(context, MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in dependency), 1));
        public GpuTask Execute(GpuContext context, ReadOnlySpan<GpuTask> dependencies)
        {
            lock (_lock)
            {
                _cmdBuffCache[0] = new CommandBuffer { FirstPipeline = context.FirstPipeline, Buffer = context.Commands };

                return _queue.Execute(_cmdBuffCache, dependencies);
            }
        }



        public GpuTask Execute(ReadOnlySpan<GpuContext> contexts) => Execute(contexts, default);
        public GpuTask Execute(ReadOnlySpan<GpuContext> contexts, in GpuTask dependency) => Execute(contexts, MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in dependency), 1));
        public GpuTask Execute(ReadOnlySpan<GpuContext> contexts, ReadOnlySpan<GpuTask> dependencies)
        {
            lock (_lock)
            {
                var cmdBuffers = _cmdBuffPool.Rent(contexts.Length);

                int i = 0;
                foreach (ref readonly var context in contexts)
                {
                    cmdBuffers[i] = new CommandBuffer { FirstPipeline = context.FirstPipeline, Buffer = context.Commands };
                }

                return _queue.Execute(cmdBuffers, dependencies);
            }
        }

        public readonly struct WorkLock : IDisposable
        {
            private readonly CommandQueue _queue;

            internal WorkLock(CommandQueue queue)
            {
                _queue = queue;
            }

            public void Release() => Dispose();
            public void Dispose()
            {
            }
        }

        public void Idle(ref WorkLock blocker)
        {
            blocker = new(this);
            //var idle = IdleTask;
            //idle.Block();
        }

        public void Dispose() => _queue.Dispose();
    }
}
