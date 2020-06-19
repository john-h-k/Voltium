using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Common
{
    internal abstract class CriticalDisposalObject : CriticalFinalizerObject, IDisposable
    {
        public abstract void Dispose();
    }
}
