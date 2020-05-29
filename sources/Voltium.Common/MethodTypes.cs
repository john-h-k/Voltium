using System;
using System.Runtime.CompilerServices;

namespace Voltium.Common
{
    internal class MethodTypes
    {
        // Short methods should be inlined where possible
        public const MethodImplOptions ShortMethod = MethodImplOptions.AggressiveInlining;

        public const MethodImplOptions Inline = MethodImplOptions.AggressiveInlining;
        public const MethodImplOptions NoInline = MethodImplOptions.NoInlining;

        // Constant branches deceive the JIT into thinking code is bigger than it is
        // Inline it because we know the code will be smaller, and force tier1 so the dead branches are definitely eliminated
        public const MethodImplOptions HasConstantBranches =
            MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;

        // Unclear and annoying. Ban in this codebase
        [Obsolete("Manually lock. Don't use synchronised methods", true)]
        public const MethodImplOptions Sync = MethodImplOptions.Synchronized;

        // The slow infrequent path should be kept not inlined to not bloat the fast path method size
        public const MethodImplOptions SlowPath = MethodImplOptions.NoInlining;

        // Software fallback inlining should be decided by the JIT
        public const MethodImplOptions SoftwareFallback = default;

        // Specially used for debugging methods in release mode
        public const MethodImplOptions DebugInRelease = MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization;

        // Wrapper methods should be inlined as they perform no significant additional function
        public const MethodImplOptions Wrapper = MethodImplOptions.AggressiveInlining;

        // The fast path should be inlined where possible
        public const MethodImplOptions FastPath =
            MethodImplOptions.AggressiveInlining;

        // Hot code should be inlined for perf, and optimised immediately as there is no point making it get rejitted later
        public const MethodImplOptions HotCode =
            MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;

        // Self explanatory
        public const MethodImplOptions HighPerformance =
            MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;

        // We don't want 'throw' instructions polluting code except on slow path
        public const MethodImplOptions ThrowHelperMethod = MethodImplOptions.NoInlining;

        // Validation should be hoisted out
        public const MethodImplOptions Validates = MethodImplOptions.AggressiveInlining;
    }
}
