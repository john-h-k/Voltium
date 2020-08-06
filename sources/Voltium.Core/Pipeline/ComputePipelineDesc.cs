using Voltium.Core.Devices;

namespace Voltium.Core.Pipeline
{

    /// <summary>
    /// Describes the state and settings of a compute pipeline
    /// </summary>
    public struct ComputePipelineDesc
    {
        /// <summary>
        /// Creates a new <see cref="ComputePipelineDesc"/>
        /// </summary>
        public ComputePipelineDesc(RootSignature shaderSignature, CompiledShader computeShader)
        {
            RootSignature = shaderSignature;
            ComputeShader = computeShader;
        }

        /// <summary>
        /// The root signature for the pipeline
        /// </summary>
        public RootSignature RootSignature { get; set; }

        /// <summary>
        /// The compute shader for the pipeline
        /// </summary>
        public CompiledShader ComputeShader { get; set; }

        // public uint NodeMask { get; set; } TODO: MULTI-GPU

        // we could have a pipeline flags thing, but that is just used for WARP debugging. do i really need to support it
    }
}
