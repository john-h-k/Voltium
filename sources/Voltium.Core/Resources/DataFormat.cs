using System;
using static TerraFX.Interop.DirectX.DXGI_FORMAT;
using static Voltium.Core.DataFormat;

namespace Voltium.Core
{
    /// <summary>
    /// Represents the format of data
    /// </summary>
    public enum DataFormat : uint
    {
        /// <summary>
        /// The format is not known
        /// </summary>
        Unknown = DXGI_FORMAT_UNKNOWN,

        /// <summary>
        /// A four-component, 128-bit typeless format that supports 32 bits per channel including alpha
        /// </summary>
        R32G32B32A32Typeless = DXGI_FORMAT_R32G32B32A32_TYPELESS,

        /// <summary>
        /// A four-component, 128-bit floating-point format that supports 32 bits per channel including alpha
        /// </summary>
        R32G32B32A32Single = DXGI_FORMAT_R32G32B32A32_FLOAT,

        /// <summary>
        /// A four-component, 128-bit unsigned-integer format that supports 32 bits per channel including alpha
        /// </summary>
        R32G32B32A32UInt = DXGI_FORMAT_R32G32B32A32_UINT,

        /// <summary>
        /// 	A four-component, 128-bit signed-integer format that supports 32 bits per channel including alpha
        /// </summary>
        R32G32B32A32Int = DXGI_FORMAT_R32G32B32A32_SINT,

        /// <summary>
        /// A three-component, 96-bit typeless format that supports 32 bits per color channel
        /// </summary>
        R32G32B32Typeless = DXGI_FORMAT_R32G32B32_TYPELESS,

        /// <summary>
        /// A three-component, 96-bit floating-point format that supports 32 bits per color channel
        /// </summary>
        R32G32B32Single = DXGI_FORMAT_R32G32B32_FLOAT,

        /// <summary>
        /// A three-component, 96-bit unsigned-integer format that supports 32 bits per color channel
        /// </summary>
        R32G32B32UInt = DXGI_FORMAT_R32G32B32_UINT,

        /// <summary>
        /// A three-component, 96-bit signed-integer format that supports 32 bits per color channel
        /// </summary>
        R32G32B32Int = DXGI_FORMAT_R32G32B32_SINT,

        /// <summary>
        /// A four-component, 64-bit typeless format that supports 16 bits per channel including alpha
        /// </summary>
        R16G16B16A16Typeless = DXGI_FORMAT_R16G16B16A16_TYPELESS,

        /// <summary>
        /// A four-component, 64-bit floating-point format that supports 16 bits per channel including alpha
        /// </summary>
        R16G16B16A16Single = DXGI_FORMAT_R16G16B16A16_FLOAT,

        /// <summary>
        /// A four-component, 64-bit unsigned-normalized-integer format that supports 16 bits per channel including alpha
        /// </summary>
        R16G16B16A16UnsignedNormalized = DXGI_FORMAT_R16G16B16A16_UNORM,

        /// <summary>
        /// A four-component, 64-bit unsigned-integer format that supports 16 bits per channel including alpha
        /// </summary>
        R16G16B16A16UInt = DXGI_FORMAT_R16G16B16A16_UINT,

        /// <summary>
        /// A four-component, 64-bit signed-normalized-integer format that supports 16 bits per channel including alpha
        /// </summary>
        R16G16B16A16Normalized = DXGI_FORMAT_R16G16B16A16_SNORM,

        /// <summary>
        /// A four-component, 64-bit signed-integer format that supports 16 bits per channel including alpha
        /// </summary>
        R16G16B16A16Int = DXGI_FORMAT_R16G16B16A16_SINT,

        /// <summary>
        /// A two-component, 64-bit typeless format that supports 32 bits for the red channel and 32 bits for the green channel
        /// </summary>
        R32G32Typeless = DXGI_FORMAT_R32G32_TYPELESS,

        /// <summary>
        /// A two-component, 64-bit floating-point format that supports 32 bits for the red channel and 32 bits for the green channel
        /// </summary>
        R32G32Single = DXGI_FORMAT_R32G32_FLOAT,

        /// <summary>
        /// A two-component, 64-bit unsigned-integer format that supports 32 bits for the red channel and 32 bits for the green channel
        /// </summary>
        R32G32UInt = DXGI_FORMAT_R32G32_UINT,

        /// <summary>
        /// A two-component, 64-bit signed-integer format that supports 32 bits for the red channel and 32 bits for the green channel
        /// </summary>
        R32G32Int = DXGI_FORMAT_R32G32_SINT,

        /// <summary>
        /// A four-component, 32-bit typeless format that supports 10 bits for each color and 2 bits for alpha
        /// </summary>
        R10G10B10A2Typeless = DXGI_FORMAT_R10G10B10A2_TYPELESS,

        /// <summary>
        /// A four-component, 32-bit unsigned-normalized-integer format that supports 10 bits for each color and 2 bits for alpha
        /// </summary>
        R10G10B10A2UnsignedNormalized = DXGI_FORMAT_R10G10B10A2_UNORM,

        /// <summary>
        /// A four-component, 32-bit unsigned-integer format that supports 10 bits for each color and 2 bits for alpha
        /// </summary>
        R10G10B10A2UInt = DXGI_FORMAT_R10G10B10A2_UINT,

        /// <summary>
        /// Three partial-precision floating-point numbers encoded into a single 32-bit value (a variant of s10e5, which is sign bit, 10-bit mantissa, and 5-bit biased (15) exponent).
        /// There are no sign bits, and there is a 5-bit biased (15) exponent for each channel, 6-bit mantissa for R and G, and a 5-bit mantissa for B
        /// </summary>
        R11G11B10Single = DXGI_FORMAT_R11G11B10_FLOAT,

        /// <summary>
        /// A four-component, 32-bit typeless format that supports 8 bits per channel including alpha
        /// </summary>
        R8G8B8A8Typeless = DXGI_FORMAT_R8G8B8A8_TYPELESS,

        /// <summary>
        /// A four-component, 32-bit unsigned-normalized-integer format that supports 8 bits per channel including alpha
        /// </summary>
        R8G8B8A8UnsignedNormalized = DXGI_FORMAT_R8G8B8A8_UNORM,

        /// <summary>
        /// A four-component, 32-bit unsigned-normalized integer sRGB format that supports 8 bits per channel including alpha
        /// </summary>
        R8G8B8A8UnsignedNormalizedSRGB = DXGI_FORMAT_R8G8B8A8_UNORM_SRGB,

        /// <summary>
        /// 	A four-component, 32-bit unsigned-integer format that supports 8 bits per channel including alpha
        /// </summary>
        R8G8B8A8UInt = DXGI_FORMAT_R8G8B8A8_UINT,

        /// <summary>
        /// A four-component, 32-bit signed-normalized-integer format that supports 8 bits per channel including alpha
        /// </summary>
        R8G8B8A8Normalized = DXGI_FORMAT_R8G8B8A8_SNORM,

        /// <summary>
        /// A four-component, 32-bit signed-integer format that supports 8 bits per channel including alpha
        /// </summary>
        R8G8B8A8Int = DXGI_FORMAT_R8G8B8A8_SINT,

        /// <summary>
        /// A two-component, 32-bit typeless format that supports 16 bits for the red channel and 16 bits for the green channel
        /// </summary>
        R16G16Typeless = DXGI_FORMAT_R16G16_TYPELESS,

        /// <summary>
        /// A two-component, 32-bit floating-point format that supports 16 bits for the red channel and 16 bits for the green channel
        /// </summary>
        R16G16Single = DXGI_FORMAT_R16G16_FLOAT,

        /// <summary>
        /// A two-component, 32-bit unsigned-normalized-integer format that supports 16 bits each for the green and red channels
        /// </summary>
        R16G16UnsignedNormalized = DXGI_FORMAT_R16G16_UNORM,

        /// <summary>
        /// A two-component, 32-bit unsigned-integer format that supports 16 bits for the red channel and 16 bits for the green channel
        /// </summary>
        R16G16UInt = DXGI_FORMAT_R16G16_UINT,

        /// <summary>
        /// A two-component, 32-bit signed-normalized-integer format that supports 16 bits for the red channel and 16 bits for the green channel
        /// </summary>
        R16G16Normalized = DXGI_FORMAT_R16G16_SNORM,

        /// <summary>
        /// A two-component, 32-bit signed-integer format that supports 16 bits for the red channel and 16 bits for the green channel
        /// </summary>
        R16G16Int = DXGI_FORMAT_R16G16_SINT,

        /// <summary>
        /// A single-component, 32-bit typeless format that supports 32 bits for the red channel
        /// </summary>
        R32Typeless = DXGI_FORMAT_R32_TYPELESS,

        /// <summary>
        /// A single-component, 32-bit floating-point format that supports 32 bits for depth
        /// </summary>
        Depth32Single = DXGI_FORMAT_D32_FLOAT,

        /// <summary>
        /// A single-component, 32-bit floating-point format that supports 32 bits for the red channel
        /// </summary>
        R32Single = DXGI_FORMAT_R32_FLOAT,

        /// <summary>
        ///	A single-component, 32-bit unsigned-integer format that supports 32 bits for the red channel
        /// </summary>
        R32UInt = DXGI_FORMAT_R32_UINT,

        /// <summary>
        /// A single-component, 32-bit signed-integer format that supports 32 bits for the red channel
        /// </summary>
        R32Int = DXGI_FORMAT_R32_SINT,

        /// <summary>
        /// A two-component, 32-bit typeless format that supports 24 bits for the red channel and 8 bits for the green channel
        /// </summary>
        R24G8Typeless = DXGI_FORMAT_R24G8_TYPELESS,

        /// <summary>
        /// A 32-bit z-buffer format that supports 24 bits for depth and 8 bits for stencil
        /// </summary>
        Depth24UnsignedNormalizedS8UInt = DXGI_FORMAT_D24_UNORM_S8_UINT,

        /// <summary>
        /// A 32-bit format, that contains a 24 bit, single-component, unsigned-normalized integer, with an additional typeless 8 bits. This format has 24 bits red channel and 8 bits unused
        /// </summary>
        R24UnsignedNormalizedX8Typeless = DXGI_FORMAT_R24_UNORM_X8_TYPELESS,

        /// <summary>
        /// A two-component, 16-bit typeless format that supports 8 bits for the red channel and 8 bits for the green channel
        /// </summary>
        R8G8Typeless = DXGI_FORMAT_R8G8_TYPELESS,

        /// <summary>
        /// A two-component, 16-bit unsigned-normalized-integer format that supports 8 bits for the red channel and 8 bits for the green channel
        /// </summary>
        R8G8UnsignedNormalized = DXGI_FORMAT_R8G8_UNORM,

        /// <summary>
        /// A two-component, 16-bit unsigned-integer format that supports 8 bits for the red channel and 8 bits for the green channel
        /// </summary>
        R8G8UInt = DXGI_FORMAT_R8G8_UINT,

        /// <summary>
        /// A two-component, 16-bit signed-normalized-integer format that supports 8 bits for the red channel and 8 bits for the green channel
        /// </summary>
        R8G8Normalized = DXGI_FORMAT_R8G8_SNORM,

        /// <summary>
        /// A two-component, 16-bit signed-integer format that supports 8 bits for the red channel and 8 bits for the green channel
        /// </summary>
        R8G8Int = DXGI_FORMAT_R8G8_SINT,

        /// <summary>
        /// A single-component, 16-bit typeless format that supports 16 bits for the red channel
        /// </summary>
        R16Typeless = DXGI_FORMAT_R16_TYPELESS,

        /// <summary>
        /// A single-component, 16-bit floating-point format that supports 16 bits for the red channel
        /// </summary>
        R16Single = DXGI_FORMAT_R16_FLOAT,

        /// <summary>
        /// A single-component, 16-bit unsigned-normalized-integer format that supports 16 bits for depth
        /// </summary>
        Depth16UnsignedNormalized = DXGI_FORMAT_D16_UNORM,

        /// <summary>
        /// 	A single-component, 16-bit unsigned-normalized-integer format that supports 16 bits for the red channel
        /// </summary>
        R16UnsignedNormalized = DXGI_FORMAT_R16_UNORM,

        /// <summary>
        /// 	A single-component, 16-bit unsigned-integer format that supports 16 bits for the red channel
        /// </summary>
        R16UInt = DXGI_FORMAT_R16_UINT,

        /// <summary>
        /// A single-component, 16-bit signed-normalized-integer format that supports 16 bits for the red channel
        /// </summary>
        R16Normalized = DXGI_FORMAT_R16_SNORM,

        /// <summary>
        /// A single-component, 16-bit signed-integer format that supports 16 bits for the red channel
        /// </summary>
        R16Int = DXGI_FORMAT_R16_SINT,

        /// <summary>
        /// A single-component, 8-bit typeless format that supports 8 bits for the red channel
        /// </summary>
        R8Typeless = DXGI_FORMAT_R8_TYPELESS,

        /// <summary>
        /// A single-component, 8-bit unsigned-normalized-integer format that supports 8 bits for the red channel
        /// </summary>
        R8UnsignedNormalized = DXGI_FORMAT_R8_UNORM,

        /// <summary>
        /// A single-component, 8-bit unsigned-integer format that supports 8 bits for the red channel
        /// </summary>
        R8UInt = DXGI_FORMAT_R8_UINT,

        /// <summary>
        /// A single-component, 8-bit signed-normalized-integer format that supports 8 bits for the red channel
        /// </summary>
        R8Normalized = DXGI_FORMAT_R8_SNORM,

        /// <summary>
        /// A single-component, 8-bit signed-integer format that supports 8 bits for the red channel
        /// </summary>
        R8Int = DXGI_FORMAT_R8_SINT,

        /// <summary>
        /// A single-component, 8-bit unsigned-normalized-integer format for alpha only
        /// </summary>
        A8UnsignedNormalized = DXGI_FORMAT_A8_UNORM,

        /// <summary>
        ///  A single-component, 1-bit unsigned-normalized integer format that supports 1 bit for the red channel
        /// </summary>
        R1UnsignedNormalized = DXGI_FORMAT_R1_UNORM,

        /// <summary>
        ///	Four-component typeless block-compression format
        /// </summary>
        BC1Typeless = DXGI_FORMAT_BC1_TYPELESS,

        /// <summary>
        /// Four-component block-compression format
        /// </summary>
        BC1UnsignedNormalized = DXGI_FORMAT_BC1_UNORM,

        /// <summary>
        /// Four-component block-compression format for sRGB data
        /// </summary>
        BC1UnsignedNormalizedSRGB = DXGI_FORMAT_BC1_UNORM_SRGB,

        /// <summary>
        /// Four-component typeless block-compression format
        /// </summary>
        BC2Typeless = DXGI_FORMAT_BC2_TYPELESS,

        /// <summary>
        /// Four-component block-compression format
        /// </summary>
        BC2UnsignedNormalized = DXGI_FORMAT_BC2_UNORM,

        /// <summary>
        /// Four-component block-compression format for sRGB data
        /// </summary>
        BC2UnsignedNormalizedSRGB = DXGI_FORMAT_BC2_UNORM_SRGB,

        /// <summary>
        /// Four-component typeless block-compression format
        /// </summary>
        BC3Typeless = DXGI_FORMAT_BC3_TYPELESS,

        /// <summary>
        /// Four-component block-compression format
        /// </summary>
        BC3UnsignedNormalized = DXGI_FORMAT_BC3_UNORM,

        /// <summary>
        /// Four-component block-compression format for sRGB data
        /// </summary>
        BC3UnsignedNormalizedSRGB = DXGI_FORMAT_BC3_UNORM_SRGB,

        /// <summary>
        /// One-component typeless block-compression format
        /// </summary>
        BC4Typeless = DXGI_FORMAT_BC4_TYPELESS,

        /// <summary>
        /// One-component block-compression format
        /// </summary>
        BC4UnsignedNormalized = DXGI_FORMAT_BC4_UNORM,

        /// <summary>
        /// One-component block-compression format
        /// </summary>
        BC4Normalized = DXGI_FORMAT_BC4_SNORM,

        /// <summary>
        /// Two-component typeless block-compression format
        /// </summary>
        BC5Typeless = DXGI_FORMAT_BC5_TYPELESS,

        /// <summary>
        /// Two-component typeless block-compression format
        /// </summary>
        BC5UnsignedNormalized = DXGI_FORMAT_BC5_UNORM,

        /// <summary>
        /// Two-component typeless block-compression format
        /// </summary>
        BC5Normalized = DXGI_FORMAT_BC5_SNORM,

        /// <summary>
        /// A three-component, 16-bit unsigned-normalized-integer format that supports 5 bits for blue, 6 bits for green, and 5 bits for red
        /// </summary>
        B5G6R5UnsignedNormalized = DXGI_FORMAT_B5G6R5_UNORM,

        /// <summary>
        /// A four-component, 16-bit unsigned-normalized-integer format that supports 5 bits for each color channel and 1-bit alpha
        /// </summary>
        B5G5R5A1UnsignedNormalized = DXGI_FORMAT_B5G5R5A1_UNORM,

        /// <summary>
        /// A four-component, 32-bit unsigned-normalized-integer format that supports 8 bits for each color channel and 8-bit alpha
        /// </summary>
        B8G8R8A8UnsignedNormalized = DXGI_FORMAT_B8G8R8A8_UNORM,

        /// <summary>
        /// A four-component, 32-bit unsigned-normalized-integer format that supports 8 bits for each color channel and 8 bits unused
        /// </summary>
        B8G8R8X8UnsignedNormalized = DXGI_FORMAT_B8G8R8X8_UNORM,

        /// <summary>
        /// A four-component, 32-bit typeless format that supports 8 bits for each channel including alpha
        /// </summary>
        B8G8R8A8Typeless = DXGI_FORMAT_B8G8R8A8_TYPELESS,

        /// <summary>
        /// A four-component, 32-bit unsigned-normalized standard RGB format that supports 8 bits for each channel including alpha
        /// </summary>
        B8G8R8A8UnsignedNormalizedSRGB = DXGI_FORMAT_B8G8R8A8_UNORM_SRGB,

        /// <summary>
        /// A four-component, 32-bit typeless format that supports 8 bits for each color channel, and 8 bits are unused
        /// </summary>
        B8G8R8X8Typeless = DXGI_FORMAT_B8G8R8X8_TYPELESS,

        /// <summary>
        /// A four-component, 32-bit unsigned-normalized standard RGB format that supports 8 bits for each color channel, and 8 bits are unused
        /// </summary>
        B8G8R8X8UnsignedNormalizedSRGB = DXGI_FORMAT_B8G8R8X8_UNORM_SRGB,

        /// <summary>
        /// A typeless block-compression format
        /// </summary>
        BC6HTypeless = DXGI_FORMAT_BC6H_TYPELESS,

        /// <summary>
        /// A typeless block-compression format
        /// </summary>
        BC6HUF16 = DXGI_FORMAT_BC6H_UF16,

        /// <summary>
        /// A typeless block-compression format
        /// </summary>
        BC6HSF16 = DXGI_FORMAT_BC6H_SF16,

        /// <summary>
        /// A block-compression format
        /// </summary>
        BC7Typeless = DXGI_FORMAT_BC7_TYPELESS,

        /// <summary>
        /// A block-compression format
        /// </summary>
        BC7UnsignedNormalized = DXGI_FORMAT_BC7_UNORM,

        /// <summary>
        /// A block-compression format
        /// </summary>
        BC7UnsignedNormalizedSRGB = DXGI_FORMAT_BC7_UNORM_SRGB,

        /// <summary>
        /// 
        /// </summary>

        OPAQUE_420 = DXGI_FORMAT_420_OPAQUE, // not supported but gotta have 420  😎
    }


    public static class DataFormatExtensions
    {
        public static bool IsBlockCompressed(this DataFormat format)
            => format is BC1Typeless or BC1UnsignedNormalized or BC1UnsignedNormalizedSRGB
                      or BC2Typeless or BC2UnsignedNormalized or BC2UnsignedNormalizedSRGB
                      or BC3Typeless or BC3UnsignedNormalized or BC3UnsignedNormalizedSRGB
                      or BC4Typeless or BC4UnsignedNormalized or BC4Normalized
                      or BC5Typeless or BC5UnsignedNormalized or BC5Normalized
                      or BC6HTypeless or BC6HSF16 or BC6HUF16
                      or BC7Typeless or BC7UnsignedNormalized or BC7UnsignedNormalizedSRGB;

        public static bool IsPlanar(this DataFormat format)
            => format is Depth32Single or Depth16UnsignedNormalized or Depth24UnsignedNormalizedS8UInt;

        public static uint BytesPerPixel(this DataFormat format) => format.BitsPerPixel() / 8;
        public static uint BytesPer4x4Block(this DataFormat format) => format.BitsPer4x4Block() / 8;

        public static uint BitsPer4x4Block(this DataFormat format)
            => format switch
            {
                BC1Typeless => 8 * 8,
                BC1UnsignedNormalized => 8 * 8,
                BC1UnsignedNormalizedSRGB => 8 * 8,

                BC2Typeless => 16 * 8,
                BC2UnsignedNormalized => 16 * 8,
                BC2UnsignedNormalizedSRGB => 16 * 8,

                BC3Typeless => 16 * 8,
                BC3UnsignedNormalized => 16 * 8,
                BC3UnsignedNormalizedSRGB => 16 * 8,

                BC4Typeless => 8 * 8,
                BC4UnsignedNormalized => 8 * 8,
                BC4Normalized => 8 * 8,

                BC5Typeless => 16 * 8,
                BC5UnsignedNormalized => 16 * 8,
                BC5Normalized => 16 * 8,

                BC6HTypeless => 16 * 8,
                BC6HUF16 => 16 * 8,
                BC6HSF16 => 16 * 8,

                BC7Typeless => 16 * 8,
                BC7UnsignedNormalized => 16 * 8,
                BC7UnsignedNormalizedSRGB => 16 * 8,

                _ => BitsPerPixel(format) * 4 * 4
            };

        public static uint PlaneCount(this DataFormat format)
            => format switch
            {
                Depth24UnsignedNormalizedS8UInt => 2,
                _ => 1,
            };

        public static uint BitsPerPixel(this DataFormat format)
            => format switch
            {
                Unknown => 0,

                R32G32B32A32Typeless => 128,
                R32G32B32A32Single => 128,
                R32G32B32A32UInt => 128,
                R32G32B32A32Int => 128,

                R32G32B32Typeless => 96,
                R32G32B32Single => 96,
                R32G32B32UInt => 96,
                R32G32B32Int => 96,

                R16G16B16A16Typeless => 64,
                R16G16B16A16Single => 64,
                R16G16B16A16UnsignedNormalized => 64,
                R16G16B16A16UInt => 64,
                R16G16B16A16Normalized => 64,
                R16G16B16A16Int => 64,
                R32G32Typeless => 64,
                R32G32Single => 64,
                R32G32UInt => 64,
                R32G32Int => 64,

                R10G10B10A2Typeless => 32,
                R10G10B10A2UnsignedNormalized => 32,
                R10G10B10A2UInt => 32,
                R11G11B10Single => 32,
                R8G8B8A8Typeless => 32,
                R8G8B8A8UnsignedNormalized => 32,
                R8G8B8A8UnsignedNormalizedSRGB => 32,
                R8G8B8A8UInt => 32,
                R8G8B8A8Normalized => 32,
                R8G8B8A8Int => 32,
                R16G16Typeless => 32,
                R16G16Single => 32,
                R16G16UnsignedNormalized => 32,
                R16G16UInt => 32,
                R16G16Normalized => 32,
                R16G16Int => 32,
                R32Typeless => 32,
                Depth32Single => 32,
                R32Single => 32,
                R32UInt => 32,
                R32Int => 32,
                R24G8Typeless => 32,
                Depth24UnsignedNormalizedS8UInt => 32,
                R24UnsignedNormalizedX8Typeless => 32,

                R8G8Typeless => 16,
                R8G8UnsignedNormalized => 16,
                R8G8UInt => 16,
                R8G8Normalized => 16,
                R8G8Int => 16,
                R16Typeless => 16,
                R16Single => 16,
                Depth16UnsignedNormalized => 16,
                R16UnsignedNormalized => 16,
                R16UInt => 16,
                R16Normalized => 16,
                R16Int => 16,

                R8Typeless => 8,
                R8UnsignedNormalized => 8,
                R8UInt => 8,
                R8Normalized => 8,
                R8Int => 8,
                A8UnsignedNormalized => 8,

                R1UnsignedNormalized => 1,


                B5G6R5UnsignedNormalized => 16,
                B5G5R5A1UnsignedNormalized => 16,

                B8G8R8A8UnsignedNormalized => 32,
                B8G8R8X8UnsignedNormalized => 32,
                B8G8R8A8Typeless => 32,
                B8G8R8A8UnsignedNormalizedSRGB => 32,
                B8G8R8X8Typeless => 32,
                B8G8R8X8UnsignedNormalizedSRGB => 32,

                BC1Typeless => throw new System.NotImplementedException(),
                BC1UnsignedNormalized => throw new System.NotImplementedException(),
                BC1UnsignedNormalizedSRGB => throw new System.NotImplementedException(),
                BC2Typeless => throw new System.NotImplementedException(),
                BC2UnsignedNormalized => throw new System.NotImplementedException(),
                BC2UnsignedNormalizedSRGB => throw new System.NotImplementedException(),
                BC3Typeless => throw new System.NotImplementedException(),
                BC3UnsignedNormalized => throw new System.NotImplementedException(),
                BC3UnsignedNormalizedSRGB => throw new System.NotImplementedException(),
                BC4Typeless => throw new System.NotImplementedException(),
                BC4UnsignedNormalized => throw new System.NotImplementedException(),
                BC4Normalized => throw new System.NotImplementedException(),
                BC5Typeless => throw new System.NotImplementedException(),
                BC5UnsignedNormalized => throw new System.NotImplementedException(),
                BC5Normalized => throw new System.NotImplementedException(),
                BC6HTypeless => throw new System.NotImplementedException(),
                BC6HUF16 => throw new System.NotImplementedException(),
                BC6HSF16 => throw new System.NotImplementedException(),
                BC7Typeless => throw new System.NotImplementedException(),
                BC7UnsignedNormalized => throw new System.NotImplementedException(),
                BC7UnsignedNormalizedSRGB => throw new System.NotImplementedException(),
                OPAQUE_420 => throw new System.NotImplementedException(),

                _ => throw new ArgumentOutOfRangeException(nameof(format))
            };
    }
}
