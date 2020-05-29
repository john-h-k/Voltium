using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Voltium.Common.Threading
{
    internal unsafe ref struct ScopedIValueLockEntry
    {
        private Span<SpinLock> _lockDummyRef;
        public ref SpinLock Lock => ref MemoryMarshal.GetReference(_lockDummyRef);
        private bool _taken;

        public ScopedIValueLockEntry(ref SpinLock pLock)
        {
            _lockDummyRef = MemoryMarshal.CreateSpan(ref pLock, 1);
            _taken = false;
            Lock.Enter(ref _taken);
        }

        public void Dispose()
        {
            if (_taken)
            {
                Lock.Exit();
            }
        }
    }

    internal unsafe ref struct ScopedIValueLockEntry<TLock> where TLock : unmanaged, IValueLock
    {
        private Span<TLock> _lockDummyRef;
        public ref TLock Lock => ref MemoryMarshal.GetReference(_lockDummyRef);
        private bool _taken;

        public ScopedIValueLockEntry(ref TLock pLock)
        {
            _lockDummyRef = MemoryMarshal.CreateSpan(ref pLock, 1);
            _taken = false;
            Lock.Enter(ref _taken);
        }

        public void Dispose()
        {
            if (_taken)
            {
                Lock.Exit();
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

        public static ScopedIValueLockEntry<TLock> EnterScoped<TLock>(ref this TLock @lock) where TLock : unmanaged, IValueLock
        {
            return new ScopedIValueLockEntry<TLock>(ref @lock);
        }
    }
}
