//using System;
//using System.Diagnostics;
//using System.Runtime.CompilerServices;
//using System.Runtime.InteropServices;
//using System.Text;
//using TerraFX.Interop;
//using Voltium.Common;
//using Voltium.Core.Configuration.Graphics;
//using Voltium.Core.Devices.Shaders;
//using Voltium.Core.Pipeline;
//using static TerraFX.Interop.D3D12_CONSERVATIVE_RASTERIZATION_MODE;
//using static Voltium.Core.Pipeline.GraphicsPipelineDesc;
//namespace Voltium.Core.Devices
//{
//    internal unsafe static class PipelineTranslationLayer
//    {
//        public static byte[] TranslateLayouts(ReadOnlyMemory<ShaderInput> inputs, D3D12_INPUT_ELEMENT_DESC* pDesc)
//        {
//            var span = inputs.Span;
//            // this is SUPER inefficient
//            int totalStringLength = 0;
//            for (var i = 0; i < span.Length; i++)
//            {
//                // get the necessary length for strings
//                totalStringLength += Encoding.ASCII.GetMaxByteCount(span[i].Name.Length) + /* null char */ 1;
//            }

//            var asciiArr = GC.AllocateArray<byte>(totalStringLength, pinned: true);

//            var asciiBuff = asciiArr.AsSpan();
//            for (var i = 0; i < span.Length; i++)
//            {
//                var elem = span[i];
//                var read = Encoding.ASCII.GetBytes(elem.Name, asciiBuff);
//                asciiBuff[read++] = 0;

//                D3D12_INPUT_ELEMENT_DESC* pCurDesc = &pDesc[i];

//                *pCurDesc = default;
//                // This is the pinned array above so we can just take the pointer
//                pCurDesc->SemanticName = (sbyte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(asciiBuff));
//                pCurDesc->SemanticIndex = elem.NameIndex;
//                pCurDesc->AlignedByteOffset = elem.Offset;
//                pCurDesc->Format = (DXGI_FORMAT)elem.Type;
//                pCurDesc->InputSlot = elem.Channel;
//                pCurDesc->InputSlotClass = (D3D12_INPUT_CLASSIFICATION)elem.InputClass;

//                asciiBuff = asciiBuff.Slice(read);
//            }

//            return asciiArr;
//        }

//        public unsafe static void TranslateShadersMustBePinned(in GraphicsPipelineDesc inDesc, ref D3D12_GRAPHICS_PIPELINE_STATE_DESC outDesc)
//        {
//            outDesc.VS = new D3D12_SHADER_BYTECODE(Unsafe.AsPointer(ref MemoryMarshal.GetReference(inDesc.VertexShader.ShaderData)), (uint)inDesc.VertexShader.Length);
//            outDesc.PS = new D3D12_SHADER_BYTECODE(Unsafe.AsPointer(ref MemoryMarshal.GetReference(inDesc.PixelShader.ShaderData)), (uint)inDesc.PixelShader.Length);
//            outDesc.GS = new D3D12_SHADER_BYTECODE(Unsafe.AsPointer(ref MemoryMarshal.GetReference(inDesc.GeometryShader.ShaderData)), (uint)inDesc.GeometryShader.Length);
//            outDesc.DS = new D3D12_SHADER_BYTECODE(Unsafe.AsPointer(ref MemoryMarshal.GetReference(inDesc.DomainShader.ShaderData)), (uint)inDesc.DomainShader.Length);
//            outDesc.HS = new D3D12_SHADER_BYTECODE(Unsafe.AsPointer(ref MemoryMarshal.GetReference(inDesc.HullShader.ShaderData)), (uint)inDesc.HullShader.Length);
//        }

//        public unsafe static void TranslateGraphicsPipelineDescriptionWithoutShadersOrShaderInputLayoutElements(ComputeDevice device, ref GraphicsPipelineDesc inDesc, out D3D12_GRAPHICS_PIPELINE_STATE_DESC outDesc)
//        {
//            inDesc.RootSignature ??= device.EmptyRootSignature;
//            inDesc.Blend ??= BlendDesc.Default;
//            inDesc.Rasterizer ??= RasterizerDesc.Default;
//            inDesc.DepthStencil ??= DepthStencilDesc.Default;

//            TranslateBlendState(inDesc.Blend.Value, out D3D12_BLEND_DESC blendDesc);
//            TranslateRasterizerState(inDesc.Rasterizer.Value, out D3D12_RASTERIZER_DESC rasterizerDesc);
//            TranslateDepthStencilState(inDesc.DepthStencil.Value, out D3D12_DEPTH_STENCIL_DESC depthStencilDesc);

//            var msaa = inDesc.Msaa ?? MultisamplingDesc.None;


//            // We calculate num render targets by going through the buffer until we find a 0 value DataFormat (DataFormat.Unknown)
//            // as this is never a valid value for a render target
//            // this is effectively what the debug layer uses to verify NumRenderTargets
//            // We ensure there aren't any non-Unknown formats after that one to catch user error
//            uint numRenderTargets = 0;
//            bool invalidFormat = false;
//            for (var i = 0; i < FormatBuffer8.BufferLength; i++)
//            {
//                if (inDesc.RenderTargetFormats[i] == 0)
//                {
//                    invalidFormat = true;
//                    continue;
//                }

//                if (inDesc.RenderTargetFormats[i] != 0)
//                {
//                    if (!invalidFormat)
//                    {
//                        numRenderTargets++;
//                    }
//                    else
//                    {
//                        ThrowHelper.ThrowArgumentException("Render target format with value DataFormat.Unknown was encountered, " +
//                            "but it was followed by a non-DataForm.Unknown format. This is invalid");
//                    }
//                }
//            }

//            outDesc = new D3D12_GRAPHICS_PIPELINE_STATE_DESC
//            {
//                pRootSignature = inDesc.RootSignature.Value,
//                DSVFormat = (DXGI_FORMAT)inDesc.DepthStencilFormat,
//                BlendState = blendDesc,
//                RasterizerState = rasterizerDesc,
//                DepthStencilState = depthStencilDesc,
//                SampleMask = 0xFFFFFFFF,
//                NumRenderTargets = numRenderTargets,
//                SampleDesc = new DXGI_SAMPLE_DESC(msaa.SampleCount, msaa.QualityLevel),
//                NodeMask = inDesc.NodeMask,
//                PrimitiveTopologyType = inDesc.Topology.Class,
//                Flags = D3D12_PIPELINE_STATE_FLAGS.D3D12_PIPELINE_STATE_FLAG_NONE
//            };

//            Unsafe.As<D3D12_GRAPHICS_PIPELINE_STATE_DESC._RTVFormats_e__FixedBuffer, FormatBuffer8>(ref outDesc.RTVFormats) = inDesc.RenderTargetFormats;
//        }

//        public static void TranslateDepthStencilState(in DepthStencilDesc desc, out D3D12_DEPTH_STENCIL_DESC depthStencilDesc)
//        {
//            TranslateStencilFuncState(desc.FrontFace, out D3D12_DEPTH_STENCILOP_DESC frontFaceDesc);
//            TranslateStencilFuncState(desc.BackFace, out D3D12_DEPTH_STENCILOP_DESC backFaceDesc);

//            depthStencilDesc = new D3D12_DEPTH_STENCIL_DESC
//            {
//                DepthEnable = desc.EnableDepthTesting ? Windows.TRUE : Windows.FALSE,
//                StencilEnable = desc.EnableStencilTesting ? Windows.TRUE : Windows.FALSE,
//                DepthFunc = (D3D12_COMPARISON_FUNC)desc.DepthComparison,
//                DepthWriteMask = (D3D12_DEPTH_WRITE_MASK)desc.DepthWriteMask,
//                StencilWriteMask = desc.StencilWriteMask,
//                StencilReadMask = desc.StencilReadMask,
//                FrontFace = frontFaceDesc,
//                BackFace = backFaceDesc,
//            };
//        }

//        public static void TranslateStencilFuncState(StencilFuncDesc frontFace, out D3D12_DEPTH_STENCILOP_DESC frontFaceDesc)
//        {
//            frontFaceDesc = new D3D12_DEPTH_STENCILOP_DESC
//            {
//                StencilPassOp = (D3D12_STENCIL_OP)frontFace.StencilPasslOp,
//                StencilFailOp = (D3D12_STENCIL_OP)frontFace.StencilTestFailOp,
//                StencilDepthFailOp = (D3D12_STENCIL_OP)frontFace.StencilTestDepthTestFailOp,
//                StencilFunc = (D3D12_COMPARISON_FUNC)frontFace.ExistingDataOp
//            };
//        }

//        public static void TranslateRasterizerState(in RasterizerDesc rasterizer, out D3D12_RASTERIZER_DESC rasterizerDesc)
//        {
//            rasterizerDesc = new D3D12_RASTERIZER_DESC
//            {
//                ConservativeRaster = rasterizer.EnableConservativerRasterization ? D3D12_CONSERVATIVE_RASTERIZATION_MODE_ON : D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF,
//                FillMode = rasterizer.EnableWireframe ? D3D12_FILL_MODE.D3D12_FILL_MODE_WIREFRAME : D3D12_FILL_MODE.D3D12_FILL_MODE_SOLID,
//                CullMode = (D3D12_CULL_MODE)rasterizer.FaceCullMode,
//                FrontCounterClockwise = Helpers.BoolToInt32(rasterizer.FrontFaceType == FaceType.Clockwise),
//                DepthBias = rasterizer.DepthBias,
//                DepthBiasClamp = rasterizer.MaxDepthBias,
//                SlopeScaledDepthBias = rasterizer.SlopeScaledDepthBias,
//                DepthClipEnable = Helpers.BoolToInt32(rasterizer.EnableDepthClip),
//                AntialiasedLineEnable = Windows.FALSE,
//                MultisampleEnable = Helpers.BoolToInt32(rasterizer.EnableMsaa),
//                ForcedSampleCount = 0,
//            };
//        }

//        public static void TranslateBlendState(in BlendDesc blend, out D3D12_BLEND_DESC blendDesc)
//        {
//            blendDesc = new D3D12_BLEND_DESC
//            {
//                AlphaToCoverageEnable = blend.UseAlphaToCoverage ? Windows.TRUE : Windows.FALSE,
//                IndependentBlendEnable = blend.UseIndependentBlend ? Windows.TRUE : Windows.FALSE
//            };

//            TranslateRenderTargetBlendState(blend[0], out blendDesc.RenderTarget[0]);
//            if (blend.UseIndependentBlend)
//            {
//                for (var i = 1; i < 8; i++)
//                {
//                    TranslateRenderTargetBlendState(blend[i], out blendDesc.RenderTarget[i]);
//                }
//            }
//        }

//        public static void TranslateRenderTargetBlendState(in RenderTargetBlendDesc inBlendDesc, out D3D12_RENDER_TARGET_BLEND_DESC desc)
//        {
//            Debug.Assert(
//                inBlendDesc.LogicalBlendOp == BlendFuncLogical.None || (inBlendDesc.BlendOp == BlendFunc.None && inBlendDesc.AlphaBlendOp == BlendFunc.None),
//                "Cannot use logical blending and RGB / alpha blending simultaneously"
//            );

//            desc = new D3D12_RENDER_TARGET_BLEND_DESC
//            {
//                RenderTargetWriteMask = (byte)inBlendDesc.RenderTargetWriteMask,
//                BlendEnable = inBlendDesc.BlendOp == BlendFunc.None ? Windows.FALSE : Windows.TRUE,
//                LogicOpEnable = inBlendDesc.LogicalBlendOp == BlendFuncLogical.None ? Windows.FALSE : Windows.TRUE,
//                BlendOp = (D3D12_BLEND_OP)inBlendDesc.BlendOp,
//                BlendOpAlpha = (D3D12_BLEND_OP)inBlendDesc.AlphaBlendOp,
//                LogicOp = (D3D12_LOGIC_OP)inBlendDesc.LogicalBlendOp,
//                SrcBlend = (D3D12_BLEND)inBlendDesc.SrcBlend,
//                DestBlend = (D3D12_BLEND)inBlendDesc.DestBlend,
//                SrcBlendAlpha = (D3D12_BLEND)inBlendDesc.SrcBlendAlpha,
//                DestBlendAlpha = (D3D12_BLEND)inBlendDesc.DestBlendAlpha
//            };
//        }

//    }
//}
