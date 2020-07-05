using Voltium.Common;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// Describes the blend state of the GPU pipeline
    /// </summary>
    public struct BlendDesc
    {
        /// <summary>
        /// The default <see cref="BlendDesc"/>. This correspends
        /// to <c>new CD3DX12_BLEND_DESC(DEFAULT)</c>
        /// </summary>
        public static BlendDesc Default { get; } = new BlendDesc
        {
            UseAlphaToCoverage = false,
            UseIndependentBlend = false,
            RenderTargetBlendDescs = new()
            {
                [0] = RenderTargetBlendDesc.Default
            }
        };


        /// <summary>
        /// Whether to use alpha-to-coverage AA
        /// </summary>
        public bool UseAlphaToCoverage;

        /// <summary>
        /// Whether each render target should use a seperate <see cref="RenderTargetBlendDesc"/>.
        /// If this is <see langword="false"/>, all render targets will use the first <see cref="RenderTargetBlendDesc"/>
        /// </summary>
        public bool UseIndependentBlend;

        /// <summary>
        /// The render target blend descriptions
        /// </summary>
        public RenderTargetBlendDescBuffer8 RenderTargetBlendDescs;

        /// <summary>
        /// A buffer of 8 <see cref="RenderTargetBlendDesc"/>s
        /// </summary>
        public struct RenderTargetBlendDescBuffer8
        {
            /// <summary>
            /// Retrieves a <see cref="RenderTargetBlendDesc"/> by index
            /// </summary>
            /// <param name="index"></param>
            public unsafe ref RenderTargetBlendDesc this[int index]
            {
                get
                {
                    Guard.Positive(index);
                    fixed (RenderTargetBlendDesc* p0 = &RenderTarget0)
                    {
                        return ref *(p0 + index);
                    }
                }
            }

#pragma warning disable 1591 // XML docs
            public RenderTargetBlendDesc RenderTarget0;
            public RenderTargetBlendDesc RenderTarget1;
            public RenderTargetBlendDesc RenderTarget2;
            public RenderTargetBlendDesc RenderTarget3;
            public RenderTargetBlendDesc RenderTarget4;
            public RenderTargetBlendDesc RenderTarget5;
            public RenderTargetBlendDesc RenderTarget6;
            public RenderTargetBlendDesc RenderTarget7;
            public RenderTargetBlendDesc RenderTarget8;
#pragma warning restore 1591
        }
    }
}
