using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Core.Callable
{
    public unsafe readonly struct Callable
    {
        private readonly delegate*<void> _fn;
        private readonly Action? _del;

        public Callable(delegate*<void> fn)
        {
            _fn = fn;
            _del = null;
        }

        public Callable(Action del)
        {
            _fn = null;
            _del = del;
        }

        public void Invoke()
        {
            if (_del is not null)
            {
                _del();
            }
            else
            {
                _fn();
            }
        }

        public static implicit operator Callable(delegate*<void> fn) => new Callable(fn);
        public static implicit operator Callable(Action del) => new Callable(del);

    }

    static class Foo
    {
        static void DoSomething(Callable c) => c.Invoke();

        static unsafe void Bar()
        {
            DoSomething((Callable)(&Bar));
            DoSomething((Callable)Bar);
        }
    }
}
