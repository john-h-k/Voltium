using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Managers;
using Voltium.Core.Managers.Shaders;

namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// Describes the state and settings of a graphics pipeline
    /// </summary>
    public struct GraphicsPipelineDesc
    {
        /// <summary>
        /// The root signature for the pipeline
        /// </summary>
        public RootSignature ShaderSignature;

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
        public ReadOnlyMemory<ShaderInput> Inputs;

        /// <summary>
        /// The MSAA (multi-sample anti-aliasing) settings for the
        /// pipeline
        /// </summary>
        public MsaaDesc Msaa;

        /// <summary>
        /// The number of render targets used by this pipeline
        /// </summary>
        public uint NumRenderTargets;

        /// <summary>
        /// The <see cref="TopologyClass"/> for this type 
        /// </summary>
        public TopologyClass Topology;

        /// <summary>
        /// The formats of the render targets used
        /// </summary>
        public FormatBuffer8 RenderTargetFormats;

        /// <summary>
        /// The format of the depth stencil
        /// </summary>
        public DXGI_FORMAT DepthStencilFormat;

        /* public TODO: MULTI-GPU */
        internal uint NodeMask;

        //public uint SampleMask;  do we need to expose this

        /// <summary>
        /// A buffer of 8 <see cref="DXGI_FORMAT"/>s
        /// </summary>
        public unsafe struct FormatBuffer8
        {
            private fixed uint _formats[8];

            /// <summary>
            /// Retrieves the <see cref="DXGI_FORMAT"/> for a given index
            /// </summary>
            public DXGI_FORMAT this[int index]
            {
                get
                {
                    Guard.InRangeInclusive(index, 0, 7);
                    return (DXGI_FORMAT)_formats[index];
                }
                set => _formats[index] = (uint)value;
            }
        }
    }
}
