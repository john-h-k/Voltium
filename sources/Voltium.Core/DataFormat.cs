using TerraFX.Interop;
using Voltium.Core.Managers.Shaders;
using static TerraFX.Interop.DXGI_FORMAT;

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
        D32Single = DXGI_FORMAT_D32_FLOAT,

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
        D24UnsignedNormalizedS8UInt = DXGI_FORMAT_D24_UNORM_S8_UINT,

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
        D16UnsignedNormalized = DXGI_FORMAT_D16_UNORM,

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

    /// <summary>
    /// Defines what class of input data a given <see cref="ShaderInput"/> is
    /// </summary>
    public enum InputClass
    {
        /// <summary>
        /// The data is per-vertex
        /// </summary>
        PerVertex = D3D12_INPUT_CLASSIFICATION.D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,

        /// <summary>
        /// The data is per-instance
        /// </summary>
        PerInstance = D3D12_INPUT_CLASSIFICATION.D3D12_INPUT_CLASSIFICATION_PER_INSTANCE_DATA
    }
}
