namespace Voltium.Core.Memory
{
    /// <summary>
    /// 
    /// </summary>
    public struct TextureClearValue
    {
        /// <summary>
        /// If used with a render target, the <see cref="Rgba128"/> to optimise clearing for
        /// </summary>
        public Rgba128 Color { get; set; }

        /// <summary>
        /// If used with a depth target, the <see cref="float"/> to optimise clearing depth for
        /// </summary>
        public float Depth { get; set; }

        /// <summary>
        /// If used with a depth target, the <see cref="byte"/> to optimise clearing stencil for
        /// </summary>
        public byte Stencil { get; set; }

        /// <summary>
        /// Creates a new <see cref="TextureClearValue"/> for a render target
        /// </summary>
        /// <param name="color">The <see cref="Rgba128"/> to optimise clearing for</param>
        /// <returns>A new <see cref="TextureClearValue"/></returns>
        public static TextureClearValue CreateForRenderTarget(Rgba128 color) => new TextureClearValue { Color = color };

        /// <summary>
        /// Creates a new <see cref="TextureClearValue"/> for a render target
        /// </summary>
        /// <param name="depth">The <see cref="float"/> to optimise clearing depth for</param>
        /// <param name="stencil">The <see cref="byte"/> to optimise clearing stencil for</param>
        /// <returns>A new <see cref="TextureClearValue"/></returns>
        public static TextureClearValue CreateForDepthStencil(float depth, byte stencil) => new TextureClearValue { Depth = depth, Stencil = stencil };
    }
}
