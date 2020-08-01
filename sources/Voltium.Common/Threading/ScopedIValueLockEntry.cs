using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Toolkit.HighPerformance;

namespace Voltium.Common.Threading
{
    internal unsafe ref struct ScopedIValueLockEntry
    {
        public Ref<SpinLock> Lock;
        private bool _taken;

        public ScopedIValueLockEntry(ref SpinLock pLock)
        {
            Lock = new Ref<SpinLock>(ref pLock);
            _taken = false;
            Lock.Value.Enter(ref _taken);
        }

        public void Dispose()
        {
            if (_taken)
            {
                Lock.Value.Exit();
            }
        }
    }

    internal unsafe ref struct ScopedIValueLockEntry<TLock> where TLock : struct, IValueLock
    {
        public Ref<TLock> Lock;
        private bool _taken;

        public ScopedIValueLockEntry(ref TLock pLock)
        {
            Lock = new Ref<TLock>(ref pLock);
            _taken = false;
            Lock.Value.Enter(ref _taken);
        }

        public void Dispose()
        {
            if (_taken)
            {
                Lock.Value.Exit();
            }
        }
    }

    internal unsafe static class IValueLockExtensions
    {
        // Special case so we don't *need* to use SpinLockWrapped
        public static ScopedIValueLockEntry EnterScoped(ref this SpinLock @lock)
        {
            return new ScopedIValueLockEntry(ref @lock);
        }

        public static ScopedIValueLockEntry<TLock> EnterScoped<TLock>(ref this TLock @lock) where TLock : struct, IValueLock
        {
            return new ScopedIValueLockEntry<TLock>(ref @lock);
        }
    }
}
