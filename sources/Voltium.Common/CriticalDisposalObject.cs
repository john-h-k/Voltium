using System;
using System.Runtime.ConstrainedExecution;

namespace Voltium.Common
{
    internal abstract class CriticalDisposalObject : CriticalFinalizerObject, IDisposable
    {
        public abstract void Dispose();
    }
}
