#pragma warning disable 649
namespace Voltium.TextureLoading.TGA
{
    internal struct TGAHeader
    {
        public byte IdLength;
        public byte ColourMapType;
        public TgaTypeCode DataTypeCode;
        public short ColourMapOrigin;
        public short ColourMapLength;
        public byte ColourMapDepth;
        public short XOrigin;
        public short YOrigin;
        public short Width;
        public short Height;
        public byte BitsPerPixel;
        public byte ImageDescriptor;
    }

    internal static class TGAHeaderExtensions
    {
        public static bool IsValidTypeCode(in TGAHeader header)
        {
            TgaTypeCode code = header.DataTypeCode;
            return code == TgaTypeCode.None ||
                   code == TgaTypeCode.Rgb ||
                   code == TgaTypeCode.ColorMapped ||
                   code == TgaTypeCode.BlackAndWhite ||
                   code == TgaTypeCode.CompressedColourMap ||
                   code == TgaTypeCode.RunLengthRgb ||
                   code == TgaTypeCode.CompressedBlackAndWhite ||
                   code == TgaTypeCode.RunLengthColourMap ||
                   code == TgaTypeCode.CompressedColourMapQuadTree;
        }

        public static bool IsValidBitsPerPixel(in TGAHeader header)
        {
            byte bits = header.BitsPerPixel;
            return bits == 16 || bits == 24 || bits == 32;
        }
    }
}
