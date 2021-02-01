using System;

namespace Voltium.CubeGame
{
    internal ref struct Span3D<T>
    {
        public readonly int Width, Height, Depth;
        public readonly Span<T> Span;

        public Span3D(Span<T> span, int width, int height, int depth)
        {
            Span = span;
            Width = width;
            Height = height;
            Depth = depth;
        }

        public ref T this[int linear] => ref Span[linear];
        public ref T this[int x, int y, int z] => ref Span[(((z * Height) + y) * Width) + x];
    }

    internal ref struct ReadOnlySpan3D<T>
    {
        public readonly int Width, Height, Depth;
        public readonly ReadOnlySpan<T> Span;

        public ReadOnlySpan3D(ReadOnlySpan<T> span, int width, int height, int depth)
        {
            Span = span;
            Width = width;
            Height = height;
            Depth = depth;
        }

        public ref readonly T this[int linear] => ref Span[linear];
        public ref readonly T this[int x, int y, int z] => ref Span[(((z * Height) + y) * Width) + x];

        public int Linearise(int x, int y, int z) => (((z * Depth) + y) * Width) + x;
    }
}
