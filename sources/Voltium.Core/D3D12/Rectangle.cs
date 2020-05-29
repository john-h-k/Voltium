using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core
{
    /// <summary>
    /// Represents a rectangle as a top-left and bottom-right corner coordinates
    /// </summary>
    public readonly struct Rectangle
    {
        /// <summary>
        /// The x-coordinate of the top-left corner
        /// </summary>
        public readonly int Left;

        /// <summary>
        /// The y-coordinate of the top-left corner
        /// </summary>
        public readonly int Top;

        /// <summary>
        /// The x-coordinate of the bottom-right corner
        /// </summary>
        public readonly int Right;

        /// <summary>
        /// The y-coordinate of the bottom-right corner
        /// </summary>
        public readonly int Bottom;

        /// <summary>
        /// Create a new instance of <see cref="Rectangle"/>
        /// </summary>
        public Rectangle(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        /// <summary>
        /// Create a new instance of <see cref="Rectangle"/> from a WIN32 <see cref="RECT"/>
        /// </summary>
        public Rectangle(RECT rectangle)
        {
            this = Unsafe.As<RECT, Rectangle>(ref rectangle);
        }

        /// <summary>
        /// Adjusts
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public static Rectangle Adjust(Rectangle rectangle)
        {
            // todo
            ThrowHelper.ThrowNotImplementedException();
            return default;
        }
    }
}
