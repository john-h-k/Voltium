using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Core.Devices;
using Voltium.Core.Memory;

namespace Voltium.Core.Pipeline
{

    /// <summary>
    /// Describes the state and settings of a compute pipeline
    /// </summary>
    public unsafe partial struct ComputePipelineDesc
    {
        /// <summary>
        /// The compute shader for the pipeline
        /// </summary>
        public CompiledShader ComputeShader;

        public uint NodeMask;
    }
}
