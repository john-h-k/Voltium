using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Pipeline
{
    //public readonly struct BlendDesc
    //{
    //    public readonly bool UseAlphaToCoverage;
    //    public readonly bool UseIndependentBlend;

    //    public readonly RenderTargetBlendDescBuffer8 RenderTargetBlendDescs;

    //    /// <summary>
    //    /// A buffer of 8 <see cref="RenderTargetBlendDesc"/>s
    //    /// </summary>
    //    public struct RenderTargetBlendDescBuffer8
    //    {
    //        /// <summary>
    //        /// Retrieves a <see cref="RenderTargetBlendDesc"/> by index
    //        /// </summary>
    //        /// <param name="index"></param>
    //        /// <returns></returns>
    //        public unsafe readonly ref readonly RenderTargetBlendDesc this[int index]
    //        {
    //            get
    //            {
    //                Guard.Positive(index);
    //                fixed (RenderTargetBlendDesc* p0 = &RenderTarget0)
    //                {
    //                    return ref *(p0 + index);
    //                }
    //            }
    //        }

    //        public readonly RenderTargetBlendDesc RenderTarget0;
    //        public readonly RenderTargetBlendDesc RenderTarget1;
    //        public readonly RenderTargetBlendDesc RenderTarget2;
    //        public readonly RenderTargetBlendDesc RenderTarget3;
    //        public readonly RenderTargetBlendDesc RenderTarget4;
    //        public readonly RenderTargetBlendDesc RenderTarget5;
    //        public readonly RenderTargetBlendDesc RenderTarget6;
    //        public readonly RenderTargetBlendDesc RenderTarget7;
    //        public readonly RenderTargetBlendDesc RenderTarget8;
    //    }
    //}
}
