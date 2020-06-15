using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.GpuResources;
using Voltium.Core.Memory.GpuResources.ResourceViews;

namespace Voltium.Core.Memory.GpuResources
{

    /// <summary>
    /// Represents a single-dimension untyped buffer of GPU data
    /// </summary>
    public unsafe struct Buffer
    {
        /// <summary>
        /// The type of this buffer to the shader
        /// </summary>
        public readonly BufferKind Kind;
        private GpuResource _resource;

        internal static uint CalculateBufferSize(BufferKind type, uint count)
            => CalcPaddedSize(type) * count;

        internal Buffer(GpuResource resource)
        {
            ByteCount = resource.GetBufferSize();
        }


        private static uint CalcPaddedSize(BufferKind type)
        {
            var size = type switch
            {
                // Constant buffers must be 256 byte aligned when being read by a shader
                // but the CPU types don't have to be 256 bytes
                BufferKind.Constant => (uint)(sizeof(T) + 255U) & ~255U,
                // The stride of index and vertex buffers is manually defined
                BufferKind.Index or BufferKind.Vertex => (uint)sizeof(T),

                // Can't use a throwhelper in a switch expression so we just use a magic value
                _ => 0xFFFFFFFF
            };

            if (size == 0xFFFFFFFF)
            {
                ThrowHelper.ThrowNotSupportedException("Unsupported buffer type");
            }

            return size;
        }

        /// <inheritdoc cref="GpuResource.Map"/>
        public Span<T> MapAs<T>()
        {
            // buffers always have subresource index 0
            _resource.Map(0);

            // consumer is expectred to klnow the alignemtn rules e,g Cb|V rquiers 256 byte
            return MemoryMarshal.Cast<byte, T>(_resource.CpuData);
        }


        /// <inheritdoc cref="GpuResource.MapScoped"/>
        public ScopedResourceMap<T> MapScoped<T>() => _resource.MapScoped<T>(0);


        /// <inheritdoc cref="GpuResource.Unmap"/>
        public void Unmap() => _resource.Map(0);

        /// <summary>
        /// The size, in bytes, of the buffer
        /// </summary>
        public readonly uint ByteCount;

        // required for binding directly without desc heap
        internal ulong GetGpuAddress() => _resource.GpuAddress;

        internal GpuResource Resource => _resource;

        internal Span<byte> GetUnderlyingDataSpan() => _resource.CpuData;
    }

    //public static unsafe class BufferExtensions
    //{
        //public static void CopyTo<T>(this Span<T> src, Buffer<T> dest) where T : unmanaged
        //    => CopyTo((ReadOnlySpan<T>)src, dest);

        //public static void CopyTo<T>(this ReadOnlySpan<T> src, Buffer<T> dest) where T : unmanaged
        //{
        //    if (dest.Kind != BufferKind.Constant && dest.GetElementSize() == sizeof(T))
        //    {
        //        src.CopyTo(MemoryMarshal.Cast<byte, T>(dest.GetUnderlyingDataSpan()));
        //    }
        //    else
        //    {
        //        for (var i = 0; i < src.Length; i++)
        //        {
        //            dest[(uint)i] = src[i];
        //        }
        //    }
        //}

        //public static void CopyTo<T>(this Buffer<T> src, Span<T> dest) where T : unmanaged
        //{
        //    if (src.Kind != BufferKind.Constant && src.GetElementSize() == sizeof(T))
        //    {
        //        src.GetUnderlyingDataSpan().CopyTo(MemoryMarshal.Cast<T, byte>(dest));
        //    }
        //    else
        //    {
        //        for (var i = 0; i < src.Count; i++)
        //        {
        //            src[(uint)i] = dest[i];
        //        }
        //    }
        //}
    //}
}
