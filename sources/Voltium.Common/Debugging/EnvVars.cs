using System;

namespace Voltium.Common.Debugging
{
    /// <summary>
    /// Defines the set of environment variables used by Voltium
    /// </summary>
    public static class EnvVars
    {
        /// <summary>
        /// Whether the build was compiled with DEBUG
        /// </summary>
        public static readonly bool IsDebug =
#if DEBUG
            true;
#else
            false;
#endif

        /// <summary>
        /// If true or 1,
        /// the debug layer internal helper type is enabled,
        /// for improved DirectX debugging in an environment where
        /// native debugging output is not supported
        /// </summary>
        public static readonly bool IsD3D12ShimEnabled = Environment.GetEnvironmentVariable("DISABLE_D3D12_SHIM") is not "1";
    }
}
