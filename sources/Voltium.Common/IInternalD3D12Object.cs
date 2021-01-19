using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;

namespace Voltium.Common
{
    /// <summary>
    /// Indicates a type internally contains a D3D12 object. This interface is for internal consumption and debugging tools only
    /// </summary>
    public interface IInternalD3D12Object
    {
#if D3D12
        internal unsafe ID3D12Object* GetPointer();
#else
        internal unsafe ulong GetPointer();
#endif
    }
}
