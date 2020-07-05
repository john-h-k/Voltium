namespace Voltium.Core.Memory.GpuResources
{
    /// <summary>
    /// The type of a GPU buffer. The names correspond to the names
    /// used in DirectX and HLSL
    /// </summary>
    public enum BufferKind
    {
        /// <summary>
        /// A constant buffer
        /// </summary>
        Constant,

        /// <summary>
        /// A vertex buffer
        /// </summary>
        Vertex,

        /// <summary>
        /// An index buffer
        /// </summary>
        Index
    }
}
