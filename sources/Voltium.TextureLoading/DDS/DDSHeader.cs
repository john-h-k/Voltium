#pragma warning disable 649
namespace Voltium.TextureLoading.DDS
{
    internal unsafe struct DDSHeader
    {
        public uint Size;
        public HeaderFlags Flags;
        public uint Height;
        public uint Width;
        public uint PitchOrLinearSize;
        public uint Depth; // only if DDS_HEADER_FLAGS_VOLUME is set in flags
        public uint MipMapCount;
        public fixed uint Reserved1[11];
        public DDSPixelFormat DdsPixelFormat;
        public uint Caps;
        public Caps2Flags Caps2;
        public uint Caps3;
        public uint Caps4;
        public uint Reserved2;
    }
}
