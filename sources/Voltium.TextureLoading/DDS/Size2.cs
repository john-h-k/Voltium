namespace Voltium.TextureLoading.DDS
{
    internal struct Size2
    {
        public Size2(uint height, uint width)
        {
            Height = height;
            Width = width;
        }

        public void Deconstruct(out uint height, out uint width)
        {
            height = Height;
            width = Width;
        }

        public uint Height { get; set; }
        public uint Width { get; set; }
    }
}