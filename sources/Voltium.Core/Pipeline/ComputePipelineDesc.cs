using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Core.Devices;

namespace Voltium.Core.Pipeline
{

    /// <summary>
    /// Describes the state and settings of a compute pipeline
    /// </summary>
    public partial struct ComputePipelineDesc : IPipelineStreamType
    {
        /// <summary>
        /// Creates a new <see cref="ComputePipelineDesc"/>
        /// </summary>
        public ComputePipelineDesc(RootSignature shaderSignature, CompiledShader computeShader)
        {
            Unsafe.SkipInit(out this);
            RootSignature = shaderSignature;
            ComputeShader = computeShader;
        }

        /// <summary>
        /// The compute shader for the pipeline
        /// </summary>
        public CompiledShader ComputeShader;

        /// <summary>
        /// 
        /// </summary>
        public RootSignatureElement RootSignature;

        // public uint NodeMask { get; set; } TODO: MULTI-GPU

        // we could have a pipeline flags thing, but that is just used for WARP debugging. do i really need to support it
    }


    /// <summary>
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct RootSignatureElement : IPipelineStreamElement<RootSignatureElement>
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public void _Initialize() => Type.Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_ROOT_SIGNATURE;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        [FieldOffset(0)]
        internal AlignedSubobjectType<nuint> Type;

        [FieldOffset(0)]
        private nuint _Pad;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rootSignature"></param>
        public static implicit operator RootSignatureElement(RootSignature rootSignature) => new RootSignatureElement() { RootSignature = rootSignature };

        /// <summary>
        /// The root signature for the pipeline
        /// </summary>
        public unsafe RootSignature RootSignature { get => RootSignature.GetRootSig((ID3D12RootSignature*)Type.Inner); set => Type.Inner = (nuint)value.Value; }

    }
}
