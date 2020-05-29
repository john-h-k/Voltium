using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Voltium.Common.Threading
{
    // for use by IValueLock and LockedQueue
    internal struct SpinLockWrapped : IValueLock
    {
        public SpinLockWrapped(bool enableThreadOwnerTracking)
        {
            _lock = new SpinLock(enableThreadOwnerTracking);
        }

        private SpinLock _lock;

        public void Enter(ref bool taken) => _lock.Enter(ref taken);

        public void Exit() => _lock.Exit();
    }
}
