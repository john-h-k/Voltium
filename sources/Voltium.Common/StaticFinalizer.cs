using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Common
{
    internal sealed class StaticFinalizer
    {
        private GCHandle _this;
        private Action _finalize;
        public static void Create(Action finalize)
            => new StaticFinalizer(finalize);

        private StaticFinalizer(Action finalize)
        {
            _this = GCHandle.Alloc(_this, GCHandleType.Normal);
            _finalize = finalize;
        }

        ~StaticFinalizer()
        {
            _finalize();
        }
    }
}
