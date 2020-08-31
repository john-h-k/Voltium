using System;
using System.Runtime.CompilerServices;
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

        /// <summary>
        /// The optional geometry shader for the pipeline
        /// </summary>
        public CompiledShader GeometryShader;

        /// <summary>
        /// The optional domain shader for the pipeline
        /// </summary>
        public CompiledShader DomainShader;

        /// <summary>
        /// The optional hull shader for the pipeline
        /// </summary>
        public CompiledShader HullShader;

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

        /* public TODO: MULTI-GPU */
        //internal uint NodeMask;

        //public uint SampleMask;  do we need to expose this
    }
}
