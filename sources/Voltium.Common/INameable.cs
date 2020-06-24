using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;

namespace Voltium.Common
{
    internal interface INameable
    {
        unsafe ID3D12Object* GetNameable();
    }
}
