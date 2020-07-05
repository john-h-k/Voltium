using System.Runtime.CompilerServices;

namespace Voltium.Common.Pix
{
    /// <summary>
    /// Represents a 32 bit ARGB color used by PIX where A must be 0xFF
    /// </summary>
    public partial struct Argb32
    {
        /// <summary>
        /// Creates a new instance from 3 separate RGB values
        /// </summary>
        /// <param name="r">The red component of the color</param>
        /// <param name="g">The green component of the color</param>
        /// <param name="b">The blue component of the color</param>
        public Argb32(byte r = 0, byte g = 0, byte b = 0)
        {
            R = r;
            G = g;
            B = b;
            _a = 0xFF;
        }

#pragma warning disable 414
        private byte _a;
#pragma warning restore 414

        /// <summary>
        /// The red component of the color
        /// </summary>
        public byte R;

        /// <summary>
        /// The green component of the color
        /// </summary>
        public byte G;

        /// <summary>
        /// The blue component of the color
        /// </summary>
        public byte B;

        /// <summary>
        /// Returns a unique color for each value between 0 and 255 inclusive
        /// </summary>
        /// <param name="i">The value between 0 and 255 inclusive</param>
        /// <returns>A unique color</returns>
        public static Argb32 FromIndex(byte i)
        {
            const int redMask = 0b11100000;
            const int greenMask = 0b00011000;
            const int blueMask = 0b00000111;
            // Split the byte into 3 bytes and split it across the 3 values
            int red = i & redMask;
            int green = i & greenMask;
            int blue = i & blueMask;

            return new Argb32((byte)red, (byte)green, (byte)blue);
        }

        internal static uint GetAs32BitArgb(in Argb32 color) => Unsafe.As<Argb32, uint>(ref Unsafe.AsRef(in color));
    }
}
