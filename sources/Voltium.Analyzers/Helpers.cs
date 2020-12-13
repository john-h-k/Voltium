using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Text;

namespace Voltium.Analyzers
{
    internal static class Helpers
    {
        [Conditional("DEBUG")]
        public static void Assert([DoesNotReturnIf(false)] bool cond)
            => Debug.Assert(cond);


        [Conditional("DEBUG")]
        public static void Assert([DoesNotReturnIf(false)] bool cond, string message)
            => Debug.Assert(cond, message);
    }
}
