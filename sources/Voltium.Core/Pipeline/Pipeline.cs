using System;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// A pipeline state object
    /// </summary>
    public unsafe class PipelineStateObject : IDisposable
    {
        private ComPtr<ID3D12PipelineState> _pso;

        internal ID3D12PipelineState* GetPso() => _pso.Ptr;
        internal virtual ID3D12RootSignature* GetRootSig() => null;

        internal PipelineStateObject(ComPtr<ID3D12PipelineState> pso)
        {
            _pso = pso.Move();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _pso.Dispose();
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
