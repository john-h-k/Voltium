using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Voltium.Common.Threading
{
    internal struct MonitorLock : IValueLock
    {
        private object _lock;

        public static MonitorLock Create(object? obj = null)
        {
            return new MonitorLock
            {
                _lock = obj ?? new()
            };
        }

        public void Enter(ref bool taken)
        {
            Monitor.Enter(_lock);
            taken = true;
        }

        public void Exit()
        {
            Monitor.Exit(_lock);
        }
    }
}
