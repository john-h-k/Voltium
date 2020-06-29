using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using static Voltium.Common.MethodTypes;

// ReSharper disable RedundantAssignment

namespace Voltium.Common.Threading
{
    /// <summary>
    /// Provided a lightweight spin lock for synchronization in high performance
    /// scenarios with a low hold time
    /// </summary>
#if TRACE_SPINLOCK_THREADS
    [DebuggerDisplay("Acquired = {" + nameof(_acquired) + " != 0}, OwnerThreadId = {" + nameof(_acquired) + "}")]
#else
    [DebuggerDisplay("Define 'TRACE_SPINLOCK_THREADS' for best debugging. Acquired = {_acquire == 1 ? true : false}")]
#endif
    public struct SpinLockSlim : IValueLock
    {
        // these are used for clarity, as we use 'int' not bool here, as interlocked ops need to be on an int
        private static int True =>
#if TRACE_SPINLOCK_THREADS
            Thread.CurrentThread.ManagedThreadId;
#else
            1 | (1 << 24); // this means the IsAcquired trick still works on big endian systems
#endif
        private static int False => 0;

        private volatile int _acquired; // either 1 or 0


        /// <summary>
        /// Returns <c>true</c> if the lock is acquired, else <c>false</c>
        /// </summary>
#pragma warning disable 420 // Unsafe.As<,> doesn't read the reference so the lack of volatility is not an issue, but we do need to treat the returned reference as volatile
        public bool IsAcquired =>
#if TRACE_SPINLOCK_THREADS
            _acquired != False;
#else
            Volatile.Read(ref Unsafe.As<int, bool>(ref _acquired));
#endif
#pragma warning restore 420

        /// <summary>
        /// Enter the lock. If this method returns, <paramref name="taken"/>
        /// will be <c>true</c>. If an exception occurs, <paramref name="taken"/> will indicate
        /// whether the lock was taken and needs to be released using <see cref="Exit()"/>.
        /// This method may never exit
        /// </summary>
        /// <param name="taken">A reference to a bool that indicates whether the lock is taken. Must
        /// be <c>false</c> when passed, else the internal state or return state may be corrupted.
        /// If the method returns, this is guaranteed to be <c>true</c></param>
        [MethodImpl(HighPerformance)]
        public void Enter(ref bool taken)
        {
            ValidateLockEntry(ref taken);

            // while acquired == 1, loop, then when it == 0, exit and set it to 1
            while (InternalTryAcquire())
            {
                // NOP
            }

            taken = true;
        }

        /// <summary>
        /// Enter the lock if it not acquired, else, do not. <paramref name="taken"/> will be
        /// <c>true</c> if the lock was taken, else <c>false</c>. If <paramref name="taken"/> is
        /// <c>true</c>, <see cref="Exit()"/> must be called to release it, else, it must not be called
        /// </summary>
        /// <param name="taken">A reference to a bool that indicates whether the lock is taken. Must
        /// be <c>false</c> when passed, else the internal state or return state may be corrupted</param>
        [MethodImpl(HighPerformance)]
        public void TryEnter(ref bool taken)
        {
            ValidateLockEntry(ref taken);

            // if it acquired == 0, change it to 1 and return true, else return false
            taken = InternalTryAcquire();
        }

        /// <summary>
        /// Try to safely enter the lock a certain number of times (<paramref name="iterations"/>).
        /// <paramref name="taken"/> will be <c>true</c> if the lock was taken, else <c>false</c>.
        /// If <paramref name="taken"/> is <c>true</c>, <see cref="Exit()"/> must be called to release
        /// it, else, it must not be called
        /// </summary>
        /// <param name="taken">A reference to a bool that indicates whether the lock is taken. Must
        /// be <c>false</c> when passed, else the internal state or return state may be corrupted</param>
        /// <param name="iterations">The number of attempts to acquire the lock before returning
        /// without the lock</param>
        [MethodImpl(HighPerformance)]
        public void TryEnter(ref bool taken, uint iterations)
        {
            ValidateLockEntry(ref taken);

            // if it acquired == 0, change it to 1 and return true, else return false
            while (InternalTryAcquire())
            {
                if (unchecked(iterations--) == 0) // postfix decrement, so no issue if iterations == 0 at first
                {
                    return;
                }
            }

            taken = true;
        }

        /// <summary>
        /// Try to safely enter the lock for a certain <see cref="TimeSpan"/> (<paramref name="timeout"/>).
        /// <paramref name="taken"/> will be <c>true</c> if the lock was taken, else <c>false</c>.
        /// If <paramref name="taken"/> is <c>true</c>, <see cref="Exit()"/> must be called to release
        /// it, else, it must not be called
        /// </summary>
        /// <param name="taken">A reference to a bool that indicates whether the lock is taken. Must
        /// be <c>false</c> when passed, else the internal state or return state may be corrupted</param>
        /// <param name="timeout">The <see cref="TimeSpan"/> to attempt to acquire the lock for before
        /// returning without the lock. A negative <see cref="TimeSpan"/>will cause undefined behaviour</param>
        [MethodImpl(HighPerformance)]
        public void TryEnter(ref bool taken, TimeSpan timeout)
        {
            EnsurePositiveTimeSpan(timeout);

            long start = Stopwatch.GetTimestamp();
            long end = ((timeout.Ticks / TimeSpan.TicksPerMillisecond) * Stopwatch.Frequency) + start;

            // if it acquired == 0, change it to 1 and return true, else return false
            while (InternalTryAcquire())
            {
                if (Stopwatch.GetTimestamp() >= end)
                {
                    return;
                }
            }

            taken = true;
        }

        /// <summary>
        /// Exit the lock
        /// </summary>
        [MethodImpl(HighPerformance)]
        public void Exit() => InternalExit();

        /// <summary>
        /// Exit the lock with an optional post-release memory barrier
        /// </summary>
        /// <param name="insertMemBarrier">Whether a memory barrier should be inserted after the release</param>
        [MethodImpl(HighPerformance)]
        public void Exit(bool insertMemBarrier)
        {
            InternalExit();

            if (insertMemBarrier)
            {
                Thread.MemoryBarrier();
            }
        }

        /// <summary>
        /// Exit the lock with a post-release memory barrier
        /// </summary>
        [MethodImpl(HighPerformance)]
        public void ExitWithBarrier()
        {
            InternalExit();

            Thread.MemoryBarrier();
        }

        [MethodImpl(HighPerformance)]
        // release the lock - int32 write will always be atomic
        private void InternalExit()
        {
            ValidateLockExit();

            _acquired = False;
        }

        [MethodImpl(HighPerformance)]
        private bool InternalTryAcquire()
        {
            return Interlocked.CompareExchange(ref _acquired, True, False) != False;
        }

        [Conditional("TRACE_SPINLOCK_THREADS")]
        [MethodImpl(Validates)]
        private void EnsurePositiveTimeSpan(TimeSpan time)
        {
            if (time.Ticks < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(time), time, "Negative timespan");
            }
        }

        [Conditional("TRACE_SPINLOCK_THREADS")]
        [MethodImpl(Validates)]
        private void ValidateLockEntry()
        {
            bool b = false;
            ValidateLockEntry(ref b);
        }

        [Conditional("TRACE_SPINLOCK_THREADS")]
        [MethodImpl(Validates)]
        private void ValidateLockEntry(ref bool value)
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;

            if (threadId == _acquired)
            {
                ThrowHelper.ThrowLockRecursionException(
                    $"Lock is owned by current thread (ThreadId {threadId}), yet the current thread is attempting to acquire the lock\n" +
                              "Lock recursion is not allowed.");
            }
            else if (value)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(value), value, "Lock taken bool was not false");
            }
        }

        [Conditional("TRACE_SPINLOCK_THREADS")]
        [MethodImpl(Validates)]
        private void ValidateLockExit()
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;

            if (threadId != _acquired)
            {
                ThrowHelper.ThrowSynchronizationLockException($"Lock is owned by thread {_acquired}, yet this thread {threadId} is trying to exit the lock");
            }
        }
    }
}
