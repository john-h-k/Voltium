using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Voltium.Common;
using Voltium.Common.Threading;

namespace Voltium.RenderEngine.Jobs
{
    public enum JobPriority
    {
        Low,
        Standard,
        High
    }

    public class JobCounter
    {
        private volatile int _value;

        public int Value => _value;

        internal void Decrement() => Interlocked.Decrement(ref _value);
        internal void Increment() => Interlocked.Increment(ref _value);

        public JobTask Reaches(int value)
        {
            return new JobTask(this, value);
        }
    }

    public class JobSystem
    {
        public JobSystem Default { get; } = new();

        private LockedQueue<Job, SpinLockWrapped> _jobs;

        private struct Job
        {
            public JobCounter Counter;
            public Action SingleFunc;
        }

        public JobCounter Queue(Action job)
        {
            var counter = new JobCounter();
            Queue(job, counter);
            return counter;
        }

        public void Queue(Action job, JobCounter counter)
        {
            counter.Increment();
            var jobData = new Job { Counter = counter, SingleFunc = job };
            _jobs.Enqueue(jobData);
        }

        
    }

    public struct JobTask : INotifyCompletion
    {
        internal JobTask(JobCounter counter, int completedValue)
        {
            Counter = counter;
            CompletedValue = completedValue;
        }

        public JobCounter Counter { get; }
        public int CompletedValue { get; }

        public bool IsCompleted => Counter.Value <= CompletedValue;
        public void GetResult() { }
        public void OnCompleted(Action completion)
        {

        }
    }

    [AsyncMethodBuilder(typeof(JobTask))]
    public struct JobTaskBuilder
    {
        public static JobTaskBuilder Create() => new();

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {

        }
        public void SetException(Exception exception)
        {

        }
        public void SetResult()
        {

        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            try
            {
                awaiter.OnCompleted(GetStateMachineBox(ref stateMachine, ref taskField).MoveNextAction);
            }
            catch (Exception e)
            {
                System.Threading.Tasks.Task.ThrowAsync(e, targetContext: null);
            }
        }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {

        }

        public JobTask Task { get; }
    }

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
