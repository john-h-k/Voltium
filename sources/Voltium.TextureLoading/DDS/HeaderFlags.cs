using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable IdentifierTypo
// ReSharper disable UnusedMember.Global

namespace Voltium.TextureLoading.DDS
{
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal enum HeaderFlags : uint
    {
        DDS_CAPS = 0x1,
        DDS_HEIGHT = 0x2,
        DDS_WIDTH = 0x4,
        DDS_PITCH = 0x8,
        DDS_PIXELFORMAT = 0x1000,
        DDS_MIPMAPCOUNT = 0x20000,
        DDS_LINEARSIZE = 0x80000,
        DDS_DEPTH = 0x800000,
        DDS_HEADER_FLAGS_VOLUME = 0x00800000,

    }
}
