

// ReSharper disable IdentifierTypo
#pragma warning disable 649

// ReSharper disable CommentTypo

namespace Voltium.TextureLoading.DDS
{
    internal struct DDSPixelFormat
    {
        public uint Size;
        public PixelFormatFlags Flags;
        public D3DFormat FourCC;
        public uint RgbBitCount;
        public uint RBitMask;
        public uint GBitMask;
        public uint BBitMask;
        public uint ABitMask;
    }
}
