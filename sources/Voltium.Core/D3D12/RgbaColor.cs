using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core
{
    /// <summary>
    /// Represents a 32 bit RGBA color
    /// </summary>
    [GenerateEquality]
    public partial struct RgbaColor 
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
        /// Create a new instance of <see cref="RgbaColor"/> from 4 floating point values
        /// </summary>
        /// <param name="r">The red component of this color</param>
        /// <param name="g">The green component of this color</param>
        /// <param name="b">The blue component of this color</param>
        /// <param name="a">The alpha, or additional, component of this color></param>
        public RgbaColor(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        /// <summary>
        /// Converts <paramref name="color"/> to an RGB <see cref="Vector3"/>
        /// </summary>
        /// <param name="color">The <see cref="RgbaColor"/> to convert</param>
        public static explicit operator Vector3(RgbaColor color) => Unsafe.As<RgbaColor, Vector3>(ref color);


        /// <summary>
        /// Converts <paramref name="color"/> to an RGBA <see cref="Vector4"/>
        /// </summary>
        /// <param name="color">The <see cref="RgbaColor"/> to convert</param>
        public static explicit operator Vector4(RgbaColor color) => Unsafe.As<RgbaColor, Vector4>(ref color);

        // you can pass a length, say, of a span, and get a free lil bit of validation
        internal static unsafe RgbaColor FromPointer(float* p, uint length = 4)
        {
            Debug.Assert(length >= 4);
            return Unsafe.As<float, RgbaColor>(ref *p);
        }
    }
}
