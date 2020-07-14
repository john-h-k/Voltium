using System;
using System.Diagnostics;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Memory;

namespace Voltium.Core.Memory
{

    /// <summary>
    /// Represents a single-dimension untyped buffer of GPU data
    /// </summary>
    public unsafe struct Buffer : IInternalD3D12Object, IDisposable
    {
        ID3D12Object* IInternalD3D12Object.GetPointer() => _resource.GetPointer();

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
                    _cpuAddress = _resource is null ? null : _resource.Map(0);
                }

                return new Span<byte>(_cpuAddress, (int)Length);
            }
        }

        /// <summary>
        /// Writes the <typeparamref name="T"/> to the buffer
        /// </summary>
        /// <typeparam name="T">The type to write</typeparam>
        public void WriteData<T>(ref T data, uint offset, bool leaveMapped = false) where T : unmanaged
        {
            if (_cpuAddress is null)
            {
                _cpuAddress = _resource.Map(0);
            }

            ((T*)_cpuAddress)[offset] = data;

            if (!leaveMapped)
            {
                _resource.Unmap(0);
                _cpuAddress = null;
            }
        }

        /// <summary>
        /// Writes the <typeparamref name="T"/> to the buffer
        /// </summary>
        /// <typeparam name="T">The type to write</typeparam>
        public void WriteConstantBufferData<T>(ref T data, uint offset, bool leaveMapped = false) where T : unmanaged
        {
            if (_cpuAddress is null)
            {
                _cpuAddress = _resource.Map(0);
            }

            var alignedSize = (sizeof(T) + 255) & ~255;

            *(T*)((byte*)_cpuAddress + (alignedSize * offset)) = data;

            if (!leaveMapped)
            {
                _resource.Unmap(0);
                _cpuAddress = null;
            }
        }

        /// <summary>
        /// Writes the <typeparamref name="T"/> to the buffer
        /// </summary>
        /// <typeparam name="T">The type to write</typeparam>
        public void WriteDataByteOffset<T>(ref T data, uint offset, bool leaveMapped = false) where T : unmanaged
        {
            if (_cpuAddress is null)
            {
                _cpuAddress = _resource.Map(0);
            }

            *(T*)((byte*)_cpuAddress + offset) = data;

            if (!leaveMapped)
            {
                _resource.Unmap(0);
                _cpuAddress = null;
            }
        }

        /// <summary>
        /// Writes the <typeparamref name="T"/> to the buffer
        /// </summary>
        /// <typeparam name="T">The type to write</typeparam>
        public void WriteData<T>(ReadOnlySpan<T> data, bool leaveMapped = false) where T : unmanaged
        {
            if (_cpuAddress is null)
            {
                _cpuAddress = _resource.Map(0);
            }

            data.CopyTo(new Span<T>(_cpuAddress, (int)Length));

            if (!leaveMapped)
            {
                _resource.Unmap(0);
                _cpuAddress = null;
            }
        }

        /// <inheritdoc/>
        public void Dispose() => _resource?.Dispose();

        // required for binding directly without desc heap
        internal ulong GpuAddress;

        internal GpuResource Resource => _resource;

        internal ID3D12Resource* GetResourcePointer() => _resource.UnderlyingResource;

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
