using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public static bool IsDebug =
#if DEBUG
            true;
#else
            false;
#endif

        /// <summary>
        /// If true or 1,
        /// the <see cref="D3D12DebugShim"/> internal helper type is enabled,
        /// for improved DirectX debugging in an environment where
        /// native debugging output is not supported
        /// </summary>
        public static readonly bool IsD3D12ShimEnabled = Environment.GetEnvironmentVariable("DISABLE_D3D12_SHIM") is not "1";
    }
}
