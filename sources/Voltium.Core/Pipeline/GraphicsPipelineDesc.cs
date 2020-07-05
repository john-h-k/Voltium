using System;
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
        public RootSignature RootSignature;

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
        public BlendDesc? Blend;

        /// <summary>
        /// The rasterizer settings for the pipeline
        /// </summary>
        public RasterizerDesc? Rasterizer;

        /// <summary>
        /// The depth stencil settings for the pipeline
        /// </summary>
        public DepthStencilDesc? DepthStencil;

        /// <summary>
        /// The inputs to the input-assembler stage of the pipeline
        /// </summary>
        public ReadOnlyMemory<ShaderInput> Inputs;

        /// <summary>
        /// The MSAA (multi-sample anti-aliasing) settings for the
        /// pipeline
        /// </summary>
        public MultisamplingDesc? Msaa;

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
        public DataFormat DepthStencilFormat;

        /* public TODO: MULTI-GPU */
        internal uint NodeMask;

        //public uint SampleMask;  do we need to expose this

        /// <summary>
        /// A buffer of 8 <see cref="DataFormat"/>s
        /// </summary>
        public unsafe struct FormatBuffer8
        {
            private fixed uint _formats[8];

            /// <summary>
            /// Creates a new <see cref="FormatBuffer8"/>
            /// </summary>
            public FormatBuffer8(
                DataFormat e0 = DataFormat.Unknown,
                DataFormat e1 = DataFormat.Unknown,
                DataFormat e2 = DataFormat.Unknown,
                DataFormat e3 = DataFormat.Unknown,
                DataFormat e4 = DataFormat.Unknown,
                DataFormat e5 = DataFormat.Unknown,
                DataFormat e6 = DataFormat.Unknown,
                DataFormat e7 = DataFormat.Unknown
            )
            {
                this[0] = e0;
                this[1] = e1;
                this[2] = e2;
                this[3] = e3;
                this[4] = e4;
                this[5] = e5;
                this[6] = e6;
                this[7] = e7;
            }

            /// <summary>
            /// Retrieves the <see cref="DataFormat"/> for a given index
            /// </summary>
            public DataFormat this[int index]
            {
                get
                {
                    Guard.InRangeInclusive(index, 0, 7);
                    return (DataFormat)_formats[index];
                }
                set => _formats[index] = (uint)value;
            }
        }
    }
}
