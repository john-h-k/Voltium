using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Voltium.RenderEngine.Jobs
{
    internal unsafe static class LockedThreadPool
    {
        private static NativeThread[] _perCoreThread;
        private static NativeThread[] _ioThreads;

        private const int StackSize = 4 * 1024 * 1024;
        private const int IoStackSize = 4 * 1024 * 1024;
        private const int IoThreadCount = 4;

        static LockedThreadPool()
        {
            var logicalProcCount = Environment.ProcessorCount;

            _perCoreThread = new NativeThread[logicalProcCount];

            // we don't want 2 threads to be tied to the same core so we clamp down to the maximum
            // number of cores that SetAffinity supports (32 on 32 bit, 64 on 64)
            for (int i = 0; i < Math.Max(_perCoreThread.Length, sizeof(nuint) * 8); i++)
            {
                var thread = NativeThread.Create(
                    StackSize,
                    // TODO mark method as UnmanagedCallback or whatever
                    &NativeThreadEntryPoint,
                    null,
                    false
                );

                // this says "we can only run on this core", where each bit represents a logical core
                thread.SetAffinity(1U << i);

                _perCoreThread[i] = thread;
            }

            _ioThreads = new NativeThread[IoThreadCount];

            for (int i = 0; i < _ioThreads.Length; i++)
            {
                var thread = NativeThread.Create(
                    IoStackSize,
                    // TODO mark method as UnmanagedCallback or whatever
                    &NativeIoThreadEntryPoint,
                    null,
                    false
                );

                _ioThreads[i] = thread;
            }
        }

        [UnmanagedCallersOnly]
        private static uint NativeThreadEntryPoint(void* pData)
        {
            // non-zero == success, so we choose the least zero value possible
            return 0xFFFFFFFF;
        }

        [UnmanagedCallersOnly]
        private static uint NativeIoThreadEntryPoint(void* pData)
        {
            // non-zero == success, so we choose the least zero value possible
            return 0xFFFFFFFF;
        }
    }

    internal class LockedSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
        {
            //LockedThreadPool.QueueJob(d, state);
        }
    }
}
