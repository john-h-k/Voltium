using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// A <see cref="PipelineStateObject"/> for a grpahics pipeline
    /// </summary>
    public unsafe sealed class RayTracingPipelineStateObject : PipelineStateObject
    {
        /// <summary>
        /// The <see cref="RayTracingPipelineDesc"/> for this pipeline
        /// </summary>
        public readonly RayTracingPipelineDesc Desc;

        internal override unsafe ID3D12RootSignature* GetRootSig()
            => Desc.GlobalRootSignature.Value;

        internal RayTracingPipelineStateObject(UniqueComPtr<ID3D12StateObject> pso, RayTracingPipelineDesc desc) : base(pso.As<ID3D12Object>())
        {
            Desc = desc;
        }
    }
}
