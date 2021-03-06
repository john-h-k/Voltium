using static TerraFX.Interop.D3D12_PRIMITIVE_TOPOLOGY_TYPE;
using static TerraFX.Interop.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE;
using TerraFX.Interop;
using System.Runtime.InteropServices;

namespace Voltium.Core.Pipeline
{
    public enum TopologyClass
    {
        Point = D3D12_PRIMITIVE_TOPOLOGY_TYPE_POINT,
        Line = D3D12_PRIMITIVE_TOPOLOGY_TYPE_LINE,
        Triangle = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE,
        Patch = D3D12_PRIMITIVE_TOPOLOGY_TYPE_PATCH,
    }
}
