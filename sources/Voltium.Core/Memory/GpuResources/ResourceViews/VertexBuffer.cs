using System;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using static TerraFX.Interop.D3D12_HEAP_FLAGS;

namespace Voltium.Core.GpuResources
{
    ///// <summary>
    ///// Represents a buffer of vertices
    ///// </summary>
    ///// <typeparam name="TVertex">The type of each vertex</typeparam>
    //public unsafe struct VertexBuffer<TVertex> where TVertex : unmanaged
    //{
    //    internal VertexBuffer(GpuResource buffer)
    //    {
    //        Resource = buffer;
    //    }

    //    /// <summary>
    //    /// The resource which contains the vertices
    //    /// </summary>
    //    public readonly GpuResource Resource;

    //    /// <summary>
    //    /// A <see cref="Span{T}"/> encompassing the vertex data
    //    /// </summary>
    //    public Span<TVertex> Vertices => MemoryMarshal.Cast<byte, TVertex>(Resource.CpuData);

    //    /// <summary>
    //    /// Retuns a <see cref="ScopedResourceMap"/> that allows a <see cref="Map"/> call to be scoped
    //    /// </summary>
    //    public ScopedResourceMap MapScoped() => Resource.MapScoped(0);

    //    /// <summary>
    //    /// If the resource is not currently mapped, maps the resource
    //    /// </summary>
    //    public void Map() => Resource.Map(0);

    //    /// <summary>
    //    /// If the resource is currently mapped, unmaps the resource
    //    /// </summary>
    //    public void Unmap() => Resource.Unmap(0);

    //    /// <summary>
    //    /// The view of this vertex buffer
    //    /// </summary>
    //    public D3D12_VERTEX_BUFFER_VIEW BufferView =>
    //        new D3D12_VERTEX_BUFFER_VIEW
    //        {
    //            BufferLocation = Resource.GpuAddress,
    //            SizeInBytes = Resource.GetBufferSize(),
    //            StrideInBytes = (uint)sizeof(TVertex)
    //        };
    //}

    /// <summary>
    /// A type used for a scoped mapping of GPU resources
    /// </summary>
    public unsafe struct ScopedResourceMap<T> : IDisposable
    {
        internal ScopedResourceMap(ID3D12Resource* resource, uint subresource, void* data, uint length)
        {
            _resource = resource;
            _subresource = subresource;
            _data = data;
            _length = length;
        }

        private ID3D12Resource* _resource;
        private uint _subresource;
        private void* _data;
        private uint _length;

        /// <summary>
        /// The data
        /// </summary>
        public Span<T> Data => new Span<T>(_data, (int)_length);

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
