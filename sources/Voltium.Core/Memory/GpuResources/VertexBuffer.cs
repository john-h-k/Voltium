using System;
using System.Runtime.InteropServices;
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
        /// A <see cref="Span{T}"/> encompassing the vertex data
        /// </summary>
        public Span<TVertex> Vertices => MemoryMarshal.Cast<byte, TVertex>(Resource.CpuData);

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

    /// <summary>
    /// A type used for a scoped mapping of GPU resources
    /// </summary>
    public unsafe struct ScopedResourceMap : IDisposable
    {
        internal ScopedResourceMap(ID3D12Resource* resource, uint subresource)
        {
            _resource = resource;
            _subresource = subresource;
        }

        private ID3D12Resource* _resource;
        private uint _subresource;

        /// <summary>
        /// Unmaps the resource
        /// </summary>
        public void Dispose()
        {
            var ptr = _resource;
            if (ptr != null)
            {
                ptr->Unmap(_subresource, null);
                _resource = null;
            }
        }
    }
}
