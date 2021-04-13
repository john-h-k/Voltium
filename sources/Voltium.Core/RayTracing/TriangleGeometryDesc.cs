using TerraFX.Interop;
using Voltium.Common;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core
{
    public unsafe struct TriangleGeometryDesc
    {
        internal D3D12_RAYTRACING_GEOMETRY_TRIANGLES_DESC Desc;

        public static TriangleGeometryDesc FromTypes<TVertex, TIndex>(VertexFormat vertexFormat, in Buffer vertexBuffer, in Buffer indexBuffer)
            where TVertex : unmanaged
            where TIndex : unmanaged
        {
            IndexFormat format;
            if (typeof(TIndex) == typeof(uint))
            {
                format = IndexFormat.R32UInt;
            }
            else if (typeof(TIndex) == typeof(ushort))
            {
                format = IndexFormat.R16UInt;
            }
            else
            {
                ThrowHelper.ThrowArgumentException(nameof(TIndex));
                throw null;
            }

            return new TriangleGeometryDesc
            {
                VertexBuffer = vertexBuffer,
                VertexFormat = vertexFormat,
                VertexCount = (uint)(vertexBuffer.Length / (uint)sizeof(TVertex)),
                VertexStride = (uint)sizeof(TVertex),
                IndexCount = (uint)(indexBuffer.Length / (uint)sizeof(TIndex)),
                IndexFormat = format,
                IndexBuffer = indexBuffer
            };
        }

        public ulong Transform3x4 { get => Desc.Transform3x4; set => Desc.Transform3x4 = value; }

        public Buffer VertexBuffer {  set => Desc.VertexBuffer.StartAddress = value.GpuAddress; }
        public VertexFormat VertexFormat { get => (VertexFormat)Desc.VertexFormat; set => Desc.VertexFormat = (DXGI_FORMAT)value; }
        public uint VertexCount { get => Desc.VertexCount; set => Desc.VertexCount = value; }
        public ulong VertexStride { get => Desc.VertexBuffer.StrideInBytes; set => Desc.VertexBuffer.StrideInBytes = value; }

        public Buffer IndexBuffer { set => Desc.IndexBuffer = value.GpuAddress; }
        public IndexFormat IndexFormat { get => (IndexFormat)Desc.IndexFormat; set => Desc.IndexFormat = (DXGI_FORMAT)value; }
        public uint IndexCount { get => Desc.IndexCount; set => Desc.IndexCount = value; }

    }
}
