using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// Describes the blend state of the GPU pipeline
    /// </summary>
    [Fluent]
    public partial struct BlendDesc
    {
        public RenderTargetBlendDescBuffer8 RenderTargets;

        /// <summary>
        /// The default <see cref="BlendDesc"/>. This correspends
        /// to <c>new CD3DX12_BLEND_DESC(DEFAULT)</c>
        /// </summary>
        public static BlendDesc Default { get; } = new BlendDesc
        {
            UseAlphaToCoverage = false,
            UseIndependentBlend = false,
            [0] = RenderTargetBlendDesc.Default
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
        /// Retrieves a <see cref="RenderTargetBlendDesc"/> by index
        /// </summary>
        /// <param name="index"></param>
        public unsafe ref RenderTargetBlendDesc this[int index] => ref RenderTargets[index];


        [FixedBufferType(typeof(RenderTargetBlendDesc), 8)]
        public partial struct RenderTargetBlendDescBuffer8
        {
        }
    }
}
