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




    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct Disposal<T>
    {
        public Disposal(delegate*<ref T, void> ptr)
        {
            Value = 0;
            FnPtr = ptr;
        }

        [FieldOffset(0)]
        public nint Value;
        [FieldOffset(0)]
        public delegate*<ref T, void> FnPtr;

        public void Dispose(ref T val)
        {
            var dispose = (delegate*<ref T, void>)Interlocked.Exchange(ref Value, (nint)0);
            if (dispose != null)
            {
                dispose(ref val);
            }
        }
    }


    public readonly struct IndirectCommandHandle : IHandle<IndirectCommandHandle>
    {
        private readonly GenerationalHandle Handle;

        public IndirectCommandHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public IndirectCommandHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    public readonly struct DescriptorSetHandle : IHandle<DescriptorSetHandle>
    {
        private readonly GenerationalHandle Handle;

        public DescriptorSetHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public DescriptorSetHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    public readonly struct ViewSetHandle : IHandle<ViewSetHandle>
    {
        private readonly GenerationalHandle Handle;

        public ViewSetHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public ViewSetHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    public readonly struct ViewHandle : IHandle<ViewHandle>
    {
        private readonly GenerationalHandle Handle;

        public ViewHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public ViewHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    public readonly struct RootSignatureHandle : IHandle<RootSignatureHandle>
    {
        private readonly GenerationalHandle Handle;

        public RootSignatureHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public RootSignatureHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }
    
    public readonly struct PipelineHandle : IHandle<PipelineHandle>
    {
        private readonly GenerationalHandle Handle;

        public PipelineHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public PipelineHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }


    public readonly struct RaytracingAccelerationStructureHandle : IHandle<RaytracingAccelerationStructureHandle>
    {
        private readonly GenerationalHandle Handle;

        public RaytracingAccelerationStructureHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public RaytracingAccelerationStructureHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    public unsafe struct RaytracingAccelerationStructure : IDisposable
    {
        /// <summary>
        /// The size, in bytes, of the buffer
        /// </summary>
        public readonly uint Length;

        public RaytracingAccelerationStructureHandle Handle;
        private Disposal<RaytracingAccelerationStructureHandle> _dispose;

        public void Dispose() => _dispose.Dispose(ref Handle);
    }

    public readonly struct BufferHandle : IHandle<BufferHandle>
    {
        private readonly GenerationalHandle Handle;

        public BufferHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public BufferHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    public readonly struct RenderPassHandle : IHandle<RenderPassHandle>
    {
        private readonly GenerationalHandle Handle;

        public RenderPassHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public RenderPassHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }

    public readonly struct QuerySetHandle : IHandle<QuerySetHandle>
    {
        private readonly GenerationalHandle Handle;

        public QuerySetHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public QuerySetHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }



    public readonly struct HeapHandle : IHandle<HeapHandle>
    {
        private readonly GenerationalHandle Handle;

        public HeapHandle(GenerationalHandle handle) => Handle = handle;

        public GenerationalHandle Generational => Handle;
        public HeapHandle FromGenerationHandle(GenerationalHandle handle) => new(handle);
    }


    /// <summary>
    /// Represents an untyped buffer of GPU data
    /// </summary>
    public unsafe struct Buffer : IDisposable
    {
        /// <summary>
        /// The size, in bytes, of the buffer
        /// </summary>
        public readonly uint Length;

        internal BufferHandle Handle;
        private Disposal<BufferHandle> _dispose;


        public void SetName(string s) { }

        /// <inheritdoc/>
        public void Dispose() => _dispose.Dispose(ref Handle);
    }

}
