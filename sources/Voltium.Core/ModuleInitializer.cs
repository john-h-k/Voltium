using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core
{
    internal static class ModuleInitializer
    {
        [ModuleInitializer]
        internal static unsafe void Initializer()
        {
            var features = stackalloc[] { Windows.D3D12ExperimentalShaderModels, Windows.D3D12MetaCommand };
            Guard.ThrowIfFailed(Windows.D3D12EnableExperimentalFeatures(2, features, null, null));
        }
    }
}
