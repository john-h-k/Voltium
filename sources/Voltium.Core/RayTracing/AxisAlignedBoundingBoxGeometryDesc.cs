using TerraFX.Interop.DirectX;

namespace Voltium.Core
{
    public unsafe struct AxisAlignedBoundingBoxGeometryDesc
    {
        internal D3D12_RAYTRACING_GEOMETRY_DESC Desc;

        public ulong AxisAlignedBoundingBoxes { get => Desc.AABBs.AABBs.StartAddress;  set => Desc.AABBs.AABBs.StartAddress = value; }
        public ulong Stride { get => Desc.AABBs.AABBs.StrideInBytes; set => Desc.AABBs.AABBs.StrideInBytes = value; }

        public ulong Count { get => Desc.AABBs.AABBCount; set => Desc.AABBs.AABBCount = value; }
    }
}
