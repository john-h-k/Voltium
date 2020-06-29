using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Core
{
    /// <summary>
    /// Represents the format of a backbuffer
    /// </summary>
    public enum BackBufferFormat : uint
    {
        /// <summary>
        /// A four-component, 32-bit unsigned-normalized-integer format that supports 8 bits per channel including alpha
        /// </summary>
        R8G8B8A8UnsignedNormalized = DataFormat.R8G8B8A8UnsignedNormalized,

        /// <summary>
        /// A four-component, 32-bit unsigned-normalized integer sRGB format that supports 8 bits per channel including alpha
        /// </summary>
        R8G8B8A8UnsignedNormalizedSRGB = DataFormat.R8G8B8A8UnsignedNormalizedSRGB,


        /// <summary>
        /// A four-component, 32-bit unsigned-normalized-integer format that supports 8 bits for each color channel and 8-bit alpha
        /// </summary>
        B8G8R8A8UnsignedNormalized = DataFormat.B8G8R8A8UnsignedNormalized,


        /// <summary>
        /// A four-component, 32-bit unsigned-normalized standard RGB format that supports 8 bits for each channel including alpha
        /// </summary>
        B8G8R8A8UnsignedNormalizedSRGB = DataFormat.B8G8R8A8UnsignedNormalizedSRGB,


        /// <summary>
        /// A four-component, 64-bit floating-point format that supports 16 bits per channel including alpha
        /// </summary>
        R16G16B16A16Single = DataFormat.R16G16B16A16Single,

        /// <summary>
        /// A four-component, 32-bit unsigned-normalized-integer format that supports 10 bits for each color and 2 bits for alpha
        /// </summary>
        R10G10B10A2UnsignedNormalized = DataFormat.R10G10B10A2UnsignedNormalized
    }
}
