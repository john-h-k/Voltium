using System;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// A pipeline state object
    /// </summary>
    public unsafe abstract class PipelineStateObject : IDisposable
    {
        internal UniqueComPtr<ID3D12Object> Pointer;

        internal virtual ID3D12RootSignature* GetRootSig() => null;

        internal PipelineStateObject(UniqueComPtr<ID3D12Object> pso)
        {
            Pointer = pso.Move();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Pointer.Dispose();
        }

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
