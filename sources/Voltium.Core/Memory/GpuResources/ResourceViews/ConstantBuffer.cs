using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.GpuResources;

namespace Voltium.Core.Memory.GpuResources.ResourceViews
{

    /// <summary>
    /// Represents a buffer of constant buffers
    /// </summary>
    /// <typeparam name="TBuffer">The type of each constant buffer</typeparam>
    public unsafe struct ConstantBuffer<TBuffer> where TBuffer : unmanaged
    {
        internal ConstantBuffer(GpuResource buffer)
        {
            Resource = buffer;
            LocalCopy = default;
        }

        /// <summary>
        /// A local copy of the buffer for modification
        /// </summary>
        public TBuffer LocalCopy;

        /// <summary>
        /// The size of a constant buffer in GPU memory. This is the same as the size of <typeparamref name="TBuffer"/>,
        /// rounded up to the nearest 256
        /// </summary>
        public static int ElementSize => (sizeof(TBuffer) + 255) & ~255;

        /// <summary>
        /// The resource which contains the vertices
        /// </summary>
        public readonly GpuResource Resource;

        /// <summary>
        /// A <see cref="Span{T}"/> encompassing the index data
        /// </summary>
        public ConstantBufferSpan<TBuffer> Buffers => new (Resource.CpuData);

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

    /// <summary>
    /// A <see cref="Span{T}"/>-like type used for a window into a <see cref="ConstantBuffer{TBuffer}"/>
    /// </summary>
    /// <typeparam name="TBuffer"></typeparam>
    public unsafe readonly ref struct ConstantBufferSpan<TBuffer> where TBuffer : unmanaged
    {
        /// <summary>
        /// The size of a constant buffer in GPU memory. This is the same as the size of <typeparamref name="TBuffer"/>,
        /// rounded up to the nearest 256
        /// </summary>
        public static int ElementSize => (sizeof(TBuffer) + 255) & ~255;

        private readonly Span<byte> _untypedData;

        internal Span<byte> GetUntypedData() => _untypedData;

        /// <summary>
        /// Create a new <see cref="ConstantBufferSpan{TBuffer}"/> from a <see cref="Span{T}"/>
        /// </summary>
        /// <param name="data">The constant buffer data</param>
        public ConstantBufferSpan(Span<byte> data)
        {
            if (data.Length % ElementSize != 0)
            {
                ThrowHelper.ThrowArgumentException("Invalid data size");
            }
            _untypedData = data;
        }

        /// <inheritdoc cref="Span{T}.this" />
        public ref TBuffer this[int index]
            => ref Unsafe.As<byte, TBuffer>(ref _untypedData[index * ElementSize]);
    }
}
