using TerraFX.Interop;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core
{
    public unsafe struct TriangleGeometryDesc
    {
        internal D3D12_RAYTRACING_GEOMETRY_TRIANGLES_DESC Desc;

        
        public ulong Transform3x4 { get => Desc.Transform3x4; set => Desc.Transform3x4 = value; }
        public IndexFormat IndexFormat { get => (IndexFormat)Desc.IndexFormat; set => Desc.IndexFormat = (DXGI_FORMAT)value; }
        public VertexFormat VertexFormat { get => (VertexFormat)Desc.VertexFormat; set => Desc.VertexFormat = (DXGI_FORMAT)value; }
        public uint IndexCount { get => Desc.IndexCount; set => Desc.IndexCount = value; }
        public uint VertexCount { get => Desc.VertexCount; set => Desc.VertexCount = value; }
        public Buffer IndexBuffer { set => Desc.IndexBuffer = value.GpuAddress; }

        public Buffer VertexBuffer {  set => Desc.VertexBuffer.StartAddress = value.GpuAddress; }
        public ulong VertexStride { get => Desc.VertexBuffer.StrideInBytes; set => Desc.VertexBuffer.StrideInBytes = value; }
    }
}
