using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Core.GpuResources;

namespace Voltium.Core.Memory.GpuResources.ResourceViews
{
    /// <summary>
    /// Represents a buffer of constant buffers
    /// </summary>
    /// <typeparam name="TBuffer">The type of each constant buffer</typeparam>
    public unsafe struct StructuredBuffer<TBuffer> where TBuffer : unmanaged
    {
        internal StructuredBuffer(GpuResource buffer)
        {
            Resource = buffer;
            LocalCopy = default;
        }

        /// <summary>
        /// A local copy of the buffer for modification
        /// </summary>
        public TBuffer LocalCopy;

        /// <summary>
        /// The size of a constant buffer in GPU memory. This is the same as the size of <typeparamref name="TBuffer"/>
        /// </summary>
        public static int ElementSize => sizeof(TBuffer);

        /// <summary>
        /// The resource which contains the vertices
        /// </summary>
        public readonly GpuResource Resource;

        /// <summary>
        /// A <see cref="Span{T}"/> encompassing the index data
        /// </summary>
        public Span<TBuffer> Buffers => MemoryMarshal.Cast<byte, TBuffer>(Resource.CpuData);

        /// <summary>
        /// Retuns a <see cref="ScopedResourceMap"/> that allows a <see cref="Map"/> call to be scoped
        /// </summary>
        public ScopedResourceMap MapScoped() => Resource.MapScoped(0);

        /// <summary>
        /// If the resource is not currently mapped, maps the resource
        /// </summary>
        public void Map() => Resource.Map(0);


        /// <summary>
        /// If the resource is currently mapped, unmaps the resource
        /// </summary>
        public void Unmap() => Resource.Unmap(0);
    }
}
