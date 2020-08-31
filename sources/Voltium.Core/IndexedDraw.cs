using TerraFX.Interop;

namespace Voltium.Core
{
    /// <summary>
    /// Represents the parameters used for a call to <see cref="ID3D12GraphicsCommandList.DrawIndexedInstanced" />
    /// </summary>
    public readonly struct IndexedDraw
    {
        /// <summary>
        /// Number of indices read from the index buffer for each instance
        /// </summary>
        public readonly uint IndexCountPerInstance;

        /// <summary>
        /// Number of instances to draw
        /// </summary>
        public readonly uint InstanceCount;

        /// <summary>
        /// The location of the first index read by the GPU from the index buffer
        /// </summary>
        public readonly uint StartIndexLocation;

        /// <summary>
        /// A value added to each index before reading a vertex from the vertex buffer
        /// </summary>
        public readonly int BaseVertexLocation;

        /// <summary>
        /// A value added to each index before reading per-instance data from a vertex buffer
        /// </summary>
        public readonly uint StartInstanceLocation;

        /// <summary>
        /// Creates a new instance of <see cref="IndexedDraw"/>
        /// </summary>
        public IndexedDraw(
            uint indexCountPerInstance,
            uint instanceCount,
            uint startIndexLocation,
            int baseVertexLocation,
            uint startInstanceLocation
        )
        {
            IndexCountPerInstance = indexCountPerInstance;
            InstanceCount = instanceCount;
            StartIndexLocation = startIndexLocation;
            BaseVertexLocation = baseVertexLocation;
            StartInstanceLocation = startInstanceLocation;
        }
    }
}
