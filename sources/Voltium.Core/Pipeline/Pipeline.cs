using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// A pipeline state object
    /// </summary>
    public unsafe abstract class Pipeline : IDisposable
    {
        private ComPtr<ID3D12PipelineState> _pso;

        internal ID3D12PipelineState* GetPipeline() => _pso.Get();

        /// <summary>
        /// The type of the pipeline
        /// </summary>
        public PipelineType Type { get;  }

        internal Pipeline(ComPtr<ID3D12PipelineState> pso, PipelineType type)
        {
            _pso = pso.Move();
            Type = type;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _pso.Dispose();
        }
    }
}
