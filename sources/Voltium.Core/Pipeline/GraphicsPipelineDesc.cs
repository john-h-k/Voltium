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
    public unsafe struct GraphicsPipelineDesc
    {
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
        public Topology Topology;

        /// <summary>
        /// The formats of the render targets used
        /// </summary>
        public RenderTargetFormats RenderTargetFormats;

        /// <summary>
        /// The format of the depth stencil
        /// </summary>
        public DataFormat DepthStencilFormat;

        /// <summary>
        /// The <see cref="MsaaDesc"/>
        /// </summary>
        public MsaaDesc Msaa;

        //public RenderPass RenderPass ;
        public uint NodeMask;
    }

    /// <summary>
    /// Describes the state and settings of a graphics pipeline
    /// </summary>
    public unsafe struct MeshPipelineDesc
    {
        /// <summary>
        /// The optional mesh shader for the pipeline
        /// </summary>
        public CompiledShader MeshShader;

        /// <summary>
        /// The optional amplification shader for the pipeline
        /// </summary>
        public CompiledShader AmplificationShader;

        /// <summary>
        /// The optional pixel shader for the pipeline
        /// </summary>
        public CompiledShader PixelShader;

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
        /// The <see c="TopologyClass"/> for this type 
        /// </summary>
        public TopologyClass Topology;

        /// <summary>
        /// The formats of the render targets used
        /// </summary>
        public RenderTargetFormats RenderTargetFormats;

        /// <summary>
        /// The format of the depth stencil
        /// </summary>
        public DataFormat DepthStencilFormat;

        /// <summary>
        /// The <see c="MsaaDesc"/>
        /// </summary>
        public MsaaDesc Msaa;

        public uint NodeMask;

        //public uint SampleMask;  do we need to expose this
    }
}
