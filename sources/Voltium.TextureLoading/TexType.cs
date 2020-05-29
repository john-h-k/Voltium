namespace Voltium.TextureLoading
{
    /// <summary>
    /// Defines the type of a texture, or <see cref="RuntimeDetect"/> to indicate consumers should attempt to detect the type
    /// themselves
    /// </summary>
    public enum TexType
    {
        /// <summary>
        /// Indicates consumers should attempt to detect the type of the texture from metadata. This
        /// may fail
        /// </summary>
        RuntimeDetect,

        /// <summary>
        /// A DirectDrawSurface (.dds) texture, with many different formats, an optional alpha channel,
        /// and built in MIPMAP support, as well as GPU supported compression formats
        /// </summary>
        DirectDrawSurface,

        /// <inheritdoc cref="DirectDrawSurface"/>
        Dds = DirectDrawSurface,

        /// <summary>
        /// An uncompressed RGB texture bitmap (.bmp)
        /// </summary>
        Bitmap,

        /// <inheritdoc cref="Bmp"/>
        Bmp = Bitmap,

        /// <summary>
        /// A TruevisionGraphicsAdapter (.tga) texture, with RGB and an alpha channel
        /// </summary>
        TruevisionGraphicsAdapter,

        /// <inheritdoc cref="TruevisionGraphicsAdapter"/>
        Tga = TruevisionGraphicsAdapter,

        /// <summary>
        /// A PortableNetworkGraphics (.PNG) file, that is compressed and contains an RGB and alpha channel
        /// </summary>
        PortableNetworkGraphics,

        /// <inheritdoc cref="PortableNetworkGraphics"/>
        Png = PortableNetworkGraphics
    }
}
