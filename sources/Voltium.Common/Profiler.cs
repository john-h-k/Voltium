using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Voltium.Common.Threading;

namespace Voltium.Common
{
    /// <summary>
    /// Provides methods used for profiling
    /// </summary>
    public static class Profiler
    {
        /// <summary>
        /// <see cref="ProfilerBlockFlags"/> that will be applied to all profiler blocks
        /// </summary>
        public static ProfilerBlockFlags OverrideFlags { get; set; }
        private static Task _asyncOut = InitializeAsyncOut();

        /// <summary>
        /// The event that occurs when a block is profiled and dispatched. This may not be when the block finishes being profiled, 
        /// but rather when it is next dispatched by the dispatcher thread
        /// </summary>
        public static event Action<BlockData> OnBlockProfiled = (_) => { };

        private static Task InitializeAsyncOut()
        {
            StaticFinalizer.Create(FlushQueues);
            var task = Task.Factory.StartNew(AsyncOut, TaskCreationOptions.LongRunning);
            return task;
        }

        private static void AsyncOut()
        {
            while (true)
            {
                Thread.Sleep(1000);

                FlushQueues();
            }
        }

        private static LockedList<List<BlockData>, MonitorLock> _queueList = new(MonitorLock.Create());

        // Using a thread static means we don't need to lock when reading/writing and offset the cross thread concerns to the slow IO worker
        [ThreadStatic]
        private static List<BlockData>? _localBlocks;

        private static void FlushQueues()
        {
            for (var i = 0; i < _queueList.Count; i++)
            {
                var queue = _queueList[i];
                while (queue.Count > 0)
                {
                    var block = queue[queue.Count - 1];
                    queue.RemoveAt(queue.Count - 1);

                    OnBlockProfiled(block);
                    Console.WriteLine(block.ToString());
                }
            }
        }

        /// <summary>
        /// The data from a profiling block
        /// </summary>
        public struct BlockData
        {
            /// <summary>
            /// The name of this block
            /// </summary>
            public string Name;

            /// <summary>
            /// The <see cref="ProfilerBlockFlags"/> which determined how this block was profiled
            /// </summary>
            public ProfilerBlockFlags Flags;

            /// <summary>
            /// The <see cref="ulong"/> tickcount when this block started
            /// </summary>
            public long StartTick;

            /// <summary>
            /// The <see cref="ulong"/> tickcount when this block ended
            /// </summary>
            public long EndTick;

            /// <summary>
            /// The <see cref="TimeSpan"/> this entire block elapsed
            /// </summary>
            public TimeSpan Elapsed => TimeSpan.FromSeconds((EndTick - StartTick) / (double)Stopwatch.Frequency);

            /// <summary>
            /// The <see cref="ulong"/> number of bytes that were allocated on the current thread when this block started
            /// </summary>
            public long StartAllocatedBytes;

            /// <summary>
            /// The <see cref="ulong"/> number of bytes that were allocated on the current thread when this block ended
            /// </summary>
            public long EndAllocatedBytes;

            /// <summary>
            /// The <see cref="ulong"/> number of bytes that were allocated on the current thread during this block
            /// </summary>
            public long AllocatedBytes => EndAllocatedBytes - StartAllocatedBytes;

            /// <summary>
            /// The <see cref="GCMemoryInfo"/> recorded when this block started
            /// </summary>
            public GCMemoryInfo StartMemoryInfo;

            /// <summary>
            /// The <see cref="GCMemoryInfo"/> recorded when this block ended
            /// </summary>
            public GCMemoryInfo EndMemoryInfo;


            /// <summary>
            /// The <see cref="StackTrace"/> for this block
            /// </summary>
            public StackTrace? Trace;

            /// <summary>
            /// Returns the string representation of the given block
            /// </summary>
            public override string ToString()
            {
                var span = Elapsed;
                string time;
                if (span.TotalSeconds > 1)
                {
                    time = $"{span.TotalSeconds}s";
                }
                else
                {
                    time = $"{span.TotalMilliseconds}ms";
                }
                return $"Profile block '{Name}' took '{time}'";
            }
        }


        [MemberNotNull(nameof(_localBlocks))]
        private static void EnsureLocalBlocksNotNull()
        {
            if (_localBlocks is not null)
            {
                return;
            }

            _localBlocks = new();
        }

        /// <summary>
        /// Represents a single profiler block
        /// </summary>
        public struct Block : IDisposable
        {
            // To prevent bloating the stack, the block simply stores an ID to its index in the thread local list of blocks
            // We then write to this when the block is started/ended

            private int _id;

            private Block(int id, GCMemoryInfo i = default) => _id = id;

            internal static Block StartNew(string name, ProfilerBlockFlags flags)
            {
                Unsafe.SkipInit(out BlockData data);

                flags |= OverrideFlags;

                // this contrived pattern generates better asm when no flags are set (the most common case)
                if (data.Flags.HasFlag(ProfilerBlockFlags.CaptureGCMemoryInfo))
                {
                    data.StartMemoryInfo = GC.GetGCMemoryInfo();
                }
                if (data.Flags.HasFlag(ProfilerBlockFlags.CaptureGCAllocations))
                {
                    data.StartAllocatedBytes = GC.GetAllocatedBytesForCurrentThread();
                }
                if (data.Flags.HasFlag(ProfilerBlockFlags.CaptureStackTrace))
                {
                    data.Trace = new StackTrace();
                }

                EnsureLocalBlocksNotNull();

                _localBlocks.Add(data);

                // Create the final
                _localBlocks.GetRefUnsafe(_localBlocks.Count - 1).StartTick = Stopwatch.GetTimestamp();

                data.Name = name;
                data.Flags = flags;
                return new Block(_localBlocks.Count - 1);
            }

            /// <summary>
            /// Ends the current profiling block
            /// </summary>
            public void Dispose()
            {
                var endTick = Stopwatch.GetTimestamp();
                ref BlockData data = ref ListExtensions.GetRef(_localBlocks!, _id);
                data.EndTick = endTick;

                if (data.Flags.HasFlag(ProfilerBlockFlags.CaptureGCMemoryInfo))
                {
                    data.EndMemoryInfo = GC.GetGCMemoryInfo();
                }
                else if (data.Flags.HasFlag(ProfilerBlockFlags.CaptureGCAllocations))
                {
                    data.EndAllocatedBytes = GC.GetAllocatedBytesForCurrentThread();
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="Profiler.Block"/>
        /// </summary>
        /// <param name="blockName">The name of the block. By default, this is CallerMemberName</param>
        /// <param name="flags">The <see cref="ProfilerBlockFlags"/> for this block</param>
        /// <returns>A new <see cref="Block"/></returns>
        public static Block BeginProfileBlock([CallerMemberName] string blockName = null!, ProfilerBlockFlags flags = ProfilerBlockFlags.None)
        {
            return Block.StartNew(blockName, flags);
        }
    }

    /// <summary>
    /// Flags used by <see cref="Profiler"/>
    /// </summary>
    public enum ProfilerBlockFlags
    {
        /// <summary>
        /// No flags
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates the block should capture the stack trace when it is created
        /// </summary>
        CaptureStackTrace = 1 << 0,

        /// <summary>
        /// Indicates the block should capture the number of bytes allocated on the block's thread when it starts and ends
        /// </summary>
        CaptureGCAllocations = 1 << 1,

        /// <summary>
        /// Indicates the block should capture a <see cref="GCMemoryInfo" /> when it starts and ends
        /// </summary>
        CaptureGCMemoryInfo = 1 << 2,
    }
}
