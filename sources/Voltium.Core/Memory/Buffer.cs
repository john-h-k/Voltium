using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;
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
        private uint _offset;
        private void* _cpuAddress;
        private ulong _gpuAddress;

        /// <summary>
        /// Whether this buffer has been allocated or is uninitialized or disposed
        /// </summary>
        public bool IsAllocated => _resource != null;

        /// <summary>
        /// The size, in bytes, of the buffer
        /// </summary>
        public readonly uint Length;

        internal Buffer(ComputeDevice device, GpuResource resource, ulong offset, InternalAllocDesc* pDesc)
        {
            _resource = resource;
            _offset = (uint)offset;

            Length = (uint)pDesc->Desc.Width;

            _gpuAddress = _resource.GetResourcePointer()->GetGPUVirtualAddress() + _offset;

            // Don't map CPU-opaque buffers or raytracing acceleration buffers
            if (pDesc->HeapProperties.IsCPUAccessible && pDesc->InitialState != D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_RAYTRACING_ACCELERATION_STRUCTURE)
            {
                void* pData;
                device.ThrowIfFailed(_resource.GetResourcePointer()->Map(0, null, &pData));

                _cpuAddress = pData;
            }
            else
            {
                _cpuAddress = null;
            }
        }

        private void ThrowIfDead()
        {
            if (_resource is null)
            {
                ThrowHelper.ThrowObjectDisposedException(this.GetName());
            }
        }

        /// <summary>
        /// The buffer data. This may be empty if the data is not CPU writable
        /// </summary>
        public Span<T> AsSpan<T>() where T : struct => MemoryMarshal.Cast<byte, T>(Span);


        /// <summary>
        /// The buffer data. This may be empty if the data is not CPU writable
        /// </summary>
        public T* As<T>() where T : unmanaged => (T*)_cpuAddress;



        /// <summary>
        /// The buffer data. This may be empty if the data is not CPU writable
        /// </summary>
        public ref T AsRef<T>() where T : unmanaged => ref *As<T>();

        /// <summary>
        /// The buffer data. This may be <see langword="null"/> if the data is not CPU writable
        /// </summary>
        public void* Pointer => _cpuAddress;

        /// <summary>
        /// The GPU address
        /// </summary>
        public ulong GpuAddress => _gpuAddress;

        /// <summary>
        /// The buffer data. This may be empty if the data is not CPU writable
        /// </summary>
        public Span<byte> Span => new Span<byte>(_cpuAddress, _cpuAddress is null ? 0 : (int)Length);

        ///// <summary>
        ///// Maps the resource
        ///// </summary>
        //public void Map()
        //{
        //    if (_cpuAddress != null)
        //    {
        //        return;
        //    }
        //    _cpuAddress = (byte*)_resource.Map(0) + _offset;
        //}

        ///// <summary>
        ///// Unmaps the resource
        ///// </summary>
        //public void Unmap()
        //{
        //    if (_cpuAddress == null)
        //    {
        //        return;
        //    }
        //    _resource.Unmap(0);
        //    _cpuAddress = null;
        //}

        //internal struct ScopedMap : IDisposable
        //{
        //    internal ScopedMap(GpuResource resource) => _resource = resource;

        //    public void Dispose() => _resource.Unmap(0);

        //    private GpuResource _resource;
        //}

        //// internal because it allows use-after-free
        //internal ScopedMap MapScoped()
        //{
        //    Map();
        //    return new ScopedMap(_resource);
        //}

        /// <summary>
        /// Writes the <typeparamref name="T"/> to the buffer
        /// </summary>
        /// <typeparam name="T">The type to write</typeparam>
        public void WriteData<T>(ref T data, uint offset = 0, bool leaveMapped = false) where T : unmanaged
        {
            ((T*)_cpuAddress)[offset] = data;
        }

        /// <summary>
        /// Writes the <typeparamref name="T"/> to the buffer
        /// </summary>
        /// <typeparam name="T">The type to write</typeparam>
        public void WriteConstantBufferData<T>(ref T data, uint offset, bool leaveMapped = false) where T : unmanaged
        {
            var alignedSize = (sizeof(T) + 255) & ~255;

            *(T*)((byte*)_cpuAddress + (alignedSize * offset)) = data;
        }

        /// <summary>
        /// Writes the <typeparamref name="T"/> to the buffer
        /// </summary>
        /// <typeparam name="T">The type to write</typeparam>
        public void WriteDataByteOffset<T>(ref T data, uint offset, bool leaveMapped = false) where T : unmanaged
        {
            *(T*)((byte*)_cpuAddress + offset) = data;
        }

        /// <summary>
        /// Writes the <typeparamref name="T"/> to the buffer
        /// </summary>
        /// <typeparam name="T">The type to write</typeparam>
        public void WriteData<T>(ReadOnlySpan<T> data, uint offset = 0, bool leaveMapped = false) where T : unmanaged
        {
            data.CopyTo(new Span<T>((byte*)_cpuAddress + offset, (int)Length));
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Interlocked.Exchange(ref _resource, null!)?.Dispose();

            _cpuAddress = null;
            _gpuAddress = 0;
        }



        /// <inheritdoc/>
        public void Dispose(in GpuTask disposeAfter)
        {
            static void _Dispose(GpuResource resource) => resource.Dispose();

            disposeAfter.RegisterCallback(_resource, &_Dispose);

            _resource = null!;
            _cpuAddress = null;
            _gpuAddress = 0;
        }

        internal void* CpuAddress
        {
            get
            {
                ThrowIfDead();
                return _cpuAddress;
            }
        }

        internal GpuResource Resource => _resource;

        /// <summary>
        /// blah
        /// </summary>
        /// <returns></returns>
        internal ID3D12Resource* GetResourcePointer() => _resource.GetResourcePointer();

    }

    //public unsafe static class BufferExtensions
    //{
    //public static void CopyTo<T>(this Span<T> src, in Buffer<T> dest) where T : unmanaged
    //    => CopyTo((ReadOnlySpan<T>)src, dest);

    //public static void CopyTo<T>(this ReadOnlySpan<T> src, in Buffer<T> dest) where T : unmanaged
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
