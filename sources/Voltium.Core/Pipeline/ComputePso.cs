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
    /// A <see cref="PipelineStateObject"/> for a compute pipeline
    /// </summary>
    public unsafe sealed class ComputePso : PipelineStateObject
    {
        /// <summary>
        /// The <see cref="ComputePipelineDesc"/> for this pipeline
        /// </summary>
        public readonly ComputePipelineDesc Desc;

        internal override unsafe ID3D12RootSignature* GetRootSig()
            => Desc.ShaderSignature.Value;

        internal ComputePso(ComPtr<ID3D12PipelineState> pso, in ComputePipelineDesc desc) : base(pso)
        {
            Desc = desc;
        }
    }
}
