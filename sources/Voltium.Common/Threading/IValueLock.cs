using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Common.Threading
{
    internal interface IValueLock
    {
        public void Enter(ref bool taken);
        public void Exit();
    }
}
