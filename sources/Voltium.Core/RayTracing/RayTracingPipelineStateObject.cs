using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// A <see cref="PipelineStateObject"/> for a raytracing pipeline
    /// </summary>
    public unsafe sealed class RaytracingPipelineStateObject : PipelineStateObject
    {
        /// <summary>
        /// The <see cref="RaytracingPipelineDesc"/> for this pipeline
        /// </summary>
        public readonly RaytracingPipelineDesc Desc;

        internal override unsafe ID3D12RootSignature* GetRootSig()
            => Desc.GlobalRootSignature is null ? null : Desc.GlobalRootSignature.Value;

        internal RaytracingPipelineStateObject(UniqueComPtr<ID3D12StateObject> pso, RaytracingPipelineDesc desc) : base(pso.As<ID3D12Object>())
        {
            Desc = desc;
        }
    }
}
