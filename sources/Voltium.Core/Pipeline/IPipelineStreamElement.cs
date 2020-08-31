using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;

namespace Voltium.Core.Pipeline
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface IPipelineStreamType
    {
        /// <summary>
        /// Don't manually invoke this method. It is used to correctly initialize pipeline elements for native code to read them
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        void _Initialize();
    }

    public interface IPipelineStreamElement<TElement> where TElement : unmanaged, IPipelineStreamElement<TElement>
    {
        /// <summary>
        /// Don't manually invoke this method. It is used to correctly initialize pipeline elements for native code to read them
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        void _Initialize();
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
