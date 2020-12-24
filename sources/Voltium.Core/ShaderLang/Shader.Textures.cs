using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Core.ShaderLang
{
    public abstract partial class Shader
    {

        [Intrinsic("SamplerState")]
        protected class Sampler
        {

        }

        [Intrinsic]
        protected class WritableTexture1D<T>
        {
            public uint Width => throw null!;

            public T this[uint x] { get => throw null!; set => throw null!; }
        }

        [Intrinsic]
        protected class WritableTexture1DArray<T>
        {
            public uint Width => throw null!;
            public uint ArrayLength => throw null!;

            public T this[uint x, uint arrayIndex] { get => throw null!; set => throw null!; }
        }

        [Intrinsic]
        protected class WritableTexture2D<T>
        {
            public uint Width => throw null!;
            public uint Height => throw null!;

            public T this[uint x, uint y] { get => throw null!; set => throw null!; }
            public T this[Vector2<uint> xy] { get => throw null!; set => throw null!; }
        }

        [Intrinsic]
        protected class WritableTexture2DArray<T>
        {
            public uint Width => throw null!;
            public uint Height => throw null!;
            public uint ArrayLength => throw null!;

            public T this[uint x, uint y, uint arrayIndex] { get => throw null!; set => throw null!; }
            public T this[Vector2<uint> xy, uint arrayIndex] { get => throw null!; set => throw null!; }
            public T this[Vector3<uint> xyArrayIndex] { get => throw null!; set => throw null!; }
        }

        [Intrinsic]
        protected class WritableTexture3D<T>
        {
            public uint Width => throw null!;
            public uint Height => throw null!;
            public uint Depth => throw null!;

            public T this[uint x, uint y, uint z] { get => throw null!; set => throw null!; }
            public T this[Vector3<uint> xyz] { get => throw null!; set => throw null!; }
        }


        [Intrinsic]
        protected class Texture2D<T>
        {
            public T this[uint x, uint y, uint mip = 0] => throw null!;
            public T this[Vector2<uint> xy, uint mip = 0] => throw null!;
            public T this[Vector2<uint> xyMip] => throw null!;

            public T Sample(Sampler sampler, float x, float y, Vector2<uint> offset = default) => throw null!;
            public T Sample(Sampler sampler, Vector2<float> xy, Vector2<uint> offset = default) => throw null!;


            public T Sample(Sampler sampler, float x, float y, uint offsetX = default, uint offsetY = 0) => throw null!;
            public T Sample(Sampler sampler, Vector2<float> xy, uint offsetX = default, uint offsetY = 0) => throw null!;


            public Vector4<T> GatherRed(Sampler sampler, float x, float y, Vector2<uint> offset = default) => throw null!;
            public Vector4<T> GatherRed(Sampler sampler, Vector2<float> xy, Vector2<uint> offset = default) => throw null!;
            public Vector4<T> GatherRed(Sampler sampler, float x, float y, uint offsetX = default, uint offsetY = 0) => throw null!;
            public Vector4<T> GatherRed(Sampler sampler, Vector2<float> xy, uint offsetX = default, uint offsetY = 0) => throw null!;

            public Vector4<T> GatherGreen(Sampler sampler, float x, float y, Vector2<uint> offset = default) => throw null!;
            public Vector4<T> GatherGreen(Sampler sampler, Vector2<float> xy, Vector2<uint> offset = default) => throw null!;
            public Vector4<T> GatherGreen(Sampler sampler, float x, float y, uint offsetX = default, uint offsetY = 0) => throw null!;
            public Vector4<T> GatherGreen(Sampler sampler, Vector2<float> xy, uint offsetX = default, uint offsetY = 0) => throw null!;

            public Vector4<T> GatherBlue(Sampler sampler, float x, float y, Vector2<uint> offset = default) => throw null!;
            public Vector4<T> GatherBlue(Sampler sampler, Vector2<float> xy, Vector2<uint> offset = default) => throw null!;
            public Vector4<T> GatherBlue(Sampler sampler, float x, float y, uint offsetX = default, uint offsetY = 0) => throw null!;
            public Vector4<T> GatherBlue(Sampler sampler, Vector2<float> xy, uint offsetX = default, uint offsetY = 0) => throw null!;

            public Vector4<T> GatherAlpha(Sampler sampler, float x, float y, Vector2<uint> offset = default) => throw null!;
            public Vector4<T> GatherAlpha(Sampler sampler, Vector2<float> xy, Vector2<uint> offset = default) => throw null!;
            public Vector4<T> GatherAlpha(Sampler sampler, float x, float y, uint offsetX = default, uint offsetY = 0) => throw null!;
            public Vector4<T> GatherAlpha(Sampler sampler, Vector2<float> xy, uint offsetX = default, uint offsetY = 0) => throw null!;
        }

        protected interface ITypeParamInteger { uint Value; }
        protected struct _1 : ITypeParamInteger { public uint Value => 1; }
        protected struct _2 : ITypeParamInteger { public uint Value => 2; }
        protected struct _4 : ITypeParamInteger { public uint Value => 4; }
        protected struct _8 : ITypeParamInteger { public uint Value => 8; }
        protected struct _16 : ITypeParamInteger { public uint Value => 16; }
        protected struct _32 : ITypeParamInteger { public uint Value => 32; }

        protected class MultiSampledTexture2D<T, TSampleCount> where TSampleCount : struct, ITypeParamInteger
        {
            public uint Width => throw null!;
            public uint Height => throw null!;
            public uint SampleCount => throw null!;
            public Vector2<float> GetSamplePosition(uint sampleIndex) => throw null!;

            public T this[uint sample, uint x, uint y]
        }
    }
}
