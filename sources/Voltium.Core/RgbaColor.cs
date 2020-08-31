using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Voltium.Common;

namespace Voltium.Core
{
    /// <summary>
    /// Represents a 32 bit RGBA color
    /// </summary>
    [GenerateEquality]
    public partial struct Rgba128
    {
        /// <summary>
        /// The red component of this color
        /// </summary>
        public float R;

        /// <summary>
        /// The green component of this color
        /// </summary>
        public float G;

        /// <summary>
        /// The blue component of this color
        /// </summary>
        public float B;

        /// <summary>
        /// The alpha, or additional, component of this color
        /// </summary>
        public float A;

        /// <summary>
        /// Create a new instance of <see cref="Rgba128"/> from 4 floating point values
        /// </summary>
        /// <param name="r">The red component of this color</param>
        /// <param name="g">The green component of this color</param>
        /// <param name="b">The blue component of this color</param>
        /// <param name="a">The alpha, or additional, component of this color></param>
        public Rgba128(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public Rgba128 WithR(float r)
            => new Rgba128(r, G, B, A);
        public Rgba128 WithG(float g)
            => new Rgba128(R, g, B, A);
        public Rgba128 WithB(float b)
            => new Rgba128(R, G, b, A);
        public Rgba128 WithA(float a)
            => new Rgba128(R, G, B, a);

        public Rgba128 WithRGB(float r, float g, float b)
            => new Rgba128(r, g, b, A);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Create a new instance of <see cref="Rgba128"/> from 4 byte values
        /// </summary>
        /// <param name="r">The red component of this color</param>
        /// <param name="g">The green component of this color</param>
        /// <param name="b">The blue component of this color</param>
        /// <param name="a">The alpha, or additional, component of this color></param>
        public static Rgba128 FromPacked(byte r, byte g, byte b, byte a)
            => new Rgba128(r / 255f, g / 255f, b / 255f, a / 255f);

        /// <summary>
        /// Converts <paramref name="color"/> to an RGB <see cref="Vector3"/>
        /// </summary>
        /// <param name="color">The <see cref="Rgba128"/> to convert</param>
        public static explicit operator Vector3(Rgba128 color) => Unsafe.As<Rgba128, Vector3>(ref color);


        /// <summary>
        /// Converts <paramref name="color"/> to an RGBA <see cref="Vector4"/>
        /// </summary>
        /// <param name="color">The <see cref="Rgba128"/> to convert</param>
        public static explicit operator Vector4(Rgba128 color) => Unsafe.As<Rgba128, Vector4>(ref color);

        // you can pass a length, say, of a span, and get a free lil bit of validation
        internal static unsafe Rgba128 FromPointer(float* p, uint length = 4)
        {
            Debug.Assert(length >= 4);
            return Unsafe.As<float, Rgba128>(ref *p);
        }

        // you can pass a length, say, of a span, and get a free lil bit of validation
        internal static unsafe Rgba128 FromRef(ref float p, uint length = 4)
        {
            Debug.Assert(length >= 4);
            return Unsafe.As<float, Rgba128>(ref p);
        }
    }
}
