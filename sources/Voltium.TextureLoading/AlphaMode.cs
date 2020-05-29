using System.Diagnostics.CodeAnalysis;

namespace Voltium.TextureLoading
{
    /// <summary>
    /// The alpha mode of the DDS texture
    /// </summary>
    public enum AlphaMode
    {
        /// <summary>
        /// This is the default for most DDS files if the specific metadata isn't present,
        /// and it's up to the application to know if it's really something else.
        /// Viewers should assume the alpha channel is intended for 'normal' alpha blending
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// This indicates that the alpha channel if present is assumed to be using 'straight' alpha.
        /// Viewers should use the alpha channel with 'normal' alpha blending
        /// </summary>
        Straight = 1,

        /// <summary>
        /// This indicates the alpha channel if present is premultiplied alpha.
        /// This information is only present if the file is written using the latest version of the "DX10" extended header,
        /// or if the file is BC2/BC3 with the "DXT2"/"DXT4" FourCC which are explicitly stored as premultiplied alpha.
        /// Viewers should use the alpha channel with premultiplied alpha blending
        /// </summary>
        Premultiplied = 2,

        /// <summary>
        /// This indicates that the alpha channel if present is fully opaque for all pixels.
        /// Viewers can assume there is no alpha blending
        /// </summary>
        Opaque = 3,

        /// <summary>
        /// This indicates the alpha channel if present does not contain transparency
        /// (neither straight or premultiplied alpha) and instead is encoding some other channel of information.
        /// Viewers should not use the alpha channel for blending, and should instead view it as a distinct image channel
        /// </summary>
        Custom = 4,
    }
}
