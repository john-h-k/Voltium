using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Core.Devices;

namespace Voltium.Core.Pipeline
{

    /// <summary>
    /// Describes the state and settings of a compute pipeline
    /// </summary>
    public unsafe partial class ComputePipelineDesc
    {
        /// <summary>
        /// Creates a new <see cref="ComputePipelineDesc"/>
        /// </summary>
        public ComputePipelineDesc()
        {
            Desc.RootSig.Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_ROOT_SIGNATURE;

            Desc.Compute = new(null, 0, ShaderType.Compute);
        }

        private _PsoDesc Desc;


        internal ref byte GetPinnableReference() => ref Unsafe.As<_PsoDesc, byte>(ref Desc);
        internal nuint DescSize => (nuint)sizeof(_PsoDesc);

        private struct _PsoDesc
        {
            public _RootSig RootSig;

            public CompiledShader Compute;


            public struct _RootSig
            {
                public D3D12_PIPELINE_STATE_SUBOBJECT_TYPE Type;
                public ID3D12RootSignature* Pointer;
            }
        }

        /// <summary>
        /// The root signature for the pipeline
        /// </summary>
        public RootSignature? RootSignature { get => RootSignature.GetRootSig(Desc.RootSig.Pointer); set => Desc.RootSig.Pointer = value is null ? null : value.Value; }

        /// <summary>
        /// The compute shader for the pipeline
        /// </summary>
        public ref CompiledShader ComputeShader => ref Desc.Compute;

        // public uint NodeMask { get; set; } TODO: MULTI-GPU
    }
}
