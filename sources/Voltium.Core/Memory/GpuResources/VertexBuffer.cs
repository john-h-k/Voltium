using TerraFX.Interop;
using static TerraFX.Interop.D3D12_HEAP_FLAGS;

namespace Voltium.Core.GpuResources
{
    /// <summary>
    /// Represents a buffer of vertices
    /// </summary>
    /// <typeparam name="TVertex">The type of each vertex</typeparam>
    public unsafe struct VertexBuffer<TVertex> where TVertex : unmanaged
    {
        internal VertexBuffer(GpuResource buffer)
        {
            Resource = buffer;
        }

        /// <summary>
        /// The resource which contains the vertices
        /// </summary>
        public readonly GpuResource Resource;

        /// <summary>
        /// The view of this vertex buffer
        /// </summary>
        public D3D12_VERTEX_BUFFER_VIEW BufferView =>
            new D3D12_VERTEX_BUFFER_VIEW
            {
                BufferLocation = Resource.GpuAddress,
                SizeInBytes = Resource.GetBufferSize(),
                StrideInBytes = (uint)sizeof(TVertex)
            };
    }
}
