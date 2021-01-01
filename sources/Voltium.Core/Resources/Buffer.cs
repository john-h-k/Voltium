using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;
using Voltium.Core.Memory;

namespace Voltium.Core.Memory
{
    //public readonly struct GpuAddress : IEquatable<GpuAddress>, IComparable<GpuAddress>
    //{
    //    internal readonly ulong Value;

    //    internal GpuAddress(ulong value) => Value = value;

    //    public static bool operator ==(GpuAddress left, GpuAddress right) => left.Value == right.Value;
    //    public static bool operator !=(GpuAddress left, GpuAddress right) => left.Value != right.Value;

    //    public static bool operator >(GpuAddress left, GpuAddress right) => left.Value > right.Value;
    //    public static bool operator <(GpuAddress left, GpuAddress right) => left.Value < right.Value;

    //    public static bool operator >=(GpuAddress left, GpuAddress right) => left.Value >= right.Value;
    //    public static bool operator <=(GpuAddress left, GpuAddress right) => left.Value <= right.Value;



    //    public static GpuAddress operator +(GpuAddress left, ulong right) => new(left.Value + right);
    //    public static GpuAddress operator +(GpuAddress left, long right) => left + (ulong)right;
    //    public static GpuAddress operator +(ulong left, GpuAddress right) => new(left + right.Value);
    //    public static GpuAddress operator +(long left, GpuAddress right) => (ulong)left + right;



    //    public static GpuAddress operator -(GpuAddress left, ulong right) => new(left.Value - right);
    //    public static GpuAddress operator -(GpuAddress left, long right) => left - (ulong)right;
    //    public static GpuAddress operator -(ulong left, GpuAddress right) => new(left - right.Value);
    //    public static GpuAddress operator -(long left, GpuAddress right) => (ulong)left - right;



    //    public static GpuAddress operator *(GpuAddress left, ulong right) => new(left.Value * right);
    //    public static GpuAddress operator *(GpuAddress left, long right) => left * (ulong)right;
    //    public static GpuAddress operator *(ulong left, GpuAddress right) => new(left * right.Value);
    //    public static GpuAddress operator *(long left, GpuAddress right) => (ulong)left * right;



    //    public static GpuAddress operator /(GpuAddress left, ulong right) => new(left.Value / right);
    //    public static GpuAddress operator /(GpuAddress left, long right) => left / (ulong)right;
    //    public static GpuAddress operator /(ulong left, GpuAddress right) => new(left / right.Value);
    //    public static GpuAddress operator /(long left, GpuAddress right) => (ulong)left / right;


    //    public static GpuAddress operator &(GpuAddress left, ulong right) => new(left.Value & right);
    //    public static GpuAddress operator &(GpuAddress left, long right) => left & (ulong)right;
    //    public static GpuAddress operator &(ulong left, GpuAddress right) => new(left & right.Value);
    //    public static GpuAddress operator &(long left, GpuAddress right) => (ulong)left & right;


    //    public static GpuAddress operator |(GpuAddress left, ulong right) => new(left.Value | right);
    //    public static GpuAddress operator |(GpuAddress left, long right) => left | (ulong)right;
    //    public static GpuAddress operator |(ulong left, GpuAddress right) => new(left | right.Value);
    //    public static GpuAddress operator |(long left, GpuAddress right) => (ulong)left | right;


    //    public static GpuAddress operator ^(GpuAddress left, ulong right) => new(left.Value ^ right);
    //    public static GpuAddress operator ^(GpuAddress left, long right) => left ^ (ulong)right;
    //    public static GpuAddress operator ^(ulong left, GpuAddress right) => new(left ^ right.Value);
    //    public static GpuAddress operator ^(long left, GpuAddress right) => (ulong)left ^ right;

    //    public static GpuAddress operator >>(GpuAddress left, int right) => new(left.Value >> right);
    //    public static GpuAddress operator <<(GpuAddress left, int right) => new(left.Value << right);


    //    public static GpuAddress operator ~(GpuAddress value) => new(~value.Value);

    //    public override bool Equals(object? obj) => obj is GpuAddress other && Equals(other);
    //    public bool Equals(GpuAddress other) => this == other;

    //    public int CompareTo(GpuAddress other) => Value.CompareTo(other.Value);

    //    public override int GetHashCode() => Value.GetHashCode();
    //    public override string ToString() => $"0x{Value:X16}";
    //}

    //public unsafe struct BufferView
    //{

    //}

    public unsafe struct RaytracingAccelerationStructure : IInternalD3D12Object, IDisposable
    {
        internal RaytracingAccelerationStructure(in Buffer buffer)
        {
            Buffer = buffer;
        }

        internal Buffer Buffer;

        public uint Length => Buffer.Length;
        public ulong GpuAddress => Buffer.GpuAddress;

        ID3D12Object* IInternalD3D12Object.GetPointer() => (ID3D12Object*)Buffer.GetResourcePointer();
        public void Dispose() => Buffer.Dispose();
    }

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

        internal uint Offset => _offset;

        public readonly uint LengthAs<T>() => Length / (uint)Unsafe.SizeOf<T>();

        internal Buffer(ComputeDevice device, GpuResource resource, ulong offset, in InternalAllocDesc pDesc)
        {
            _resource = resource;
            _offset = (uint)offset;

            Length = (uint)pDesc.Desc.Width;

            _gpuAddress = _resource.GetResourcePointer()->GetGPUVirtualAddress() + _offset;

            // Don't map CPU-opaque buffers or raytracing acceleration buffers
            if (pDesc.HeapProperties.IsCPUAccessible && pDesc.InitialState != D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_RAYTRACING_ACCELERATION_STRUCTURE)
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

        private readonly void ThrowIfDead()
        {
            if (_resource is null)
            {
                ThrowHelper.ThrowObjectDisposedException(this.GetName());
            }
        }

        /// <summary>
        /// The buffer data. This may be empty if the data is not CPU writable
        /// </summary>
        public readonly Span<T> AsSpan<T>() where T : unmanaged => new(Pointer, (int)LengthAs<T>());

        /// <summary>
        /// The buffer data. This may be empty if the data is not CPU writable
        /// </summary>
        public T* As<T>() where T : unmanaged => (T*)Pointer;

        /// <summary>
        /// The buffer data. This may be empty if the data is not CPU writable
        /// </summary>
        public ref T AsRef<T>() where T : unmanaged => ref *As<T>();

        /// <summary>
        /// The buffer data. This may be <see langword="null"/> if the data is not CPU writable
        /// </summary>
        public readonly void* Pointer
        {
            get
            {
                ThrowIfDead();
                return _cpuAddress;
            }
        }

        /// <summary>
        /// The GPU address
        /// </summary>
        public ulong GpuAddress => _gpuAddress;

        /// <summary>
        /// The buffer data. This may be empty if the data is not CPU writable
        /// </summary>
        public Span<byte> Span => new Span<byte>(_cpuAddress, _cpuAddress is null ? 0 : (int)Length);

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
            static void _Dispose(GpuResource? resource) => resource?.Dispose();

            disposeAfter.RegisterCallback(Interlocked.Exchange(ref _resource, null!), &_Dispose);

            _resource = null!;
            _cpuAddress = null;
            _gpuAddress = 0;
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
