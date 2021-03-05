using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.CommandBuffer;
using Voltium.Core.Devices;
using Voltium.Core.Memory;

namespace Voltium.Core.Memory
{
    //public readonly partial struct GpuAddress : IEquatable<GpuAddress>, IComparable<GpuAddress>
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




    public unsafe struct Disposal<T>
    {
        public Disposal(object state, delegate*<object, ref T, void> ptr)
        {
            State = state;
            Hack.Value = 0;
            Hack.FnPtr = (delegate*<object, ref byte, void>)ptr;
        }

        private NotGenericHack Hack;
        private object State;

        public void Dispose(ref T val)
        {
            var dispose = (delegate*<object, ref T, void>)Interlocked.Exchange(ref Hack.Value, (nint)0);
            if (dispose != null)
            {
                dispose(State, ref val);
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct NotGenericHack
    {
        [FieldOffset(0)]
        public nint Value;
        [FieldOffset(0)]
        public delegate*<object, ref byte, void> FnPtr;
    }

    [GenerateEquality]
    public readonly partial struct IndirectCommandHandle : IHandle<IndirectCommandHandle>
    {
        private readonly GenerationalHandle Handle;

        public IndirectCommandHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public IndirectCommandHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    [GenerateEquality]
    public readonly partial struct DescriptorSetHandle : IHandle<DescriptorSetHandle>
    {
        private readonly GenerationalHandle Handle;

        public DescriptorSetHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public DescriptorSetHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    [GenerateEquality]
    public readonly partial struct ViewSetHandle : IHandle<ViewSetHandle>
    {
        private readonly GenerationalHandle Handle;

        public ViewSetHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public ViewSetHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    [GenerateEquality]
    public readonly partial struct ViewHandle : IHandle<ViewHandle>
    {
        private readonly GenerationalHandle Handle;

        public ViewHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public ViewHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    [GenerateEquality]
    public readonly partial struct RootSignatureHandle : IHandle<RootSignatureHandle>
    {
        private readonly GenerationalHandle Handle;

        public RootSignatureHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public RootSignatureHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    [GenerateEquality]
    public readonly partial struct PipelineHandle : IHandle<PipelineHandle>
    {
        private readonly GenerationalHandle Handle;

        public PipelineHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public PipelineHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }


    [GenerateEquality]
    public readonly partial struct RaytracingAccelerationStructureHandle : IHandle<RaytracingAccelerationStructureHandle>
    {
        private readonly GenerationalHandle Handle;

        public RaytracingAccelerationStructureHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public RaytracingAccelerationStructureHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    public unsafe struct RaytracingAccelerationStructure : IDisposable
    {
        internal RaytracingAccelerationStructure(ulong length, RaytracingAccelerationStructureHandle handle, Disposal<RaytracingAccelerationStructureHandle> disposal)
        {
            Length = length;
            Handle = handle;
            _dispose = disposal;
        }

        /// <summary>
        /// The size, in bytes, of the buffer
        /// </summary>
        public readonly ulong Length;

        public RaytracingAccelerationStructureHandle Handle;
        private Disposal<RaytracingAccelerationStructureHandle> _dispose;

        public void Dispose() => _dispose.Dispose(ref Handle);
    }

    [GenerateEquality]
    public readonly partial struct BufferHandle : IHandle<BufferHandle>
    {
        private readonly GenerationalHandle Handle;

        public BufferHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public BufferHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    [GenerateEquality]
    public readonly partial struct RenderPassHandle : IHandle<RenderPassHandle>
    {
        private readonly GenerationalHandle Handle;

        public RenderPassHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public RenderPassHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    [GenerateEquality]
    public readonly partial struct QuerySetHandle : IHandle<QuerySetHandle>
    {
        private readonly GenerationalHandle Handle;

        public QuerySetHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public QuerySetHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }



    [GenerateEquality]
    public readonly partial struct HeapHandle : IHandle<HeapHandle>
    {
        private readonly GenerationalHandle Handle;

        public HeapHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public HeapHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }



    [GenerateEquality]
    public readonly partial struct FenceHandle : IHandle<FenceHandle>
    {
        private readonly GenerationalHandle Handle;

        public FenceHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public FenceHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }


    /// <summary>
    /// Represents an untyped buffer of GPU data
    /// </summary>
    public unsafe struct Buffer : IDisposable
    {
        /// <summary>
        /// The size, in bytes, of the buffer
        /// </summary>
        public readonly ulong Length;

        public readonly ulong LengthAs<T>() => Length / (uint)Unsafe.SizeOf<T>();

        internal BufferHandle Handle;
        private Disposal<BufferHandle> _dispose;
        private void* _address;

        public void* Address => _address;
        public T* As<T>() where T : unmanaged => (T*)_address;
        public ref T AsRef<T>() where T : unmanaged => ref *(T*)_address;
        public Span<T> AsSpan<T>() where T : unmanaged => Address is null ? Span<T>.Empty : new(Address, checked((int)Length));

        public Buffer(ulong length, void* address, BufferHandle handle, Disposal<BufferHandle> dispose)
        {
            Length = length;
            Handle = handle;
            _address = address;
            _dispose = dispose;
        }

        public void SetName(string s) { }

        /// <inheritdoc/>
        public void Dispose() => _dispose.Dispose(ref Handle);
    }

}
