using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Common;

using static TerraFX.Interop.Windows;

namespace Voltium
{
    public static class Voltium
    {
        public static void DeclareDeviceRemovalSupport()
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
