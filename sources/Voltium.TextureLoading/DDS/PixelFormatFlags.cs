using System;
using System.Diagnostics.CodeAnalysis;

namespace Voltium.TextureLoading.DDS
{
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal enum PixelFormatFlags : uint
    {
        DDS_FOURCC = 0x00000004,
        DDS_RGB = 0x00000040,
        DDS_LUMINANCE = 0x00020000,
        DDS_ALPHA = 0x00000002
    }
}
