using System;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core
{
    /// <summary>
    /// A type used over the duration of an application execution for monitoring timing
    /// </summary>
    public sealed class ApplicationTimer
    {
        private ApplicationTimer()
        {
        }

        /// <summary>
        /// Creates a new <see cref="ApplicationTimer"/> and starts measuring elapsed time
        /// </summary>
        public static ApplicationTimer StartNew()
        {
            var timer = new ApplicationTimer();

            timer._qpcFrequency = QueryPerformanceFrequency();

            timer._qpcLastTime = QueryPerformanceCounter();

            timer._qpcMaxDelta = (ulong)(timer._qpcFrequency.QuadPart / 10);

            return timer;
        }

        /// <summary>
        /// The number of ticks that have elapsed between the last call to <see cref="Tick"/> and the one prior to it
        /// </summary>
        public ulong ElapsedTicks => _elapsedTicks;

        /// <summary>
        /// The number of seconds that have elapsed between the last call to <see cref="Tick"/> and the one prior to it
        /// </summary>
        public double ElapsedSeconds => TicksToSeconds(_elapsedTicks);

        /// <summary>
        /// The number of ticks that have elapsed since the timer was started or reset
        /// </summary>
        public ulong TotalTicks => _totalTicks;

        /// <summary>
        /// The number of seconds that have elapsed since the timer was started or reset
        /// </summary>
        public double TotalSeconds => TicksToSeconds(_totalTicks);

        /// <summary>
        /// The number of calls to <see cref="Tick"/> that have occurred
        /// </summary>
        public uint FrameCount => _frameCount;

        /// <summary>
        /// The current framerate
        /// </summary>
        public uint FramesPerSeconds => _framesPerSecond;

        /// <summary>
        /// Whether a fixed or variable timestep should be used
        /// </summary>
        public bool IsFixedTimeStep { get; set; }

        /// <summary>
        /// Sets the desired number of ticks to elapse each frame in fixed timestep mode
        /// </summary>
        public ulong TargetElapsedTicks { get; set; }

        /// <summary>
        /// Sets the desired number of seconds to elapse each frame in fixed timestep mode
        /// </summary>
        public double TargetElapsedSeconds { get => TargetElapsedTicks / TicksPerSecond; set => TargetElapsedTicks = (ulong)(value * TicksPerSecond); }

        /// <summary>
        /// Converts ticks to seconds
        /// </summary>
        /// <param name="seconds">The number of seconds</param>
        /// <returns>The number of ticks equivalent to <paramref name="seconds"/></returns>
        public static ulong SecondsToTicks(double seconds)
            => (ulong)(seconds * TicksPerSecond);

        /// <summary>
        ///  Converts seconds to ticks
        /// </summary>
        /// <param name="ticks">The number of ticks</param>
        /// <returns>The number of seconds equivalent to <paramref name="ticks"/></returns>
        public static double TicksToSeconds(ulong ticks)
            => (double)ticks / TicksPerSecond;

        /// <summary>
        /// Resets the elapsed time of the timer
        /// </summary>
        public void ResetElapsedTime()
        {
            _qpcLastTime = QueryPerformanceCounter();

            _leftOverTicks = 0;
            _framesPerSecond = 0;
            _framesThisSecond = 0;
            _qpcSecondCounter = 0;
        }

        /// <summary>
        /// Ticks the timer, indicating a single frame has elapsed
        /// </summary>
        /// in fixed timestep mode, or immediately, in variable timestep mode
        public void Tick(Action update)
        {
            var currentTime = QueryPerformanceCounter();

            var timeDelta = (ulong)(currentTime.QuadPart - _qpcLastTime.QuadPart);

            _qpcLastTime = currentTime;
            _qpcSecondCounter += timeDelta;

            if (timeDelta > _qpcMaxDelta)
            {
                timeDelta = _qpcMaxDelta;
            }

            timeDelta *= TicksPerSecond;
            timeDelta /= (ulong)_qpcFrequency.QuadPart;

            uint lastFrameCount = _frameCount;

            if (IsFixedTimeStep)
            {
                if ((ulong)Math.Abs((long)(timeDelta - TargetElapsedTicks)) < TicksPerSecond / 4000)
                {
                    timeDelta = TargetElapsedTicks;
                }

                _leftOverTicks += timeDelta;

                while (_leftOverTicks >= TargetElapsedTicks)
                {
                    _elapsedTicks = TargetElapsedTicks;
                    _totalTicks += TargetElapsedTicks;
                    _leftOverTicks -= TargetElapsedTicks;
                    _frameCount++;
                    update();
                }
            }
            else
            {
                _elapsedTicks = timeDelta;
                _totalTicks += timeDelta;
                _leftOverTicks = 0;
                _frameCount++;

                update();
            }

            if (_frameCount != lastFrameCount)
            {
                _framesThisSecond++;
            }

            if (_qpcSecondCounter >= (ulong)_qpcFrequency.QuadPart)
            {
                _framesPerSecond = _framesThisSecond;
                _framesThisSecond = 0;
                _qpcSecondCounter %= (ulong)_qpcFrequency.QuadPart;
            }
        }

        private static unsafe LARGE_INTEGER QueryPerformanceCounter()
        {
            LARGE_INTEGER counter;
            if (Windows.QueryPerformanceCounter(&counter) == Windows.TRUE)
            {
                return counter;
            }
            ThrowHelper.ThrowExternalException(Marshal.GetLastWin32Error());
            return default;
        }

        private static unsafe LARGE_INTEGER QueryPerformanceFrequency()
        {
            LARGE_INTEGER frequency;
            if (Windows.QueryPerformanceFrequency(&frequency) == Windows.TRUE)
            {
                return frequency;
            }
            ThrowHelper.ThrowExternalException(Marshal.GetLastWin32Error());
            return default;
        }

        private const ulong TicksPerSecond = 10000000;

        private LARGE_INTEGER _qpcFrequency;
        private LARGE_INTEGER _qpcLastTime;
        private ulong _qpcMaxDelta;

        private ulong _elapsedTicks;
        private ulong _totalTicks;
        private ulong _leftOverTicks;

        private uint _frameCount;
        private uint _framesPerSecond;
        private uint _framesThisSecond;
        private ulong _qpcSecondCounter;
    }
}
