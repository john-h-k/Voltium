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
        /// If <code>true</code> or <code>1</code>,
        /// the <see cref="D3D12DebugShim"/> internal helper type is enabled,
        /// for improved DirectX debugging in an environment where
        /// native debugging output is not supported
        /// </summary>
        public const string IsD3D12ShimEnabled = "ENABLE_DX_DEBUG_SHIM";
    }
}