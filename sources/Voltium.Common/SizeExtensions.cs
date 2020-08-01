using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Extensions
{
    /// <summary>
    /// Extensions for <see cref="Size"/>
    /// </summary>
    public static class SizeExtensions
    {
        /// <summary>
        /// Returns the aspect ratio (widht / height) for a <see cref="Size"/>
        /// </summary>
        public static float AspectRatio(this Size size) => size.Width / (float)size.Height;
    }
}
