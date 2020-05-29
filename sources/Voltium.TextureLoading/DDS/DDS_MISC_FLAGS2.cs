using System;
using System.Diagnostics.CodeAnalysis;

namespace Voltium.TextureLoading.DDS
{
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal enum DDS_MISC_FLAGS2 : uint
    {
        DDS_MISC_FLAGS2_ALPHA_MODE_MASK = 0x7,
    };
}