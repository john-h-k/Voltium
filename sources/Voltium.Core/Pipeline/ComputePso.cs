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
            => Desc.RootSignature.Value;

        internal ComputePipelineStateObject(UniqueComPtr<ID3D12PipelineState> pso, in ComputePipelineDesc desc) : base(pso.As<ID3D12Object>())
        {
            Desc = desc;
        }
    }

    /// <summary>
    /// A <see cref="PipelineStateObject"/> for a mesh pipeline
    /// </summary>
    public unsafe sealed class MeshPipelineStateObject : PipelineStateObject
    {
        /// <summary>
        /// The <see cref="ComputePipelineDesc"/> for this pipeline
        /// </summary>
        public readonly MeshPipelineDesc Desc;

        internal override unsafe ID3D12RootSignature* GetRootSig()
            => Desc.RootSignature.Value;

        internal MeshPipelineStateObject(UniqueComPtr<ID3D12PipelineState> pso, in MeshPipelineDesc desc) : base(pso.As<ID3D12Object>())
        {
            Desc = desc;
        }
    }
}
