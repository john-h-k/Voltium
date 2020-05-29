using System.Runtime.CompilerServices;

namespace Voltium.TextureLoading.DDS
{
    internal struct Size3
    {
        public Size3(uint height, uint width, uint depth)
        {
            Height = height;
            Width = width;
            Depth = depth;
        }

        public void Deconstruct(out uint height, out uint width, out uint depth)
        {
            height = Height;
            width = Width;
            depth = Depth;
        }

        public static explicit operator Size2(Size3 size) => Unsafe.As<Size3, Size2>(ref size);

        public uint Height { get; set; }
        public uint Width { get; set; }
        public uint Depth { get; set; }
    }
}