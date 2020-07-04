using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// A <see cref="PipelineStateObject"/> for a grpahics pipeline
    /// </summary>
    public unsafe sealed class GraphicsPso : PipelineStateObject
    {
        /// <summary>
        /// The <see cref="GraphicsPipelineDesc"/> for this pipeline
        /// </summary>
        public readonly GraphicsPipelineDesc Desc;

        internal override unsafe ID3D12RootSignature* GetRootSig()
            => Desc.RootSignature.Value;
        internal GraphicsPso(ComPtr<ID3D12PipelineState> pso, in GraphicsPipelineDesc desc) : base(pso)
        {
            Desc = desc;
        }
    }
}
