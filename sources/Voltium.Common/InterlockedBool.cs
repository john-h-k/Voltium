using System.Drawing;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Extensions;
using System.Threading;
using Voltium.Common.Threading;

namespace Voltium.Core.Devices
{
    struct InterlockedBool
    {
        private const int True = 1;
        private const int False = 0;

        private volatile int _value;

        public bool Value
        {
            get => _value == True;
            set => _value = value ? True : False;
        }

        public bool Exchange(bool value)
        {
            return Interlocked.Exchange(ref _value, value ? True : False) == True;
        }

        public bool CompareExchange(bool value, bool comparand)
        {
            return Interlocked.CompareExchange(ref _value, value ? True : False, comparand ? True : False) == True;
        }

        public static implicit operator bool(InterlockedBool val) => val.Value;
    }
}
