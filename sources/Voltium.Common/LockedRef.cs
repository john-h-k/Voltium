using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Voltium.Common
{
    internal struct LockedRef<T> where T : class
    {
        private T? _value;

        public bool TryGetValue([NotNullWhen(true)] out T? value)
        {
            value = Interlocked.Exchange(ref _value, null);
            return value is not null;
        }

        public T? GetValue()
        {
            return _value;
        }

        [MemberNotNull(nameof(_value))]
        public bool TrySetValue(T value)
        {
            return Interlocked.CompareExchange(ref _value, value, null) is null;
        }

        public void SetValue(T value)
        {
            _value = value;
        }
    }
}
