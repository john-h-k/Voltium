using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;

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
        public static void StartNew()
        {
            var timer = new ApplicationTimer();

            TryQueryPerformanceFrequency(out timer._qpcFrequency);

            TryQueryPerformanceCounter(out timer._qpcLastTime);

            timer._qpcMaxDelta = (ulong)(timer._qpcFrequency.QuadPart / 10);
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
        /// <param name="isFixedTimestep"><c>true</c> if a fixed timestep should be used, and <c>false</c> if a variable timestep should be used</param>
        public void SetFixedTimeStep(bool isFixedTimestep) { _isFixedTimeStep = isFixedTimestep; }

        /// <summary>
        /// Sets the desired number of ticks to elapse each frame in fixed timestep mode
        /// </summary>
        /// <param name="targetElapsed">The number of ticks to elapse each frame</param>
        public void SetTargetElapsedTicks(ulong targetElapsed) { _targetElapsedTicks = targetElapsed; }

        /// <summary>
        /// Sets the desired number of seconds to elapse each frame in fixed timestep mode
        /// </summary>
        /// <param name="targetElapsed">The number of seconds to elapse each frame</param>
        public void SetTargetElapsedSeconds(double targetElapsed) { _targetElapsedTicks = SecondsToTicks(targetElapsed); }

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
            TryQueryPerformanceCounter(out _qpcLastTime);

            _leftOverTicks = 0;
            _framesPerSecond = 0;
            _framesThisSecond = 0;
            _qpcSecondCounter = 0;
        }

        /// <summary>
        /// Ticks the timer, indicating a single frame has elapsed
        /// </summary>
        /// <param name="update">The callback to use when the desired timestep is reached,
        /// in fixed timestep mode, or immediately, in variable timestep mode
        /// </param>
        public void Tick(Action update)
        {
            TryQueryPerformanceCounter(out LARGE_INTEGER currentTime);

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

            if (_isFixedTimeStep)
            {
                if ((ulong)Math.Abs((long)(timeDelta - _targetElapsedTicks)) < TicksPerSecond / 4000)
                {
                    timeDelta = _targetElapsedTicks;
                }

                _leftOverTicks += timeDelta;

                while (_leftOverTicks >= _targetElapsedTicks)
                {
                    _elapsedTicks = _targetElapsedTicks;
                    _totalTicks += _targetElapsedTicks;
                    _leftOverTicks -= _targetElapsedTicks;
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

        private static unsafe void TryQueryPerformanceCounter(out LARGE_INTEGER lpPerformanceCounter)
        {
            fixed (LARGE_INTEGER* pPerformanceCounter = &lpPerformanceCounter)
            {
                if (Windows.QueryPerformanceCounter(pPerformanceCounter) == Windows.TRUE)
                {
                    return;
                }
            }
        }

        private static unsafe void TryQueryPerformanceFrequency(out LARGE_INTEGER lpFrequency)
        {
            fixed (LARGE_INTEGER* pFrequency = &lpFrequency)
            {
                if (Windows.QueryPerformanceFrequency(pFrequency) == Windows.TRUE)
                {
                    return;
                }
            }
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

        private bool _isFixedTimeStep;
        private ulong _targetElapsedTicks;
    }
}
