using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Core.ShaderLang
{
    partial class Shader
    {
        protected class GroupSharedBuffer<T>
        {
        }

        [Intrinsic("ByteAddressBuffer")]
        protected class RawBuffer
        {
            public int Length => throw null!;
            public uint Load(int address) => throw null!;
            public Vector2<uint> Load2(int address) => throw null!;
            public Vector3<uint> Load3(int address) => throw null!;
            public Vector4<uint> Load4(int address) => throw null!;
            public T Load<T>(int address) => throw null!;


            public bool TryLoad(int address, out uint value) => throw null!;
            public bool TryLoad2(int address, out Vector2<uint> value) => throw null!;
            public bool TryLoad3(int address, out Vector3<uint> value) => throw null!;
            public bool TryLoad4(int address, out Vector4<uint> value) => throw null!;
            public bool TryLoad<T>(int address, out T value) => throw null!;
        }

        [Intrinsic("RWByteAddressBuffer")]
        protected class WritableRawBuffer
        {
            public int Length => throw null!;
            public uint Load(int address) => throw null!;
            public Vector2<uint> Load2(int address) => throw null!;
            public Vector3<uint> Load3(int address) => throw null!;
            public Vector4<uint> Load4(int address) => throw null!;
            public T Load<T>(int address) => throw null!;


            public void Store(int address, uint value) => throw null!;
            public void Store2(int address, Vector2<uint> value) => throw null!;
            public void Store3(int address, Vector3<uint> value) => throw null!;
            public void Store4(int address, Vector4<uint> value) => throw null!;
            public void Store<T>(int address, T value) => throw null!;


            public bool TryLoad(int address, out uint value) => throw null!;
            public bool TryLoad2(int address, out Vector2<uint> value) => throw null!;
            public bool TryLoad3(int address, out Vector3<uint> value) => throw null!;
            public bool TryLoad4(int address, out Vector4<uint> value) => throw null!;
            public bool TryLoad<T>(int address, out T value) => throw null!;
        }

        [Intrinsic("RWBuffer<T>")]
        protected class TypedBuffer<T>
        {
            public int Length => throw null!;
            public T this[int index] => throw null!;

            public bool TryLoad(int index, out T value) => throw null!;
        }


        [Intrinsic("Buffer<T>")]
        protected class WritableTypedBuffer<T>
        {
            public int Length => throw null!;
            public T this[int index] { get => throw null!; set => throw null!; }

            public bool TryLoad(int index, out T value) => throw null!;
        }

        [Intrinsic("StructuredBuffer<T>")]
        protected class StructuredBuffer<T>
        {
            public int Length => throw null!;
            public T this[int index] => throw null!;

            public bool TryLoad(int index, out T value) => throw null!;
        }

        [Intrinsic("RWStructuredBuffer<T>")]
        protected class WritableStructuredBuffer<T>
        {
            public uint IncrementCounter() => throw null!;
            public uint DecrementCounter() => throw null!;

            public int Length => throw null!;
            public T this[int index] { get => throw null!; set => throw null!; }

            public bool TryLoad(int index, out T value) => throw null!;
        }

        [Intrinsic("AppendStructuredBuffer<T>")]
        protected class AppendStructuredBuffer<T>
        {
            public int Length => throw null!;
            public int Stride => throw null!;

            public void Append(T value) => throw null!;
        }



        protected class ConsumeStructuredBuffer<T>
        {
            public int Length => throw null!;
            public int Stride => throw null!;

            public T Consume() => throw null!;
        }
        protected class ConstantBufferArray<T>
        {
            public int Length => throw null!;
            public T this[int index] => throw null!;
        }

        [AttributeUsage(AttributeTargets.Struct)]
        protected class ConstantBufferAttribute : ShaderAttribute
        { }

    }
}
