namespace Voltium.TextureLoading.TGA
{
    /// <summary>
    /// The type code representing the format of the TGA texture
    /// </summary>
    public enum TgaTypeCode : byte
    {
        /// <summary>
        /// No image data included
        /// </summary>
        None = 0,

        /// <summary>
        /// Uncompressed, color-mapped images
        /// </summary>
        ColorMapped = 1,

        /// <summary>
        /// Uncompressed, RGB images
        /// </summary>
        Rgb = 2,

        /// <summary>
        /// Uncompressed, black and white images
        /// </summary>
        BlackAndWhite = 3,

        /// <summary>
        /// Run Length encoded color-mapped images
        /// </summary>
        RunLengthColourMap = 9,

        /// <summary>
        /// Run Length encoded RGB images
        /// </summary>
        RunLengthRgb = 10,

        /// <summary>
        /// Compressed, black and white images
        /// </summary>
        CompressedBlackAndWhite = 11,

        /// <summary>
        /// Compressed color-mapped data, using Huffman, Delta, and Run Length encoding
        /// </summary>
        CompressedColourMap = 32,

        /// <summary>
        /// Compressed color-mapped data, using Huffman, Delta, and Run Length encoding.  4-pass quad tree-type process.
        /// </summary>
        CompressedColourMapQuadTree = 33
    }
}
