using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.GpuResources
{
    /// <summary>
    /// Represents a buffer of indices
    /// </summary>
    /// <typeparam name="TIndex">The type of each vertex</typeparam>
    public unsafe struct IndexBuffer<TIndex> where TIndex : unmanaged
    {
        internal IndexBuffer(GpuResource buffer)
        {
            Resource = buffer;
        }

        /// <summary>
        /// The resource which contains the vertices
        /// </summary>
        public readonly GpuResource Resource;

        /// <summary>
        /// The view of this index buffer
        /// </summary>
        public D3D12_INDEX_BUFFER_VIEW BufferView =>
            new D3D12_INDEX_BUFFER_VIEW
            {
                BufferLocation = Resource.GpuAddress,
                SizeInBytes = Resource.GetBufferSize(),
                Format = sizeof(TIndex) == 2 ? DXGI_FORMAT.DXGI_FORMAT_R16_UINT : DXGI_FORMAT.DXGI_FORMAT_R32_UINT
            };
    }
}
