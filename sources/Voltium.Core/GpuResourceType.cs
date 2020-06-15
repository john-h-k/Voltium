using TerraFX.Interop;

namespace Voltium.Core
{
    /// <summary>
    /// Represents the type of a GPU resource
    /// </summary>
    public enum GpuMemoryKind
    {
        /// <summary>
        /// A resource that is only accessible by the GPU.
        /// Otherwise known as a default buffer
        /// </summary>
        GpuOnly = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_DEFAULT,

        /// <summary>
        /// A resource optimized for writing to the GPU by the CPU.
        /// Otherwise known as an upload buffer
        /// </summary>
        CpuUpload = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_UPLOAD,

        /// <summary>
        /// A resource optimized for reading from the GPU by the CPU.
        /// Otherwise known as a readback buffer
        /// </summary>
        CpuReadback = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_READBACK
    }
}
