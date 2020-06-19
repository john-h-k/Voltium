using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public unsafe struct Buffer : IDisposable
    {
        private GpuResource _resource;
        private void* _cpuAddress;
        /// <summary>
        /// The size, in bytes, of the buffer
        /// </summary>
        public readonly uint Length;

        internal Buffer(ulong length, GpuResource resource)
        {
            _resource = resource;

            Length = (uint)length;
            _cpuAddress = null;
            GpuAddress = resource.GpuAddress;
        }

        /// <summary>
        /// The buffer data. This may be empty if the data is not CPU writable
        /// </summary>
        public Span<byte> Data
        {
            get
            {
                if (_cpuAddress == null)
                {
                    _cpuAddress = _resource.Map(0);
                }

                return new Span<byte>(_cpuAddress, (int)Length);
            }
        }

        /// <summary>
        /// Writes the <typeparamref name="T"/> to the buffer
        /// </summary>
        /// <typeparam name="T">The type to write</typeparam>
        public void WriteData<T>(ref T data, uint offset) where T : unmanaged
        {
            if (_cpuAddress == null)
            {
                _cpuAddress = _resource.Map(0);
            }

            *((T*)_cpuAddress + offset) = data;
        }

        /// <summary>
        /// Writes the <typeparamref name="T"/> to the buffer
        /// </summary>
        /// <typeparam name="T">The type to write</typeparam>
        public void WriteData<T>(Span<T> data) where T : unmanaged
        {
            if (_cpuAddress == null)
            {
                _cpuAddress = _resource.Map(0);
            }

            data.CopyTo(new Span<T>(_cpuAddress, (int)Length));
        }

        /// <inheritdoc/>
        public void Dispose() => _resource?.Dispose();

        // required for binding directly without desc heap
        internal ulong GpuAddress;

        internal GpuResource Resource => _resource;
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
