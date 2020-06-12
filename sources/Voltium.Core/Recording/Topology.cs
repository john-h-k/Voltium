using Voltium.Core.Pipeline;
using static TerraFX.Interop.D3D_PRIMITIVE_TOPOLOGY;

namespace Voltium.Core
{
    /// <summary>
    /// Represents the exact topology type used
    /// </summary>
    public enum Topology
    {
        /// <summary>
        /// The topology is undefined
        /// </summary>
        Unknown = D3D_PRIMITIVE_TOPOLOGY_UNDEFINED,

        /// <summary>
        /// Each vertex represents a single point. This is a type of <see cref="TopologyClass.Point"/>
        /// </summary>
        PointList = D3D_PRIMITIVE_TOPOLOGY_POINTLIST,

        /// <summary>
        /// Each set of 2 vertices represents a single line. This is a type of <see cref="TopologyClass.Line"/>
        /// </summary>
        LineList = D3D_PRIMITIVE_TOPOLOGY_LINELIST,

        /// <summary>
        /// Each set of 2 vertices represent a line, but each end vertex is shared by 2 lines.
        /// So Vertices { A, B, C, D } means a line from A->B, B->C, and C->D. This is a type of <see cref="TopologyClass.Line"/>
        /// </summary>
        LineStrip = D3D_PRIMITIVE_TOPOLOGY_LINESTRIP,

        /// <summary>
        /// Each set of 3 vertices represents a single triangle. This is a type of <see cref="TopologyClass.Triangle"/>
        /// </summary>
        TriangeList = D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST,

        /// <summary>
        /// Each set of 2 vertices represent a line, but each end vertex is shared by 2 lines
        /// So Vertices { A, B, C, D, E, F, G } means a triangle from A->B->C, C->D->E, and E->F->G. This is a type of <see cref="TopologyClass.Triangle"/>
        /// </summary>
        TriangleStrip = D3D_PRIMITIVE_TOPOLOGY_TRIANGLESTRIP
    }
}
