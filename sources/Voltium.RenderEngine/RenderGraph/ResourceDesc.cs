using System;
using System.Runtime.InteropServices;
using Voltium.Core;
using Voltium.Core.CommandBuffer;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.RenderEngine
{
    internal struct ResourceDesc
    {
        public ResourceType Type;

        public BufferDesc BufferDesc;
        public TextureDesc TextureDesc;
        public ulong AccelerationStructureDesc;

        public MemoryAccess MemoryAccess;
        public ResourceState InitialState;

        public Buffer Buffer;
        public Texture Texture;
        public RaytracingAccelerationStructure RaytracingAccelerationStructure;

        public string? DebugName;
    }

    internal struct ViewDesc
    {
        public ResourceType Type;
        public ResourceHandle Handle;

        public BufferViewDesc? BufferViewDesc;
        public TextureViewDesc? TextureViewDesc;

        public View View;

        public string? DebugName;
    }

    //[StructLayout(LayoutKind.Explicit)]
    //public struct Multiplier
    //{
    //    enum Type { Double, Rational };

    //    [FieldOffset(0)]
    //    private Type MultiplierType;

    //    [FieldOffset(sizeof(Type))]
    //    public double Double;
    //    [FieldOffset(sizeof(Type))]
    //    public Rational Rational;

    //    public ushort Apply(ushort value) => MultiplierType == Type.Double ? (ushort)(Double * value) : (ushort)(Rational.Numerator / (value * Rational.Denominator));
    //    public uint Apply(uint value) => MultiplierType == Type.Double ? (uint)(Double * value) : Rational.Numerator / (value * Rational.Denominator);
    //    public ulong Apply(ulong value) => MultiplierType == Type.Double ? (uint)(Double * value) : Rational.Numerator / (value * Rational.Denominator);
    //}

    //public struct Rational
    //{
    //    public uint Numerator, Denominator;
    //}
}
