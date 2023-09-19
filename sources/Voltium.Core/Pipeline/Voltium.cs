using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Common;
using static TerraFX.Interop.DirectX.DirectX;

namespace Voltium
{
    public static class Options
    {
        public static void DeclareDeviceRemovalSupport()
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17143, 0))
            {
                try
                {
                    Guard.ThrowIfFailed(DXGIDeclareAdapterRemovalSupport());
                }
                catch (EntryPointNotFoundException)
                {

                }
            }
        }
    }
}
