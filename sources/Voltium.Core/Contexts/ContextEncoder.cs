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
        private ResourceHandle GetViewResource(ViewHandle handle) => throw new NotImplementedException();
        private ResourceState GetTrackedState(ResourceHandle handle) => throw new NotImplementedException();
        private bool IsLegalState(ResourceHandle handle, ResourceState state) => throw new NotImplementedException();
        private D3D12_CPU_DESCRIPTOR_HANDLE GetDsv(ViewHandle handle) => throw new NotImplementedException();
        private D3D12_CPU_DESCRIPTOR_HANDLE GetRtv(ViewHandle handle) => throw new NotImplementedException();
        private D3D12_CPU_DESCRIPTOR_HANDLE GetUav(ViewHandle handle) => throw new NotImplementedException();
        private void AssertState(ViewHandle handle, ResourceState state) => AssertState(GetViewResource(handle), state);
        private void AssertState(ResourceHandle handle, ResourceState state) => throw new NotImplementedException();
        private bool TryGetStateObjectPipeline(PipelineHandle handle, out ID3D12StateObject* pStateObject) => throw new NotImplementedException();
        private bool TryGetPipeline(PipelineHandle handle, out ID3D12PipelineState* pPipeline) => throw new NotImplementedException();
        private BindPoint PipelineBindPoint(PipelineHandle handle) => throw new NotImplementedException();
        private ID3D12RootSignature* GetRootSignature(PipelineHandle handle) => throw new NotImplementedException();
        private ID3D12QueryHeap* GetQueryHeap(QueryHeapHandle handle) => throw new NotImplementedException();

        private void EncodeBarrier(ResourceHandle handle, ResourceState before, ResourceState after) => throw new NotImplementedException();

        public void Encode(GraphicsContext context, ID3D12GraphicsCommandList6* pEncode)
        {
            var buff = context.CommandBuffer;

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
                            pPacketStart = &cmd->Arguments + sizeof(CommandBind32BitConstants) + (pConstants->Num32BitValues * sizeof(uint));
                            break;

                        case CommandType.BufferCopy:
                            var pCopy = &cmd->BufferCopy;
                            pEncode->CopyBufferRegion(GetResourcePointer(pCopy->Dest), pCopy->DestOffset, GetResourcePointer(pCopy->Source), pCopy->SourceOffset, pCopy->Length);
                            pPacketStart = &cmd->Arguments + sizeof(CommandBufferCopy);
                            break;

                        case CommandType.TextureCopy:
                            break;

                        case CommandType.BufferToTextureCopy:
                            break;

                        case CommandType.TextureToBufferCopy:
                            break;

                        case CommandType.Draw:
                            pEncode->DrawInstanced(
                                cmd->Draw.VertexCountPerInstance,
                                cmd->Draw.InstanceCount,
                                cmd->Draw.StartVertexLocation,
                                cmd->Draw.StartInstanceLocation
                            );
                            pPacketStart = &cmd->Arguments + sizeof(CommandDraw);
                            break;

                        case CommandType.DrawIndexed:
                            pEncode->DrawIndexedInstanced(
                                cmd->DrawIndexed.IndexCountPerInstance,
                                cmd->DrawIndexed.InstanceCount,
                                cmd->DrawIndexed.StartIndexLocation,
                                cmd->DrawIndexed.BaseVertexLocation,
                                cmd->DrawIndexed.StartInstanceLocation
                            );
                            pPacketStart = &cmd->Arguments + sizeof(CommandDrawIndexed);
                            break;

                        case CommandType.Dispatch:
                            pEncode->Dispatch(
                                cmd->Dispatch.X,
                                cmd->Dispatch.Y,
                                cmd->Dispatch.Z
                            );
                            pPacketStart = &cmd->Arguments + sizeof(CommandDispatch);
                            break;

                        case CommandType.RayTrace:
                            pEncode->DispatchRays(
                                (D3D12_DISPATCH_RAYS_DESC*)&cmd->RayTrace
                            );
                            pPacketStart = &cmd->Arguments + sizeof(CommandRayTrace);
                            break;

                        case CommandType.MeshDispatch:
                            pEncode->DispatchMesh(
                                cmd->MeshDispatch.X,
                                cmd->MeshDispatch.Y,
                                cmd->MeshDispatch.Z
                            );

                            pPacketStart = &cmd->Arguments + sizeof(CommandDispatchMesh);
                            break;

                        case CommandType.ClearDepthStencil:
                            CommandClearDepthStencil* pDepthClear = &cmd->ClearDepthStencil;
                            AssertState(pDepthClear->View, ResourceState.DepthWrite);
                            D3D12_CLEAR_FLAGS flags = default;
                            flags |= pDepthClear->Flags.HasFlag(DepthStencilClearFlags.ClearStencil) ? D3D12_CLEAR_FLAGS.D3D12_CLEAR_FLAG_STENCIL : 0;
                            flags |= pDepthClear->Flags.HasFlag(DepthStencilClearFlags.ClearDepth) ? D3D12_CLEAR_FLAGS.D3D12_CLEAR_FLAG_DEPTH : 0;
                            pEncode->ClearDepthStencilView(GetDsv(pDepthClear->View), 0, pDepthClear->Depth, pDepthClear->Stencil, pDepthClear->RectangleCount, (RECT*)(&pDepthClear->RectangleCount + sizeof(uint)));

                            pPacketStart = &cmd->Arguments + sizeof(CommandClearDepthStencil) + (pDepthClear->RectangleCount * sizeof(Rectangle));
                            break;

                        case CommandType.ClearTextureInteger:
                            CommandClearTextureInteger* pIntegerClear = &cmd->ClearTextureInteger;
                            var resource = GetViewResource(pIntegerClear->View);


                            pPacketStart = &cmd->Arguments + sizeof(CommandClearTexture) + (pIntegerClear->RectangleCount * sizeof(Rectangle));
                            break;

                        case CommandType.SetPipeline:
                            var pipeline = cmd->SetPipeline.Pipeline;
                            if (_previouslyBoundPipelineHandle != pipeline)
                            {
                                if (TryGetStateObjectPipeline(pipeline, out var pStateObject))
                                {
                                    pEncode->SetPipelineState1(pStateObject);
                                }
                                else
                                {
                                    _ = TryGetPipeline(pipeline, out var pPipeline);
                                    pEncode->SetPipelineState(pPipeline);
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

                            pPacketStart = &cmd->Arguments + sizeof(CommandSetPipeline);
                            break;

                        case CommandType.SetShadingRate:
                            var pShadingRate = &cmd->SetShadingRate;
                            pEncode->RSSetShadingRate(
                                (D3D12_SHADING_RATE)pShadingRate->BaseRate,
                                (D3D12_SHADING_RATE_COMBINER*)&pShadingRate->ShaderCombiner
                            );

                            pPacketStart = &cmd->Arguments + sizeof(CommandSetShadingRate);
                            break;

                        case CommandType.SetShadingRateImage:
                            pEncode->RSSetShadingRateImage(GetResourcePointer(cmd->SetShadingRateImage.ShadingRateImage));

                            pPacketStart = &cmd->Arguments + sizeof(CommandSetShadingRateImage);
                            break;

                        case CommandType.SetTopology:
                            pEncode->IASetPrimitiveTopology((D3D_PRIMITIVE_TOPOLOGY)cmd->SetTopology.Topology);

                            pPacketStart = &cmd->Arguments + sizeof(CommandSetTopology);
                            break;

                        case CommandType.SetStencilRef:
                            pEncode->OMSetStencilRef(cmd->SetStencilRef.StencilRef);

                            pPacketStart = &cmd->Arguments + sizeof(CommandSetStencilRef);
                            break;

                        case CommandType.SetBlendFactor:
                            pEncode->OMSetBlendFactor(cmd->SetBlendFactor.BlendFactor);

                            pPacketStart = &cmd->Arguments + sizeof(CommandSetBlendFactor);
                            break;

                        case CommandType.SetDepthBounds:
                            var pDepthBounds = &cmd->SetDepthBounds;
                            pEncode->OMSetDepthBounds(pDepthBounds->Min, pDepthBounds->Max);

                            pPacketStart = &cmd->Arguments + sizeof(CommandSetDepthBounds);
                            break;

                        case CommandType.SetSamplePositions:
                            var pSamplePositions = &cmd->SetSamplePositions;
                            pEncode->SetSamplePositions(
                                pSamplePositions->SamplesPerPixel,
                                pSamplePositions->PixelCount,
                                (D3D12_SAMPLE_POSITION*)pSamplePositions->Values
                            );

                            pPacketStart = &cmd->Arguments + sizeof(CommandSetSamplePositions)
                                + (pSamplePositions->PixelCount * pSamplePositions->SamplesPerPixel * sizeof(SamplePosition));
                            break;

                        case CommandType.SetViewInstanceMask:
                            pEncode->SetViewInstanceMask(cmd->SetViewInstanceMask.Mask);

                            AdvanceCommand(&cmd->SetViewInstanceMask);
                            break;

                        case CommandType.BindDescriptors:
                            break;

                        case CommandType.BeginRenderPass:

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

                    void AdvanceCommand<T>(T* pVal) where T : unmanaged => pPacketStart = &cmd->Arguments + sizeof(T);
                    void AdvanceEmptyCommand() => pPacketStart = &cmd->Arguments;
                }

            }
        }
    }
}
