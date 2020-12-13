using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using TerraFX.Interop;
using static TerraFX.Interop.DXGI_FORMAT;
using static Voltium.TextureLoading.DDS.PixelFormatFlags;

// ReSharper disable CommentTypo

namespace Voltium.TextureLoading.DDS
{
    internal static class InteropTypeUtilities
    {
        public static AlphaMode GetAlphaMode(in DDSHeader header)
        {
            PixelFormatFlags flags = header.DdsPixelFormat.Flags;
            if (flags.HasFlag(DDS_FOURCC))
            {
                if (MakeFourCC('D', 'X', '1', '0') == header.DdsPixelFormat.FourCC)
                {
                    ref readonly DDSHeaderDxt10 d3d10Ext =
                        ref Unsafe.As<DDSHeader, DDSHeaderDxt10>(ref Unsafe.Add(ref Unsafe.AsRef(in header), 1));
                    var mode = (AlphaMode)(d3d10Ext.MiscFlags2 &
                                           (uint)DDS_MISC_FLAGS2.DDS_MISC_FLAGS2_ALPHA_MODE_MASK);
                    switch (mode)
                    {
                        case AlphaMode.Straight:
                        case AlphaMode.Premultiplied:
                        case AlphaMode.Opaque:
                        case AlphaMode.Custom:
                            return mode;
                    }
                }
                else if ((MakeFourCC('D', 'X', 'T', '2') == header.DdsPixelFormat.FourCC)
                         || (MakeFourCC('D', 'X', 'T', '4') == header.DdsPixelFormat.FourCC))
                {
                    return AlphaMode.Premultiplied;
                }
            }

            return AlphaMode.Unknown;
        }

        public static DXGI_FORMAT MakeSrgb(DXGI_FORMAT format)
        {
            switch (format)
            {
                case DXGI_FORMAT_R8G8B8A8_UNORM:
                    return DXGI_FORMAT_R8G8B8A8_UNORM_SRGB;

                case DXGI_FORMAT_BC1_UNORM:
                    return DXGI_FORMAT_BC1_UNORM_SRGB;

                case DXGI_FORMAT_BC2_UNORM:
                    return DXGI_FORMAT_BC2_UNORM_SRGB;

                case DXGI_FORMAT_BC3_UNORM:
                    return DXGI_FORMAT_BC3_UNORM_SRGB;

                case DXGI_FORMAT_B8G8R8A8_UNORM:
                    return DXGI_FORMAT_B8G8R8A8_UNORM_SRGB;

                case DXGI_FORMAT_B8G8R8X8_UNORM:
                    return DXGI_FORMAT_B8G8R8X8_UNORM_SRGB;

                case DXGI_FORMAT_BC7_UNORM:
                    return DXGI_FORMAT_BC7_UNORM_SRGB;

                default:
                    return format;
            }
        }

        private readonly struct Bitmask16 : IEquatable<Bitmask16>
        {
            public Bitmask16(uint e0, uint e1, uint e2, uint e3)
            {
                E0 = e0;
                E1 = e1;
                E2 = e2;
                E3 = e3;
            }

            public override string ToString() => $"{E0}, {E1}, {E2}, {E3}";

            public readonly uint E0, E1, E2, E3;

            public bool Equals(Bitmask16 other) => E0 == other.E0 && E1 == other.E1 && E2 == other.E2 && E3 == other.E3;

            public override bool Equals(object? obj) => obj is Bitmask16 other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(E0, E1, E2, E3);
        }

        public static bool IsBitmask(in DDSPixelFormat format, uint r, uint g, uint b, uint a)
        {
            if (Sse2.IsSupported)
            {

                return Sse2.MoveMask(Sse2.CompareEqual(Unsafe.As<uint, Vector128<uint>>(ref Unsafe.AsRef(in format.RBitMask)), Vector128.Create(r, g, b, a)).AsByte()) == 0xFFFF;
            }

            return format.RBitMask == r &&
                          format.GBitMask == g &&
                          format.BBitMask == b &&
                          format.ABitMask == a;
        }

        public static DXGI_FORMAT GetDxgiFormat(in DDSPixelFormat pixelFormat)
        {
            PixelFormatFlags flags = pixelFormat.Flags;
            if (flags.HasFlag(DDS_RGB))
            {
                // Note that sRGB formats are written using the "DX10" extended header.

                switch (pixelFormat.RgbBitCount)
                {
                    case 32:
                        if (IsBitmask(pixelFormat, 0x000000ff, 0x0000ff00, 0x00ff0000, 0xff000000))
                        {
                            return DXGI_FORMAT_R8G8B8A8_UNORM;
                        }

                        if (IsBitmask(pixelFormat, 0x00ff0000, 0x0000ff00, 0x000000ff, 0xff000000))
                        {
                            return DXGI_FORMAT_B8G8R8A8_UNORM;
                        }

                        if (IsBitmask(pixelFormat, 0x00ff0000, 0x0000ff00, 0x000000ff, 0x00000000))
                        {
                            return DXGI_FORMAT_B8G8R8X8_UNORM;
                        }

                        // No DXGI format maps to ISBITMASK(0x000000ff, 0x0000ff00, 0x00ff0000, 0x00000000) aka D3DFMT_X8B8G8R8

                        // Note that many common DDS reader/writers (including D3DX) swap the
                        // the RED/BLUE masks for 10:10:10:2 formats. We assumme
                        // below that the 'backwards' header mask is being used since it is most
                        // likely written by D3DX. The more robust solution is to use the 'DX10'
                        // header extension and specify the DXGI_FORMAT_R10G10B10A2_UNORM format directly

                        // For 'correct' writers, this should be 0x000003ff, 0x000ffc00, 0x3ff00000 for RGB data
                        if (IsBitmask(pixelFormat, 0x3ff00000, 0x000ffc00, 0x000003ff, 0xc0000000))
                        {
                            return DXGI_FORMAT_R10G10B10A2_UNORM;
                        }

                        // No DXGI format maps to ISBITMASK(0x000003ff, 0x000ffc00, 0x3ff00000, 0xc0000000) aka D3DFMT_A2R10G10B10

                        if (IsBitmask(pixelFormat, 0x0000ffff, 0xffff0000, 0x00000000, 0x00000000))
                        {
                            return DXGI_FORMAT_R16G16_UNORM;
                        }

                        if (IsBitmask(pixelFormat, 0xffffffff, 0x00000000, 0x00000000, 0x00000000))
                        {
                            // Only 32-bit color channel format in D3D9 was R32F.
                            return DXGI_FORMAT_R32_FLOAT; // D3DX writes this out as a FourCC of 114.
                        }

                        break;

                    case 24:
                        // No 24bpp DXGI formats aka D3DFMT_R8G8B8
                        break;

                    case 16:
                        if (IsBitmask(pixelFormat, 0x7c00, 0x03e0, 0x001f, 0x8000))
                        {
                            return DXGI_FORMAT_B5G5R5A1_UNORM;
                        }

                        if (IsBitmask(pixelFormat, 0xf800, 0x07e0, 0x001f, 0x0000))
                        {
                            return DXGI_FORMAT_B5G6R5_UNORM;
                        }

                        // No DXGI format maps to ISBITMASK(0x7c00, 0x03e0, 0x001f, 0x0000) aka D3DFMT_X1R5G5B5.
                        if (IsBitmask(pixelFormat, 0x0f00, 0x00f0, 0x000f, 0xf000))
                        {
                            return DXGI_FORMAT_B4G4R4A4_UNORM;
                        }

                        // No DXGI format maps to ISBITMASK(0x0f00, 0x00f0, 0x000f, 0x0000) aka D3DFMT_X4R4G4B4.

                        // No 3:3:2, 3:3:2:8, or paletted DXGI formats aka D3DFMT_A8R3G3B2, D3DFMT_R3G3B2, D3DFMT_P8, D3DFMT_A8P8, etc.
                        break;
                }
            }
            else if (flags.HasFlag(DDS_LUMINANCE))
            {
                if (8 == pixelFormat.RgbBitCount)
                {
                    if (IsBitmask(pixelFormat, 0x000000ff, 0x00000000, 0x00000000, 0x00000000))
                    {
                        return DXGI_FORMAT_R8_UNORM; // D3DX10/11 writes this out as DX10 extension
                    }

                    // No DXGI format maps to ISBITMASK(0x0f, 0x00, 0x00, 0xf0) aka D3DFMT_A4L4.
                }

                if (16 == pixelFormat.RgbBitCount)
                {
                    if (IsBitmask(pixelFormat, 0x0000ffff, 0x00000000, 0x00000000, 0x00000000))
                    {
                        return DXGI_FORMAT_R16_UNORM; // D3DX10/11 writes this out as DX10 extension.
                    }

                    if (IsBitmask(pixelFormat, 0x000000ff, 0x00000000, 0x00000000, 0x0000ff00))
                    {
                        return DXGI_FORMAT_R8G8_UNORM; // D3DX10/11 writes this out as DX10 extension.
                    }
                }
            }
            else if (flags.HasFlag(DDS_ALPHA))
            {
                if (pixelFormat.RgbBitCount == 8)
                {
                    return DXGI_FORMAT_A8_UNORM;
                }
            }
            else if (flags.HasFlag(DDS_FOURCC))
            {
                if (MakeFourCC('D', 'X', 'T', '1') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT_BC1_UNORM;
                }

                if (MakeFourCC('D', 'X', 'T', '3') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT_BC2_UNORM;
                }

                if (MakeFourCC('D', 'X', 'T', '5') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT_BC3_UNORM;
                }

                // While pre-multiplied alpha isn't directly supported by the DXGI formats,
                // they are basically the same as these BC formats so they can be mapped
                if (MakeFourCC('D', 'X', 'T', '2') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT_BC2_UNORM;
                }

                if (MakeFourCC('D', 'X', 'T', '4') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT_BC3_UNORM;
                }

                if (MakeFourCC('A', 'T', 'I', '1') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT_BC4_UNORM;
                }

                if (MakeFourCC('B', 'C', '4', 'U') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT_BC4_UNORM;
                }

                if (MakeFourCC('B', 'C', '4', 'S') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT_BC4_SNORM;
                }

                if (MakeFourCC('A', 'T', 'I', '2') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT_BC5_UNORM;
                }

                if (MakeFourCC('B', 'C', '5', 'U') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT_BC5_UNORM;
                }

                if (MakeFourCC('B', 'C', '5', 'S') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT_BC5_SNORM;
                }

                // BC6H and BC7 are written using the "DX10" extended header

                if (MakeFourCC('R', 'G', 'B', 'G') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT_R8G8_B8G8_UNORM;
                }

                if (MakeFourCC('G', 'R', 'G', 'B') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT_G8R8_G8B8_UNORM;
                }

                if (MakeFourCC('Y', 'U', 'Y', '2') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT_YUY2;
                }

                return pixelFormat.FourCC switch
                {
                    D3DFormat.D3DFMT_A16B16G16R16 => // D3DFMT_A16B16G16R16
                    DXGI_FORMAT_R16G16B16A16_UNORM,

                    D3DFormat.D3DFMT_Q16W16V16U16 => // D3DFMT_Q16W16V16U16
                    DXGI_FORMAT_R16G16B16A16_SNORM,

                    D3DFormat.D3DFMT_R16F => // D3DFMT_R16F
                    DXGI_FORMAT_R16_FLOAT,

                    D3DFormat.D3DFMT_G16R16F => // D3DFMT_G16R16F
                    DXGI_FORMAT_R16G16_FLOAT,

                    D3DFormat.D3DFMT_A16B16G16R16F => // D3DFMT_A16B16G16R16F
                    DXGI_FORMAT_R16G16B16A16_FLOAT,

                    D3DFormat.D3DFMT_R32F => // D3DFMT_R32F
                    DXGI_FORMAT_R32_FLOAT,

                    D3DFormat.D3DFMT_G32R32F => // D3DFMT_G32R32F
                    DXGI_FORMAT_R32G32_FLOAT,

                    D3DFormat.D3DFMT_A32B32G32R32F => // D3DFMT_A32B32G32R32F
                    DXGI_FORMAT_R32G32B32A32_FLOAT,

                    _ => DXGI_FORMAT_UNKNOWN
                };
            }

            return DXGI_FORMAT_UNKNOWN;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static D3DFormat MakeFourCC(char ch0, char ch1, char ch2, char ch3)
            => (D3DFormat)MakeFourCC((byte)ch0, (byte)ch1, (byte)ch2, (byte)ch3);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint MakeFourCC(byte ch0, byte ch1, byte ch2, byte ch3)
            => ch0 |
               ((uint)ch1 << 8) |
               ((uint)ch2 << 16) |
               ((uint)ch3 << 24);
    }
}
