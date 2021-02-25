using System;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Memory;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// A pipeline state object
    /// </summary>
    public unsafe abstract class PipelineStateObject : IDisposable, IInternalGraphicsObject<PipelineStateObject>
    {
        internal PipelineHandle Handle;
        private Disposal<PipelineHandle> _dispose;

        public ReadOnlyMemory<RootParameter> RootParameters { get; }
        public ReadOnlyMemory<StaticSampler> StaticSamplers { get;  }

        /// <inheritdoc/>
        public void Dispose()
        {
            _dispose.Dispose(ref Handle);
        }

        TypedHandle<PipelineStateObject> IInternalGraphicsObject<PipelineStateObject>.GetPointer() => Pointer.Ptr;

#if TRACE_DISPOSABLES || DEBUG
        /// <summary>
        /// no :)
        /// </summary>
        ~PipelineStateObject()
        {
            Guard.MarkDisposableFinalizerEntered();
        }
#endif
    }
}
