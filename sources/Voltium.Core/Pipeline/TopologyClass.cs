using static TerraFX.Interop.D3D12_PRIMITIVE_TOPOLOGY_TYPE;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// Defines the type of topology
    /// </summary>
    public enum TopologyClass
    {
        /// <summary>
        /// Primitives are points
        /// </summary>
        Point = D3D12_PRIMITIVE_TOPOLOGY_TYPE_POINT,

        /// <summary>
        /// Primitives are lines
        /// </summary>
        Line = D3D12_PRIMITIVE_TOPOLOGY_TYPE_LINE,

        /// <summary>
        /// Primitives are triangles
        /// </summary>
        Triangle = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE,

        /// <summary>
        /// Primitives are control patches used in tesselation
        /// </summary>
        Patch = D3D12_PRIMITIVE_TOPOLOGY_TYPE_PATCH
    }
}
