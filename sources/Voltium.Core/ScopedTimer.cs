using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Toolkit.HighPerformance;

namespace Voltium.Core
{
    /// <summary>
    /// Represents a scoped timing operation
    /// </summary>
    public readonly ref struct ScopedTimer
    {
        private readonly Ref<TimeSpan> _outTime;
        private readonly ulong _startTicks;

        private ScopedTimer(out TimeSpan outTime)
        {
            Unsafe.SkipInit(out outTime);
            _outTime = new Ref<TimeSpan>(ref outTime);
            _startTicks = (ulong)Stopwatch.GetTimestamp();
        }

        /// <summary>
        /// Starts a new <see cref="ScopedTimer"/> block that will write to <paramref name="outTime"/> when <see cref="Dispose"/> is called
        /// </summary>
        /// <param name="outTime">The <see cref="TimeSpan"/> to write to</param>
        /// <returns>A new <see cref="ScopedTimer"/></returns>
        public static ScopedTimer Start(out TimeSpan outTime) => new ScopedTimer(out outTime);


        /// <summary>
        /// Starts a new <see cref="ScopedTimer"/> block
        /// </summary>
        /// <returns>A new <see cref="ScopedTimer"/></returns>
        public static unsafe ScopedTimer Start() => new ScopedTimer(out Unsafe.AsRef<TimeSpan>(null));

        /// <summary>
        /// The amount of time that has elapsed since this <see cref="ScopedTimer"/> was created
        /// </summary>
        public TimeSpan Elapsed
            => TimeSpan.FromSeconds(((ulong)Stopwatch.GetTimestamp() - _startTicks) / (double)Stopwatch.Frequency);

        /// <summary>
        /// Ends the <see cref="ScopedTimer"/> block
        /// </summary>
        public unsafe void Dispose()
        {
            if (!Unsafe.AreSame(ref _outTime.Value, ref Unsafe.AsRef<TimeSpan>(null)))
            {
                _outTime.Value = Elapsed;
            }
        }
    }
}
