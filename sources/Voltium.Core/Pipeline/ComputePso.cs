using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// A <see cref="PipelineStateObject"/> for a compute pipeline
    /// </summary>
    public unsafe sealed class ComputePipelineStateObject : PipelineStateObject
    {
        /// <summary>
        /// The <see cref="ComputePipelineDesc"/> for this pipeline
        /// </summary>
        public readonly ComputePipelineDesc Desc;

        internal override unsafe ID3D12RootSignature* GetRootSig()
            => Desc.ShaderSignature.Value;

        internal ComputePipelineStateObject(ComPtr<ID3D12PipelineState> pso, in ComputePipelineDesc desc) : base(pso)
        {
            Desc = desc;
        }
    }
}
