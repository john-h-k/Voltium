using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Devices;
using Voltium.Core.Devices.Shaders;
using Voltium.Core.Memory;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// Describes the state and settings of a graphics pipeline
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe class GraphicsPipelineDesc
    {

        [StructLayout(LayoutKind.Sequential)]
        internal struct _PsoDesc
        {
            public _RootSig RootSig;

            public CompiledShader Vertex, Pixel, Hull, Domain, Geometry;

            public RasterizerDesc Rasterizer;
            public BlendDesc Blend;
            public DepthStencilDesc DepthStencil;
            public InputLayout Inputs;
            public TopologyClass Topology;
            public RenderTargetFormats RenderTargetFormats;
            public DepthStencilFormat DepthStencilFormat;
            public _Msaa Msaa;


            public struct _RootSig
            {
                public D3D12_PIPELINE_STATE_SUBOBJECT_TYPE Type;
                public ID3D12RootSignature* Pointer;
            }

            [StructLayout(LayoutKind.Explicit)]
            public struct _Msaa
            {
                [FieldOffset(0)]
                public AlignedSubobjectType<MsaaDesc> Type;

                [FieldOffset(0)]
                public nuint _Align;
            }

        }

        /// <summary>
        /// Constructs a default instance of <see cref="GraphicsPipelineDesc"/>
        /// </summary>
        public GraphicsPipelineDesc()
        {
            Desc.Vertex = new(null, 0, ShaderType.Vertex);
            Desc.Pixel = new(null, 0, ShaderType.Pixel);
            Desc.Hull = new(null, 0, ShaderType.Hull);
            Desc.Domain = new(null, 0, ShaderType.Domain);
            Desc.Geometry = new(null, 0, ShaderType.Geometry);

            Desc.Rasterizer = RasterizerDesc.Default;
            Desc.Blend = BlendDesc.Default;
            Desc.DepthStencil = DepthStencilDesc.Default;
            Desc.Msaa.Type.Inner = MsaaDesc.None;

            SetMarkers(null);
        }

        internal void SetMarkers(ComputeDevice? device)
        {
            if (RootSignature is null && device is not null)
            {
                RootSignature = device.EmptyRootSignature;
            }

            Desc.RootSig.Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_ROOT_SIGNATURE;

            FixupShader(ref VertexShader, ShaderType.Vertex);
            FixupShader(ref PixelShader, ShaderType.Pixel);
            FixupShader(ref HullShader, ShaderType.Hull);
            FixupShader(ref DomainShader, ShaderType.Domain);
            FixupShader(ref GeometryShader, ShaderType.Geometry);

            static void FixupShader(ref CompiledShader shader, ShaderType fixup)
            {
                if (!shader.Type.IsValid())
                {
                    shader = new(null, 0, fixup);
                }
            }

            uint i = 0;
            for (; i < RenderTargetFormats.FormatBuffer8.BufferLength && RenderTargetFormats[i] != DataFormat.Unknown; i++)
            {

            }

            Desc.RenderTargetFormats.Type.Inner.NumFormats = i;

            Desc.Rasterizer.Type.Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_RASTERIZER;
            Desc.Blend.Type.Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_BLEND;
            Desc.DepthStencil.Type.Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_DEPTH_STENCIL1;
            Desc.Inputs.Type.Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_INPUT_LAYOUT;
            Desc.Topology.Type.Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_PRIMITIVE_TOPOLOGY;
            Desc.RenderTargetFormats.Type.Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_RENDER_TARGET_FORMATS;
            Desc.DepthStencilFormat.Type.Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_DEPTH_STENCIL_FORMAT;
            Desc.Msaa.Type.Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_SAMPLE_DESC;
        }

        internal ref byte GetPinnableReference() => ref Unsafe.As<_PsoDesc, byte>(ref Desc);
        internal nuint DescSize => (nuint)sizeof(_PsoDesc);

        internal RootSignatureHandle Sig;
        internal _PsoDesc Desc;

        /// <summary>
        /// The root signature for the pipeline
        /// </summary>
        public RootSignature? RootSignature { get => RootSignature.GetRootSig(Desc.RootSig.Pointer); set => Desc.RootSig.Pointer = value is null ? null : value.Value; }

        /// <summary>
        /// The optional vertex shader for the pipeline
        /// </summary>
        public ref CompiledShader VertexShader => ref Desc.Vertex;

        /// <summary>
        /// The optional pixel shader for the pipeline
        /// </summary>
        public ref CompiledShader PixelShader => ref Desc.Pixel;

        /// <summary>
        /// The optional geometry shader for the pipeline
        /// </summary>
        public ref CompiledShader GeometryShader => ref Desc.Geometry;

        /// <summary>
        /// The optional domain shader for the pipeline
        /// </summary>
        public ref CompiledShader DomainShader => ref Desc.Domain;

        /// <summary>
        /// The optional hull shader for the pipeline
        /// </summary>
        public ref CompiledShader HullShader => ref Desc.Hull;

        /// <summary>
        /// The blend settings for the pipeline
        /// </summary>
        public ref BlendDesc Blend => ref Desc.Blend;

        /// <summary>
        /// The rasterizer settings for the pipeline
        /// </summary>
        public ref RasterizerDesc Rasterizer => ref Desc.Rasterizer;

        /// <summary>
        /// The depth stencil settings for the pipeline
        /// </summary>
        public ref DepthStencilDesc DepthStencil => ref Desc.DepthStencil;

        /// <summary>
        /// The inputs to the input-assembler stage of the pipeline
        /// </summary>
        public ref InputLayout Inputs => ref Desc.Inputs;

        /// <summary>
        /// The <see cref="TopologyClass"/> for this type 
        /// </summary>
        public ref TopologyClass Topology => ref Desc.Topology;

        /// <summary>
        /// The formats of the render targets used
        /// </summary>
        public ref RenderTargetFormats RenderTargetFormats => ref Desc.RenderTargetFormats;

        /// <summary>
        /// The format of the depth stencil
        /// </summary>
        public ref DataFormat DepthStencilFormat => ref Desc.DepthStencilFormat.Type.Inner;

        /// <summary>
        /// The <see cref="MsaaDesc"/>
        /// </summary>
        public ref MsaaDesc Msaa => ref Desc.Msaa.Type.Inner;

        //public RenderPass RenderPass { get; set; }

        /* public TODO: MULTI-GPU */
        //internal uint NodeMask;

        //public uint SampleMask;  do we need to expose this
    }

    /// <summary>
    /// Describes the state and settings of a graphics pipeline
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe class MeshPipelineDesc
    {

        private struct _PsoDesc
        {
            public _RootSig RootSig;

            public CompiledShader Mesh, Amplification, Pixel;

            public RasterizerDesc Rasterizer;
            public BlendDesc Blend;
            public DepthStencilDesc DepthStencil;
            public InputLayout Inputs;
            public TopologyClass Topology;
            public RenderTargetFormats RenderTargetFormats;
            public DepthStencilFormat DepthStencilFormat;
            public _Msaa Msaa;


            public struct _RootSig
            {
                public D3D12_PIPELINE_STATE_SUBOBJECT_TYPE Type;
                public ID3D12RootSignature* Pointer;
            }

            [StructLayout(LayoutKind.Explicit)]
            public struct _Msaa
            {
                [FieldOffset(0)]
                public AlignedSubobjectType<MsaaDesc> Type;

                [FieldOffset(0)]
                public nuint _Align;
            }

        }

        /// <summary>
        /// Constructs a default instance of <see cref="GraphicsPipelineDesc"/>
        /// </summary>
        public MeshPipelineDesc()
        {
            Desc.RootSig.Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_ROOT_SIGNATURE;

            Desc.Mesh = new(null, 0, ShaderType.Mesh);
            Desc.Amplification = new(null, 0, ShaderType.Amplification);
            Desc.Pixel = new(null, 0, ShaderType.Pixel);

            Desc.Rasterizer = RasterizerDesc.Default;
            Desc.Blend = BlendDesc.Default;
            Desc.DepthStencil = DepthStencilDesc.Default;
            Desc.Msaa.Type.Inner = MsaaDesc.None;

            Desc.Rasterizer.Type.Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_RASTERIZER;
            Desc.Blend.Type.Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_BLEND;
            Desc.DepthStencil.Type.Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_DEPTH_STENCIL1;
            Desc.Inputs.Type.Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_INPUT_LAYOUT;
            Desc.Topology.Type.Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_PRIMITIVE_TOPOLOGY;
            Desc.RenderTargetFormats.Type.Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_RENDER_TARGET_FORMATS;
            Desc.DepthStencilFormat.Type.Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_DEPTH_STENCIL_FORMAT;
            Desc.Msaa.Type.Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_SAMPLE_DESC;
        }

        internal ref byte GetPinnableReference() => ref Unsafe.As<_PsoDesc, byte>(ref Desc);
        internal nuint DescSize => (nuint)sizeof(_PsoDesc);

        private _PsoDesc Desc;

        /// <summary>
        /// The root signature for the pipeline
        /// </summary>
        public RootSignature? RootSignature { get => RootSignature.GetRootSig(Desc.RootSig.Pointer); set => Desc.RootSig.Pointer = value is null ? null : value.Value; }

        /// <summary>
        /// The optional mesh shader for the pipeline
        /// </summary>
        public ref CompiledShader MeshShader => ref Desc.Mesh;

        /// <summary>
        /// The optional amplification shader for the pipeline
        /// </summary>
        public ref CompiledShader AmplificationShader => ref Desc.Amplification;

        /// <summary>
        /// The optional pixel shader for the pipeline
        /// </summary>
        public ref CompiledShader PixelShader => ref Desc.Pixel;

        /// <summary>
        /// The blend settings for the pipeline
        /// </summary>
        public ref BlendDesc Blend => ref Desc.Blend;

        /// <summary>
        /// The rasterizer settings for the pipeline
        /// </summary>
        public ref RasterizerDesc Rasterizer => ref Desc.Rasterizer;

        /// <summary>
        /// The depth stencil settings for the pipeline
        /// </summary>
        public ref DepthStencilDesc DepthStencil => ref Desc.DepthStencil;

        /// <summary>
        /// The inputs to the input-assembler stage of the pipeline
        /// </summary>
        public ref InputLayout Inputs => ref Desc.Inputs;

        /// <summary>
        /// The <see cref="TopologyClass"/> for this type 
        /// </summary>
        public ref TopologyClass Topology => ref Desc.Topology;

        /// <summary>
        /// The formats of the render targets used
        /// </summary>
        public ref RenderTargetFormats RenderTargetFormats => ref Desc.RenderTargetFormats;

        /// <summary>
        /// The format of the depth stencil
        /// </summary>
        public ref DataFormat DepthStencilFormat => ref Desc.DepthStencilFormat.Type.Inner;

        /// <summary>
        /// The <see cref="MsaaDesc"/>
        /// </summary>
        public ref MsaaDesc Msaa => ref Desc.Msaa.Type.Inner;

        /* public TODO: MULTI-GPU */
        //internal uint NodeMask;

        //public uint SampleMask;  do we need to expose this
    }
}
