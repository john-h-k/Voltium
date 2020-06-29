using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TerraFX.Interop.D3D12_FILTER;

namespace Voltium.Core
{
    /// <summary>
    /// Defines the filter used by the <see cref="Sampler"/> when sampling a texture
    /// </summary>
    [Flags]
    public enum SamplerFilterType
    {
        /// <summary>
        /// Use point sampling to select the mipmap
        /// </summary>
        MipPoint = 0,

        /// <summary>
        /// Use point sampling for magnification
        /// </summary>
        MagPoint = 0,

        /// <summary>
        /// Use point sampling for minification
        /// </summary>
        MinPoint = 0,

        /// <summary>
        /// Use point sampling for magnification, minificatiom, and sampling across mipmaps
        /// </summary>
        Point = MipPoint | MagPoint | MinPoint,

        /// <summary>
        /// The mask used to clear the min, mag, and mip states of the filter
        /// </summary>
        MinMagMipMask = MipLinear | MagLinear | MinLinear,

        /// <summary>
        /// Use bilinear sampling across mipmaps
        /// </summary>
        MipLinear = 0b00001,

        /// <summary>
        /// Use bilinear sampling for magnification
        /// </summary>
        MagLinear = 0b00100,

        /// <summary>
        /// Use bilinear sampling for minification
        /// </summary>
        MinLinear = 0b10000,

        /// <summary>
        /// Use bilinear sampling for magnification, minificatiom, and sampling across mipmaps
        /// </summary>
        Linear = MipLinear | MagLinear | MinLinear,

        /// <summary>
        /// Use anisotropic interpolation for mipmaps, magnification, and minifaction
        /// </summary>
        Anistropic = 0b1000000 | MinMagMipMask,

        /// <summary>
        /// The mask used to clear the 
        /// </summary>
        ComparisonMinimumMaximumMask = UseComparisonOperator | Minimum | Maximum,

        /// <summary>
        /// Uses the <see cref="Sampler.ComparisonFunc"/> to compare the texture value to the shader provided comparison value,
        /// and then filter the 0 or 1 value returned by the comparison
        /// </summary>
        UseComparisonOperator = 0x80,

        /// <summary>
        /// Always returns the smaller of two texture values
        /// </summary>
        Minimum = 0x100,

        /// <summary>
        /// Always returns the larger of two texture values
        /// </summary>
        Maximum = 0x180,
    }
}
