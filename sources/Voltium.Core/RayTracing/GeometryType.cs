using TerraFX.Interop;

namespace Voltium.Core
{
    public enum GeometryType
    {
        AxisAlignedBoundingBoxes = D3D12_RAYTRACING_GEOMETRY_TYPE.D3D12_RAYTRACING_GEOMETRY_TYPE_PROCEDURAL_PRIMITIVE_AABBS,
        Triangles = D3D12_RAYTRACING_GEOMETRY_TYPE.D3D12_RAYTRACING_GEOMETRY_TYPE_TRIANGLES
    }
}
