using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Devices;
using Voltium.Core.Devices.Shaders;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// Describes the state and settings of a graphics pipeline
    /// </summary>
    public partial struct GraphicsPipelineDesc : IPipelineStreamType
    {
        /// <summary>
        /// The root signature for the pipeline
        /// </summary>
        public RootSignatureElement RootSignature;

        /// <summary>
        /// The optional vertex shader for the pipeline
        /// </summary>
        public CompiledShader VertexShader;

        /// <summary>
        /// The optional pixel shader for the pipeline
        /// </summary>
        public CompiledShader PixelShader;

        ///// <summary>
        ///// The optional geometry shader for the pipeline
        ///// </summary>
        //public CompiledShader GeometryShader;

        ///// <summary>
        ///// The optional domain shader for the pipeline
        ///// </summary>
        //public CompiledShader DomainShader;

        ///// <summary>
        ///// The optional hull shader for the pipeline
        ///// </summary>
        //public CompiledShader HullShader;

        /// <summary>
        /// The blend settings for the pipeline
        /// </summary>
        public BlendDesc Blend;

        /// <summary>
        /// The rasterizer settings for the pipeline
        /// </summary>
        public RasterizerDesc Rasterizer;

        /// <summary>
        /// The depth stencil settings for the pipeline
        /// </summary>
        public DepthStencilDesc DepthStencil;

        /// <summary>
        /// The inputs to the input-assembler stage of the pipeline
        /// </summary>
        public InputLayout Inputs;

        /// <summary>
        /// The <see cref="TopologyClass"/> for this type 
        /// </summary>
        public TopologyClass Topology;

        /// <summary>
        /// The formats of the render targets used
        /// </summary>
        public RenderTargetFormats RenderTargetFormats;

        /// <summary>
        /// The format of the depth stencil
        /// </summary>
        public DepthStencilFormat DepthStencilFormat;

        /// <summary>
        /// The <see cref="MsaaElement"/>
        /// </summary>
        public MsaaElement Msaa;

        /* public TODO: MULTI-GPU */
        //internal uint NodeMask;

        //public uint SampleMask;  do we need to expose this
    }

    /// <summary>
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct MsaaElement : IPipelineStreamElement<MsaaElement>
    {
        [FieldOffset(0)]
        private AlignedSubobjectType<MsaaDesc> Type;

        [FieldOffset(0)]
        private nuint _Pad;

        /// <inheritdoc cref="MsaaDesc.SampleCount"/>
        public uint SampleCount { get => Type.Inner.SampleCount; set => Type.Inner.SampleCount = value; }

        /// <inheritdoc cref="MsaaDesc.QualityLevel"/>
        public uint QualityLevel { get => Type.Inner.QualityLevel; set => Type.Inner.QualityLevel = value; }


        /// <summary>
        /// Creates a <see cref="MsaaElement"/>
        /// </summary>
        /// <param name="desc"></param>
        public MsaaElement(MsaaDesc desc) : this(desc.SampleCount, desc.QualityLevel)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sampleCount"></param>
        /// <param name="qualityLevel"></param>
        public MsaaElement(uint sampleCount, uint qualityLevel)
        {
            Unsafe.SkipInit(out this);
            SampleCount = sampleCount;
            QualityLevel = qualityLevel;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public void _Initialize()
        {
            Type.Type = D3D12_PIPELINE_STATE_SUBOBJECT_TYPE.D3D12_PIPELINE_STATE_SUBOBJECT_TYPE_SAMPLE_DESC;
            if (SampleCount == 0)
            {
                SampleCount = 1;
            }
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
