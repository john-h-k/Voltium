using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.Pool;
using static TerraFX.Interop.Windows;
using SysDebug = System.Diagnostics.Debug;

namespace Voltium.Core.Devices
{
    internal unsafe partial class CommandQueue : IDisposable, IInternalD3D12Object
    {
        private readonly ComputeDevice _device;
        private ulong _lastFence;

        public readonly ExecutionContext Type;
        public readonly ulong Frequency;

#if D3D12
        internal ID3D12CommandQueue* GetQueue() => _queue.Ptr;
#else
        internal ulong GetQueue() => _queue;
#endif

        private static ulong StartingFenceForContext(ExecutionContext context) => 0; // context switch
        //{
        //    // we do this to prevent conflicts when comparing markers
        //    ExecutionContext.Copy => ulong.MaxValue / 4 * 0,
        //    ExecutionContext.Compute => ulong.MaxValue / 4 * 1,
        //    ExecutionContext.Graphics => ulong.MaxValue / 4 * 2,
        //    _ => 0xFFFFFFFFFFFFFFFF
        //};

        public partial CommandQueue(
            ComputeDevice device,
            ExecutionContext context,
            bool enableTdr
        );

        public partial GpuTask ExecuteCommandLists(ReadOnlySpan<ContextParams> lists);


        public bool TryQueryTimestamps(out ulong gpu, out ulong cpu)
        {
            fixed (ulong* pGpu = &gpu)
            fixed (ulong* pCpu = &cpu)
            {
                return TryQueryTimestamps(pGpu, pCpu);
            }
        }

        public partial bool TryQueryTimestamps(ulong* gpu, ulong* cpu);

        private static string GetListTypeName(ExecutionContext type) => type switch
        {
            ExecutionContext.Graphics => nameof(ExecutionContext.Graphics),
            ExecutionContext.Compute => nameof(ExecutionContext.Compute),
            ExecutionContext.Copy => nameof(ExecutionContext.Copy),
            _ => "Unknown"
        };

        internal void Idle() => GetSynchronizerForIdle().Block();

        public partial void Wait(in GpuTask waitable);

        public partial GpuTask Signal();

        public partial void Dispose();

        ID3D12Object* IInternalD3D12Object.GetPointer() => (ID3D12Object*)_queue.Ptr;
    }
}
