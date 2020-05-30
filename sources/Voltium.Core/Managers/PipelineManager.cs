using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Common.Strings;
using Voltium.Core.Managers.Shaders;
using Voltium.Core.Pipeline;
using static TerraFX.Interop.D3D12_CONSERVATIVE_RASTERIZATION_MODE;
using static Voltium.Core.Pipeline.GraphicsPipelineDesc;

namespace Voltium.Core.Managers
{
    // TODO: allow serializing to file sonehow
    /// <summary>
    /// In charge of creation, storing, and retrieving pipeline state objects (PSOs)
    /// </summary>
    [ThreadSafe]
    public unsafe sealed class PipelineManager : IDisposable
    {
        //private ComPtr<ID3D12PipelineLibrary> _psoLibrary;
        private Dictionary<string, ComPtr<ID3D12PipelineState>> _psos = new(16, new FastStringComparer());
        private ComPtr<ID3D12Device> _device;

        /// <summary>
        /// Creates a new <see cref="PipelineManager"/>
        /// </summary>
        /// <param name="device">The <see cref="ID3D12Device"/> to be associated with the pipeline states</param>
        public PipelineManager(ComPtr<ID3D12Device> device)
        {
            _device = device.Move();
        }


        /// <summary>
        /// Creates a new named pipeline state object and registers it in the library for retrieval with
        /// <see cref="RetrievePso(string)"/>
        /// </summary>
        /// <param name="name">The name of the pipeline state</param>
        /// <param name="graphicsDesc">The descriptor for the pipeline state</param>
        public void CreatePso<TShaderInput>(string name, GraphicsPipelineDesc graphicsDesc) where TShaderInput : unmanaged, IBindableShaderType
        {
            graphicsDesc.Inputs = ((IBindableShaderType)default(TShaderInput)).GetShaderInputs();
            CreatePso(name, graphicsDesc);
        }


        /// <summary>
        /// Creates a new named pipeline state object and registers it in the library for retrieval with
        /// <see cref="RetrievePso(string)"/>
        /// </summary>
        /// <param name="name">The name of the pipeline state</param>
        /// <param name="graphicsDesc">The descriptor for the pipeline state</param>
        public void CreatePso(string name, in GraphicsPipelineDesc graphicsDesc)
        {
            TranslateGraphicsPipelineDescriptionWithoutShadersOrShaderInputLayoutElements(graphicsDesc, out D3D12_GRAPHICS_PIPELINE_STATE_DESC desc);

            // TODO use pinned pool
            using var buff = RentedArray<D3D12_INPUT_ELEMENT_DESC>.Create(graphicsDesc.Inputs.Length);

            fixed (D3D12_INPUT_ELEMENT_DESC* pDesc = buff.Value)
            fixed (byte* vs = graphicsDesc.VertexShader)
            fixed (byte* ps = graphicsDesc.PixelShader)
            fixed (byte* gs = graphicsDesc.GeometryShader)
            fixed (byte* ds = graphicsDesc.DomainShader)
            fixed (byte* hs = graphicsDesc.HullShader)
            {
                // we must keep this alive until the end of the scope
                var strBuff = TranslateLayouts(graphicsDesc.Inputs, pDesc);

                desc.VS = new D3D12_SHADER_BYTECODE(vs, (uint)graphicsDesc.VertexShader.Length);
                desc.PS = new D3D12_SHADER_BYTECODE(ps, (uint)graphicsDesc.PixelShader.Length);
                desc.GS = new D3D12_SHADER_BYTECODE(gs, (uint)graphicsDesc.GeometryShader.Length);
                desc.DS = new D3D12_SHADER_BYTECODE(ds, (uint)graphicsDesc.DomainShader.Length);
                desc.HS = new D3D12_SHADER_BYTECODE(hs, (uint)graphicsDesc.HullShader.Length);
                desc.InputLayout = new D3D12_INPUT_LAYOUT_DESC { NumElements = (uint)graphicsDesc.Inputs.Length, pInputElementDescs = pDesc };

                using ComPtr<ID3D12PipelineState> pso = default;
                Guard.ThrowIfFailed(_device.Get()->CreateGraphicsPipelineState(
                    &desc,
                    pso.Guid,
                    ComPtr.GetVoidAddressOf(&pso)
                ));

                GC.KeepAlive(strBuff);

                DirectXHelpers.SetObjectName(pso.Get(), $"Pipeline state object '{name}'");

                StorePso(name, pso.Move());
            }
        }

        private byte[] TranslateLayouts(ReadOnlyMemory<ShaderInput> inputs, D3D12_INPUT_ELEMENT_DESC* pDesc)
        {
            var span = inputs.Span;
            // this is SUPER inefficient
            int totalStringLength = 0;
            for (var i = 0; i < span.Length; i++)
            {
                // get the necessary length for strings
                totalStringLength += Encoding.ASCII.GetMaxByteCount(span[i].Name.Length) + /* null char */ 1;
            }

            var asciiArr = GC.AllocateArray<byte>(totalStringLength, pinned: true);

            var asciiBuff = asciiArr.AsSpan();
            for (var i = 0; i < span.Length; i++)
            {
                var elem = span[i];
                var read = Encoding.ASCII.GetBytes(elem.Name, asciiBuff);
                asciiBuff[read++] = 0;

                D3D12_INPUT_ELEMENT_DESC* pCurDesc = &pDesc[i];

                *pCurDesc = default;
                pCurDesc->SemanticName = (sbyte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(asciiBuff));
                pCurDesc->AlignedByteOffset = Windows.D3D12_APPEND_ALIGNED_ELEMENT;
                pCurDesc->Format = (DXGI_FORMAT)elem.Type;
                pCurDesc->InputSlotClass = D3D12_INPUT_CLASSIFICATION.D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA;

                asciiBuff = asciiBuff.Slice(read);
            }

            return asciiArr;
        }

        private unsafe void TranslateGraphicsPipelineDescriptionWithoutShadersOrShaderInputLayoutElements(in GraphicsPipelineDesc inDesc, out D3D12_GRAPHICS_PIPELINE_STATE_DESC outDesc)
        {
            TranslateBlendState(inDesc.Blend, out D3D12_BLEND_DESC blendDesc);
            TranslateRasterizerState(inDesc.Rasterizer, out D3D12_RASTERIZER_DESC rasterizerDesc);
            TranslateDepthStencilState(inDesc.DepthStencil, out D3D12_DEPTH_STENCIL_DESC depthStencilDesc);

            outDesc = new D3D12_GRAPHICS_PIPELINE_STATE_DESC
            {
                pRootSignature = inDesc.ShaderSignature.Value,
                DSVFormat = inDesc.DepthStencilFormat,
                BlendState = blendDesc,
                RasterizerState = rasterizerDesc,
                DepthStencilState = depthStencilDesc,
                SampleMask = 0xFFFFFFFF,
                NumRenderTargets = inDesc.NumRenderTargets,
                SampleDesc = new DXGI_SAMPLE_DESC(inDesc.Msaa.SampleCount, inDesc.Msaa.QualityLevel),
                NodeMask = inDesc.NodeMask,
                PrimitiveTopologyType = (D3D12_PRIMITIVE_TOPOLOGY_TYPE)inDesc.Topology,
                Flags = D3D12_PIPELINE_STATE_FLAGS.D3D12_PIPELINE_STATE_FLAG_NONE
            };

            Unsafe.As<D3D12_GRAPHICS_PIPELINE_STATE_DESC._RTVFormats_e__FixedBuffer, FormatBuffer8>(ref outDesc.RTVFormats) = inDesc.RenderTargetFormats;
        }

        private void TranslateDepthStencilState(in DepthStencilDesc desc, out D3D12_DEPTH_STENCIL_DESC depthStencilDesc)
        {
            TranslateStencilFuncState(desc.FrontFace, out D3D12_DEPTH_STENCILOP_DESC frontFaceDesc);
            TranslateStencilFuncState(desc.BackFace, out D3D12_DEPTH_STENCILOP_DESC backFaceDesc);

            depthStencilDesc = new D3D12_DEPTH_STENCIL_DESC
            {
                DepthEnable = desc.EnableDepthTesting ? Windows.TRUE : Windows.FALSE,
                StencilEnable = desc.EnableStencilTesting ? Windows.TRUE : Windows.FALSE,
                DepthFunc = (D3D12_COMPARISON_FUNC)desc.DepthComparison,
                DepthWriteMask = (D3D12_DEPTH_WRITE_MASK)desc.DepthWriteMask,
                StencilWriteMask = desc.StencilWriteMask,
                StencilReadMask = desc.StencilReadMask,
                FrontFace = frontFaceDesc,
                BackFace = backFaceDesc,
            };
        }

        private void TranslateStencilFuncState(StencilFuncDesc frontFace, out D3D12_DEPTH_STENCILOP_DESC frontFaceDesc)
        {
            frontFaceDesc = new D3D12_DEPTH_STENCILOP_DESC
            {
                StencilPassOp = (D3D12_STENCIL_OP)frontFace.StencilPasslOp,
                StencilFailOp = (D3D12_STENCIL_OP)frontFace.StencilTestFailOp,
                StencilDepthFailOp = (D3D12_STENCIL_OP)frontFace.StencilTestDepthTestFailOp,
                StencilFunc = (D3D12_COMPARISON_FUNC)frontFace.ExitingDataOp
            };
        }

        private void TranslateRasterizerState(in RasterizerDesc rasterizer, out D3D12_RASTERIZER_DESC rasterizerDesc)
        {
            rasterizerDesc = new D3D12_RASTERIZER_DESC
            {
                ConservativeRaster = rasterizer.EnableConservativerRasterization ? D3D12_CONSERVATIVE_RASTERIZATION_MODE_ON : D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF,
                FillMode = rasterizer.EnableWireframe ? D3D12_FILL_MODE.D3D12_FILL_MODE_WIREFRAME : D3D12_FILL_MODE.D3D12_FILL_MODE_SOLID,
                CullMode = (D3D12_CULL_MODE)rasterizer.FaceCullMode,
                FrontCounterClockwise = Windows.FALSE,
                DepthBias = rasterizer.DepthBias,
                DepthBiasClamp = rasterizer.MaxDepthBias,
                SlopeScaledDepthBias = rasterizer.SlopeScaledDepthBias,
                DepthClipEnable = rasterizer.EnableDepthClip ? Windows.TRUE : Windows.FALSE,
                AntialiasedLineEnable = Windows.FALSE,
                MultisampleEnable = rasterizer.EnableMsaa ? Windows.TRUE : Windows.FALSE,
                ForcedSampleCount = 0,
            };
        }

        private void TranslateBlendState(in BlendDesc blend, out D3D12_BLEND_DESC blendDesc)
        {
            blendDesc = new D3D12_BLEND_DESC
            {
                AlphaToCoverageEnable = blend.UseAlphaToCoverage ? Windows.TRUE : Windows.FALSE,
                IndependentBlendEnable = blend.UseIndependentBlend ? Windows.TRUE : Windows.FALSE
            };

            TranslateRenderTargetBlendState(blend.RenderTargetBlendDescs[0], out blendDesc.RenderTarget[0]);
            if (blend.UseIndependentBlend)
            {
                for (var i = 1; i < 8; i++)
                {
                    TranslateRenderTargetBlendState(blend.RenderTargetBlendDescs[0], out blendDesc.RenderTarget[0]);
                }
            }
        }

        private void TranslateRenderTargetBlendState(in RenderTargetBlendDesc inBlendDesc, out D3D12_RENDER_TARGET_BLEND_DESC desc)
        {
            Debug.Assert(
                inBlendDesc.LogicalBlendOp == BlendFuncLogical.None || (inBlendDesc.BlendOp == BlendFunc.None && inBlendDesc.AlphaBlendOp == BlendFunc.None),
                "Cannot use logical blending and RGB / alpha blending simultaneously"
            );

            desc = new D3D12_RENDER_TARGET_BLEND_DESC
            {
                RenderTargetWriteMask = (byte)inBlendDesc.RenderTargetWriteMask,
                BlendEnable = inBlendDesc.BlendOp == BlendFunc.None ? Windows.FALSE : Windows.TRUE,
                LogicOpEnable = inBlendDesc.LogicalBlendOp == BlendFuncLogical.None ? Windows.FALSE : Windows.TRUE,
                BlendOp = (D3D12_BLEND_OP)inBlendDesc.BlendOp,
                BlendOpAlpha = (D3D12_BLEND_OP)inBlendDesc.AlphaBlendOp,
                LogicOp = (D3D12_LOGIC_OP)inBlendDesc.LogicalBlendOp,
                SrcBlend = (D3D12_BLEND)inBlendDesc.SrcBlend,
                DestBlend = (D3D12_BLEND)inBlendDesc.DestBlend,
                SrcBlendAlpha = (D3D12_BLEND)inBlendDesc.SrcBlendAlpha,
                DestBlendAlpha = (D3D12_BLEND)inBlendDesc.DestBlendAlpha
            };
        }

        /// <summary>
        /// Retrives a pipeline state object by name
        /// </summary>
        /// <param name="name">The name of the PSO to retrieve</param>
        /// <returns>The PSO stored with the name</returns>
        public ComPtr<ID3D12PipelineState> RetrievePso(string name)
        {
            return _psos[name].Copy();
        }

        /// <summary>
        /// Store a pipeline state object with an associated name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pso"></param>
        /// <param name="overwrite"></param>
        public void StorePso(string name, ComPtr<ID3D12PipelineState> pso, bool overwrite = false)
        {
            if (overwrite)
            {
                _psos[name] = pso;
            }
            else
            {
                if (!_psos.TryAdd(name, pso))
                {
                    ThrowHelper.ThrowInvalidOperationException($"PSO with name '{name}' was already present, and the " +
                        $"overwrite parameter was set to false");
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (var value in _psos.Values)
            {
                value.Dispose();
            }
            _device.Dispose();
            //_psoLibrary.Dispose();
        }
    }
}
