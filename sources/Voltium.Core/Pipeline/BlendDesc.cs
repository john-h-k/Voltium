using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// Describes the blend state of the GPU pipeline
    /// </summary>
    public readonly struct BlendDesc
    {
        /// <summary>
        /// Whether to use alpha-to-coverage AA
        /// </summary>
        public readonly bool UseAlphaToCoverage;

        /// <summary>
        /// Whether each render target should use a seperate <see cref="RenderTargetBlendDesc"/>.
        /// If this is <see langword="false"/>, all render targets will use the first <see cref="RenderTargetBlendDesc"/>
        /// </summary>
        public readonly bool UseIndependentBlend;

        /// <summary>
        /// The render target blend descriptions
        /// </summary>
        public readonly RenderTargetBlendDescBuffer8 RenderTargetBlendDescs;

        /// <summary>
        /// A buffer of 8 <see cref="RenderTargetBlendDesc"/>s
        /// </summary>
        public struct RenderTargetBlendDescBuffer8
        {
            /// <summary>
            /// Retrieves a <see cref="RenderTargetBlendDesc"/> by index
            /// </summary>
            /// <param name="index"></param>
            public unsafe readonly ref readonly RenderTargetBlendDesc this[int index]
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
            public readonly RenderTargetBlendDesc RenderTarget0;
            public readonly RenderTargetBlendDesc RenderTarget1;
            public readonly RenderTargetBlendDesc RenderTarget2;
            public readonly RenderTargetBlendDesc RenderTarget3;
            public readonly RenderTargetBlendDesc RenderTarget4;
            public readonly RenderTargetBlendDesc RenderTarget5;
            public readonly RenderTargetBlendDesc RenderTarget6;
            public readonly RenderTargetBlendDesc RenderTarget7;
            public readonly RenderTargetBlendDesc RenderTarget8;
#pragma warning restore 1591
        }
    }
}
