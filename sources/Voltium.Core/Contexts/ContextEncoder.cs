using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using MathSharp;
using SixLabors.ImageSharp;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;
using Vector = MathSharp.Vector;

namespace Voltium.Core.Contexts
{
    internal unsafe sealed class ContextEncoder
    {
        private ComputeDevice _device;

        public ContextEncoder(ComputeDevice device)
        {
            _device = device;
        }

        private PipelineHandle _previouslyBoundPipelineHandle;

        private ID3D12Resource* GetResourcePointer(ResourceHandle handle) => throw new NotImplementedException();
        private DXGI_FORMAT GetFormat(ViewHandle handle) => throw new NotImplementedException();
        private ResourceHandle GetViewResource(ViewHandle handle) => throw new NotImplementedException();
        private ResourceState GetTrackedState(ResourceHandle handle) => throw new NotImplementedException();
        private bool IsLegalState(ResourceHandle handle, ResourceState state) => throw new NotImplementedException();
        private D3D12_CPU_DESCRIPTOR_HANDLE GetDsv(ViewHandle handle) => throw new NotImplementedException();
        private D3D12_CPU_DESCRIPTOR_HANDLE GetRtv(ViewHandle handle) => throw new NotImplementedException();
        private D3D12_CPU_DESCRIPTOR_HANDLE GetUav(ViewHandle handle) => throw new NotImplementedException();
        private D3D12_GPU_DESCRIPTOR_HANDLE GetShaderDescriptor(ViewHandle handle) => throw new NotImplementedException();
        private void AssertState(ViewHandle handle, ResourceState state) => AssertState(GetViewResource(handle), state);
        private void AssertState(ResourceHandle handle, ResourceState state) => throw new NotImplementedException();
        private bool TryGetStateObjectPipeline(PipelineHandle handle, out ID3D12StateObject* pStateObject) => throw new NotImplementedException();
        private bool TryGetPipeline(PipelineHandle handle, out ID3D12PipelineState* pPipeline) => throw new NotImplementedException();
        private BindPoint PipelineBindPoint(PipelineHandle handle) => throw new NotImplementedException();
        private ID3D12RootSignature* GetRootSignature(PipelineHandle handle) => throw new NotImplementedException();
        private ID3D12QueryHeap* GetQueryHeap(QueryHeapHandle handle) => throw new NotImplementedException();
        private RenderPassInfo GetRenderPassInfo(RenderPassHandle handle) => throw new NotImplementedException();

        private void EncodeBarrier(ResourceHandle handle, ResourceState before, ResourceState after) => throw new NotImplementedException();

        public void Encode(GraphicsContext context, ID3D12GraphicsCommandList6* pEncode)
        {
            var buff = context.CommandBuffer;

            // for BeginRenderPass. No stackalloc in loops remember!
            var renderTargets = stackalloc D3D12_RENDER_PASS_RENDER_TARGET_DESC[8];
            ResourceHandle buffer, texture;
            ViewHandle view;

            fixed (byte* pBuff = &buff[0])
            {
                byte* pPacketStart = pBuff;
                byte* pPacketEnd = pPacketStart + buff.Length;

                while (pPacketStart < pPacketEnd)
                {
                    var cmd = (Command*)pPacketStart;
                    switch (cmd->Type)
                    {
                        case CommandType.Bind32BitConstants:
                            var pConstants = &cmd->Bind32BitConstants;
                            switch (pConstants->BindPoint)
                            {
                                case BindPoint.Graphics:
                                    pEncode->SetGraphicsRoot32BitConstants(pConstants->ParameterIndex, pConstants->Num32BitValues, pConstants->Values, pConstants->OffsetIn32BitValues);
                                    break;
                                case BindPoint.Compute:
                                    pEncode->SetComputeRoot32BitConstants(pConstants->ParameterIndex, pConstants->Num32BitValues, pConstants->Values, pConstants->OffsetIn32BitValues);
                                    break;
                                default:
                                    _device.ThrowGraphicsException("Invalid bind point?");
                                    break;
                            }

                            AdvanceVariableCommand(pConstants, pConstants->Values, pConstants->Num32BitValues);
                            break;

                        case CommandType.BufferCopy:
                            var pCopy = &cmd->BufferCopy;
                            pEncode->CopyBufferRegion(GetResourcePointer(pCopy->Dest), pCopy->DestOffset, GetResourcePointer(pCopy->Source), pCopy->SourceOffset, pCopy->Length);

                            AdvanceCommand(pCopy);
                            break;

                        case CommandType.TextureCopy:
                            break;

                        case CommandType.BufferToTextureCopy:
                            break;

                        case CommandType.TextureToBufferCopy:
                            break;

                        case CommandType.Draw:
                            var pDraw = &cmd->Draw;

                            pEncode->DrawInstanced(
                                pDraw->VertexCountPerInstance,
                                pDraw->InstanceCount,
                                pDraw->StartVertexLocation,
                                pDraw->StartInstanceLocation
                            );

                            AdvanceCommand(pDraw);
                            break;

                        case CommandType.DrawIndexed:
                            var pDrawIndexed = &cmd->DrawIndexed;

                            pEncode->DrawIndexedInstanced(
                                pDrawIndexed->IndexCountPerInstance,
                                pDrawIndexed->InstanceCount,
                                pDrawIndexed->StartIndexLocation,
                                pDrawIndexed->BaseVertexLocation,
                                pDrawIndexed->StartInstanceLocation
                            );

                            AdvanceCommand(pDrawIndexed);
                            break;

                        case CommandType.Dispatch:
                            var pDispatch = &cmd->Dispatch;

                            pEncode->Dispatch(
                                pDispatch->X,
                                pDispatch->Y,
                                pDispatch->Z
                            );

                            AdvanceCommand(pDispatch);
                            break;

                        case CommandType.RayTrace:
                            var pRayTrace = &cmd->RayTrace;

                            pEncode->DispatchRays(
                                (D3D12_DISPATCH_RAYS_DESC*)pRayTrace
                            );

                            AdvanceCommand(pRayTrace);
                            break;

                        case CommandType.MeshDispatch:
                            var pMeshDispatch = &cmd->MeshDispatch;

                            pEncode->DispatchMesh(
                                pMeshDispatch->X,
                                pMeshDispatch->Y,
                                pMeshDispatch->Z
                            );

                            AdvanceCommand(pMeshDispatch);
                            break;

                        case CommandType.ClearDepthStencil:
                            var pDepthClear = &cmd->ClearDepthStencil;
                            AssertState(pDepthClear->View, ResourceState.DepthWrite);
                            D3D12_CLEAR_FLAGS flags = default;
                            flags |= pDepthClear->Flags.HasFlag(DepthStencilClearFlags.ClearStencil) ? D3D12_CLEAR_FLAGS.D3D12_CLEAR_FLAG_STENCIL : 0;
                            flags |= pDepthClear->Flags.HasFlag(DepthStencilClearFlags.ClearDepth) ? D3D12_CLEAR_FLAGS.D3D12_CLEAR_FLAG_DEPTH : 0;
                            pEncode->ClearDepthStencilView(GetDsv(pDepthClear->View), 0, pDepthClear->Depth, pDepthClear->Stencil, pDepthClear->RectangleCount, (RECT*)(&pDepthClear->RectangleCount + sizeof(uint)));

                            AdvanceVariableCommand(pDepthClear, pDepthClear->Rectangles, pDepthClear->RectangleCount);
                            break;

                        case CommandType.ClearTextureInteger:
                            var pIntegerClear = &cmd->ClearTextureInteger;
                            view = pIntegerClear->View;
                            texture = GetViewResource(view);

                            pEncode->ClearUnorderedAccessViewUint(
                                GetShaderDescriptor(view),
                                GetUav(view),
                                GetResourcePointer(texture),
                                pIntegerClear->ClearValue,
                                pIntegerClear->RectangleCount,
                                (RECT*)pIntegerClear->Rectangles
                            );


                            AdvanceVariableCommand(pIntegerClear, pIntegerClear->Rectangles, pIntegerClear->RectangleCount);
                            break;

                        case CommandType.ClearTexture:
                            var pClear = &cmd->ClearTexture;
                            view = pClear->View;
                            texture = GetViewResource(view);

                            pEncode->ClearUnorderedAccessViewFloat(
                                GetShaderDescriptor(view),
                                GetUav(view),
                                GetResourcePointer(texture),
                                pClear->ClearValue,
                                pClear->RectangleCount,
                                (RECT*)pClear->Rectangles
                            );


                            AdvanceVariableCommand(pClear, pClear->Rectangles, pClear->RectangleCount);
                            break;

                        case CommandType.ClearBuffer:
                            var pClearBuffer = &cmd->ClearBuffer;
                            view = pClearBuffer->View;
                            buffer = GetViewResource(pClearBuffer->View);

                            pEncode->ClearUnorderedAccessViewFloat(
                                GetShaderDescriptor(view),
                                GetUav(view),
                                GetResourcePointer(buffer),
                                pClearBuffer->ClearValue,
                                0,
                                null
                            );

                            AdvanceCommand(pClearBuffer);
                            break;

                        case CommandType.ClearBufferInteger:
                            var pClearBufferInteger = &cmd->ClearBufferInteger;
                            view = pClearBufferInteger->View;
                            buffer = GetViewResource(pClearBufferInteger->View);

                            pEncode->ClearUnorderedAccessViewUint(
                                GetShaderDescriptor(view),
                                GetUav(view),
                                GetResourcePointer(buffer),
                                pClearBufferInteger->ClearValue,
                                0,
                                null
                            );

                            AdvanceCommand(pClearBufferInteger);
                            break;

                        case CommandType.SetPipeline:
                            var pPipeline = &cmd->SetPipeline;
                            var pipeline = pPipeline->Pipeline;
                            if (_previouslyBoundPipelineHandle != pipeline)
                            {
                                if (TryGetStateObjectPipeline(pipeline, out var pStateObject))
                                {
                                    pEncode->SetPipelineState1(pStateObject);
                                }
                                else
                                {
                                    _ = TryGetPipeline(pipeline, out var pPipelineState);
                                    pEncode->SetPipelineState(pPipelineState);
                                }

                                var rootSig = GetRootSignature(pipeline);
                                switch (PipelineBindPoint(pipeline))
                                {
                                    case BindPoint.Graphics:
                                        pEncode->SetGraphicsRootSignature(rootSig);
                                        break;
                                    case BindPoint.Compute:
                                        pEncode->SetComputeRootSignature(rootSig);
                                        break;
                                    default:
                                        _device.ThrowGraphicsException("bad bind point");
                                        break;
                                }
                                _previouslyBoundPipelineHandle = pipeline;
                            }

                            AdvanceCommand(pPipeline);
                            break;

                        case CommandType.SetShadingRate:
                            var pShadingRate = &cmd->SetShadingRate;
                            pEncode->RSSetShadingRate(
                                (D3D12_SHADING_RATE)pShadingRate->BaseRate,
                                (D3D12_SHADING_RATE_COMBINER*)&pShadingRate->ShaderCombiner
                            );

                            AdvanceCommand(pShadingRate);
                            break;

                        case CommandType.SetShadingRateImage:
                            var pShadingRateImage = &cmd->SetShadingRateImage;
                            pEncode->RSSetShadingRateImage(GetResourcePointer(pShadingRateImage->ShadingRateImage));

                            AdvanceCommand(pShadingRateImage);
                            break;

                        case CommandType.SetTopology:
                            var pTopology = &cmd->SetTopology;
                            pEncode->IASetPrimitiveTopology((D3D_PRIMITIVE_TOPOLOGY)pTopology->Topology);

                            AdvanceCommand(pTopology);
                            break;

                        case CommandType.SetStencilRef:
                            var pStencilRef = &cmd->SetStencilRef;
                            pEncode->OMSetStencilRef(pStencilRef->StencilRef);

                            AdvanceCommand(pStencilRef);
                            break;

                        case CommandType.SetBlendFactor:
                            var pBlendFactor = &cmd->SetBlendFactor;
                            pEncode->OMSetBlendFactor(pBlendFactor->BlendFactor);

                            AdvanceCommand(pBlendFactor);
                            break;

                        case CommandType.SetDepthBounds:
                            var pDepthBounds = &cmd->SetDepthBounds;
                            pEncode->OMSetDepthBounds(pDepthBounds->Min, pDepthBounds->Max);

                            AdvanceCommand(pDepthBounds);
                            break;

                        case CommandType.SetSamplePositions:
                            var pSamplePositions = &cmd->SetSamplePositions;
                            pEncode->SetSamplePositions(
                                pSamplePositions->SamplesPerPixel,
                                pSamplePositions->PixelCount,
                                (D3D12_SAMPLE_POSITION*)pSamplePositions->SamplePositions
                            );

                            AdvanceVariableCommand(pSamplePositions, pSamplePositions->SamplePositions, pSamplePositions->SamplesPerPixel * pSamplePositions->PixelCount);
                            break;

                        case CommandType.SetViewInstanceMask:
                            pEncode->SetViewInstanceMask(cmd->SetViewInstanceMask.Mask);

                            AdvanceCommand(&cmd->SetViewInstanceMask);
                            break;

                        case CommandType.BindDescriptors:
                            break;

                        case CommandType.BeginRenderPass:
                            var pBeginRenderPass = &cmd->BeginRenderPass;
                            var info = GetRenderPassInfo(pBeginRenderPass->RenderPass);

                            var depthStencilDesc = pBeginRenderPass->DepthStencil;

                            static D3D12_RENDER_PASS_BEGINNING_ACCESS_CLEAR_PARAMETERS CreateDepthStencilClear(DXGI_FORMAT format, float depth, byte stencil)
                                => new D3D12_RENDER_PASS_BEGINNING_ACCESS_CLEAR_PARAMETERS
                                {
                                    ClearValue = new D3D12_CLEAR_VALUE
                                    {
                                        Format = format,
                                        DepthStencil = new D3D12_DEPTH_STENCIL_VALUE
                                        {
                                            Depth = depth,
                                            Stencil = stencil
                                        }
                                    }
                                };

                            var depthStencilClear = CreateDepthStencilClear(GetFormat(depthStencilDesc.Resource), depthStencilDesc.Depth, depthStencilDesc.Stencil);

                            var depthStencil = new D3D12_RENDER_PASS_DEPTH_STENCIL_DESC
                            {
                                cpuDescriptor = GetDsv(depthStencilDesc.Resource),
                                DepthBeginningAccess = new D3D12_RENDER_PASS_BEGINNING_ACCESS
                                {
                                    Type = (D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE)info.DepthLoad,
                                },
                                DepthEndingAccess = new D3D12_RENDER_PASS_ENDING_ACCESS
                                {
                                    Type = (D3D12_RENDER_PASS_ENDING_ACCESS_TYPE)info.DepthStore
                                },
                                StencilBeginningAccess = new D3D12_RENDER_PASS_BEGINNING_ACCESS
                                {
                                    Type = (D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE)info.DepthLoad,
                                },
                                StencilEndingAccess = new D3D12_RENDER_PASS_ENDING_ACCESS
                                {
                                    Type = (D3D12_RENDER_PASS_ENDING_ACCESS_TYPE)info.DepthStore
                                },
                            };

                            if (info.DepthLoad == LoadOperation.Clear)
                            {
                                depthStencil.DepthBeginningAccess.Clear = depthStencilClear;
                            }
                            if (info.StencilLoad == LoadOperation.Clear)
                            {
                                depthStencil.StencilBeginningAccess.Clear = depthStencilClear;
                            }

                            // see top of method for renderTargets stackalloc
                            for (var i = 0; i < pBeginRenderPass->RenderTargetCount; i++)
                            {
                                var target = pBeginRenderPass->RenderTargets[i];

                                var load = info.RenderTargetLoad[i];
                                var store = info.RenderTargetStore[i];

                                renderTargets[i] = new D3D12_RENDER_PASS_RENDER_TARGET_DESC
                                {
                                    cpuDescriptor = GetRtv(target.Resource),
                                    BeginningAccess = new D3D12_RENDER_PASS_BEGINNING_ACCESS
                                    {
                                        Type = (D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE)load,
                                    },
                                    EndingAccess = new D3D12_RENDER_PASS_ENDING_ACCESS
                                    {
                                        Type = (D3D12_RENDER_PASS_ENDING_ACCESS_TYPE)store,
                                    },
                                };

                                if (load == LoadOperation.Clear)
                                {
                                    Unsafe.As<float, Rgba128>(ref renderTargets[i].BeginningAccess.Clear.ClearValue.Anonymous.Color[0]) = *(Rgba128*)target.ClearValue;
                                }
                            }

                            pEncode->BeginRenderPass(
                                pBeginRenderPass->RenderTargetCount,
                                renderTargets,
                                &depthStencil,
                                pBeginRenderPass->AllowTextureWrites ? D3D12_RENDER_PASS_FLAGS.D3D12_RENDER_PASS_FLAG_ALLOW_UAV_WRITES : 0
                            );

                            AdvanceVariableCommand(pBeginRenderPass, pBeginRenderPass->RenderTargets, pBeginRenderPass->RenderTargetCount);
                            break;

                        case CommandType.EndRenderPass:
                            pEncode->EndRenderPass();

                            AdvanceEmptyCommand();
                            break;

                        case CommandType.ReadTimestamp:
                            var pReadTimestamp = &cmd->ReadTimestamp;
                            pEncode->BeginQuery(
                                GetQueryHeap(pReadTimestamp->QueryHeap),
                                D3D12_QUERY_TYPE.D3D12_QUERY_TYPE_TIMESTAMP,
                                pReadTimestamp->Index
                            );

                            AdvanceCommand(pReadTimestamp);
                            break;

                        case CommandType.BeginQuery:
                            var pBeginQuery = &cmd->BeginQuery;
                            pEncode->BeginQuery(
                                GetQueryHeap(pBeginQuery->QueryHeap),
                                (D3D12_QUERY_TYPE)pBeginQuery->Type,
                                pBeginQuery->Index
                            );

                            AdvanceCommand(pBeginQuery);
                            break;

                        case CommandType.EndQuery:
                            var pEndQuery = &cmd->EndQuery;
                            pEncode->EndQuery(
                                GetQueryHeap(pEndQuery->QueryHeap),
                                (D3D12_QUERY_TYPE)pEndQuery->Type,
                                pEndQuery->Index
                            );

                            AdvanceCommand(pEndQuery);
                            break;

                        case CommandType.BeginConditionalRendering:
                            var pConditionalRender = &cmd->BeginConditionalRendering;
                            pEncode->SetPredication(
                                GetResourcePointer(pConditionalRender->Buffer),
                                pConditionalRender->Offset,
                                pConditionalRender->Predicate ? D3D12_PREDICATION_OP.D3D12_PREDICATION_OP_NOT_EQUAL_ZERO : D3D12_PREDICATION_OP.D3D12_PREDICATION_OP_EQUAL_ZERO
                            );

                            AdvanceCommand(pConditionalRender);
                            break;

                        case CommandType.EndConditionalRendering:
                            pEncode->SetPredication(null, 0, 0);

                            AdvanceEmptyCommand();
                            break;

                        case CommandType.CopyAccelerationStructure:
                            break;
                        case CommandType.BuildAccelerationStructure:
                            break;
                        case CommandType.ExecuteIndirect:
                            break;

                            //static bool IsSafeToFpRecast(Vector128<float> pVal) => Vector.IsFinite(pVal).AllTrue();
                    }

                    void AdvanceCommand<T>(T* pVal)
                        where T : unmanaged => pPacketStart = &cmd->Arguments + sizeof(T);

                    void AdvanceVariableCommand<T, TVariable>(T* pVal, TVariable* pVariable, uint pVariableCount)
                        where T : unmanaged where TVariable : unmanaged => pPacketStart = &cmd->Arguments + sizeof(T) + (sizeof(TVariable) * pVariableCount);

                    void AdvanceEmptyCommand() => pPacketStart = &cmd->Arguments;
                }

            }
        }
    }
}
