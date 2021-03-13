using System;
using System.Collections.Generic;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Contexts;
using Voltium.Core.Memory;
using static TerraFX.Interop.Windows;
using System.Diagnostics;
using Voltium.Core.CommandBuffer;
using Microsoft.Toolkit.HighPerformance.Extensions;
using System.Runtime.CompilerServices;
using Voltium.Core.NativeApi;
using Voltium.Core.NativeApi.D3D12;

namespace Voltium.Core.Devices
{
    /// <inheritdoc />
    public unsafe class D3D12NativeQueue : INativeQueue
    {
        private struct InFlightAllocator
        {
            public UniqueComPtr<ID3D12CommandAllocator> Allocator;
            public ulong Finish;
        }

        /// <inheritdoc />
        public INativeDevice Device => _device;

        private D3D12NativeDevice _device;
        private D3D12_COMMAND_LIST_TYPE _type;
        private UniqueComPtr<ID3D12CommandQueue> _queue;
        private FenceHandle _fenceHandle;
        private UniqueComPtr<ID3D12Fence> _fence;
        private Queue<InFlightAllocator> _allocators;
        private UniqueComPtr<ID3D12GraphicsCommandList6> _list;
        private readonly object _lock = new();
        private ulong _fenceValue = 0;

        internal D3D12NativeQueue(D3D12NativeDevice device, ExecutionEngine type)
        {
            _device = device;
            _type = (D3D12_COMMAND_LIST_TYPE)type;
            _allocators = new();

            fixed (void* pQueue = &_queue)
            fixed (void* pList = &_list)
            fixed (void* pFence = &_fence)
            {
                var desc = new D3D12_COMMAND_QUEUE_DESC
                {
                    Type = _type,
                    Priority = (int)D3D12_COMMAND_QUEUE_PRIORITY.D3D12_COMMAND_QUEUE_PRIORITY_NORMAL
                };

                _device.ThrowIfFailed(_device.GetDevice()->CreateCommandList1(0, _type, 0, _list.Iid, (void**)pList));
                _device.ThrowIfFailed(_device.GetDevice()->CreateCommandQueue(&desc, _queue.Iid, (void**)pQueue));
                _device.ThrowIfFailed(_device.GetDevice()->CreateFence(0, 0, _fence.Iid, (void**)pFence));
            }

            _fenceHandle = _device.CreateFromPreexisting(_fence.Ptr);

            ulong frequency;
            _ = _queue.Ptr->GetTimestampFrequency(&frequency);
            Frequency = frequency;

        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_queue.Exists)
            {
                return;
            }

            lock (_lock)
            {
                foreach (var allocator in _allocators)
                {
                    allocator.Allocator.Dispose();
                }
                _list.Dispose();
                _fence.Dispose();
                _queue.Dispose();
            }
        }

        internal IUnknown* GetQueue() => _queue.AsIUnknown().Ptr;

        /// <inheritdoc />
        public ulong Frequency { get; }

        /// <inheritdoc />
        public GpuTask Execute(
            ReadOnlySpan<CommandBuffer> cmds,
            ReadOnlySpan<GpuTask> dependencies
        )
        {
            if (cmds.IsEmpty && dependencies.IsEmpty)
            {
                return GpuTask.Completed;
            }

            lock (_lock)
            {
                foreach (var dependency in dependencies)
                {
                    if (!dependency.IsCompleted)
                    {
                        _device.ThrowIfFailed(_queue.Ptr->Wait(_device.GetFence(dependency._fence).Fence, dependency._reached));
                    }
                }

                var list = _list.Ptr;
                var allocator = GetAllocator();

                if (!cmds.IsEmpty)
                {

                    Encode(cmds, allocator.Ptr, list);

                    _device.ThrowIfFailed(list->Close());

                    _queue.Ptr->ExecuteCommandLists(1, (ID3D12CommandList**)&list);
                }

                _device.ThrowIfFailed(_queue.Ptr->Signal(_fence.Ptr, ++_fenceValue));

                ReturnAllocator(allocator, _fenceValue);

                
                return new(_device, _fenceHandle, _fenceValue);
            }
        }

        public void QueryTimestamps(ulong* cpu, ulong* gpu) => _queue.Ptr->GetClockCalibration(gpu, cpu);

        private UniqueComPtr<ID3D12CommandAllocator> GetAllocator()
        {
            UniqueComPtr<ID3D12CommandAllocator> allocator = default;
            if (_allocators.TryPeek(out var peek) && _fence.Ptr->GetCompletedValue() >= peek.Finish)
            {
                allocator = _allocators.Dequeue().Allocator;
                _device.ThrowIfFailed(allocator.Ptr->Reset());
            }

            _device.ThrowIfFailed(_device.GetDevice()->CreateCommandAllocator(_type, allocator.Iid, (void**)&allocator));
            return allocator;
        }

        private void ReturnAllocator(UniqueComPtr<ID3D12CommandAllocator> allocator, ulong finish)
            => _allocators.Enqueue(new InFlightAllocator { Allocator = allocator, Finish = finish });

        private void Encode(ReadOnlySpan<CommandBuffer> cmdBuffs, ID3D12CommandAllocator* pAllocator, ID3D12GraphicsCommandList6* pEncode)
        {
            const uint WIN_EVENT_3BLOB_VERSION = 2;
            const uint D3D12_EVENT_METADATA = WIN_EVENT_3BLOB_VERSION;

            const int MaxBarrierCount = 32;

            // for BeginRenderPass. No stackalloc in loops remember!
            var renderTargets = stackalloc D3D12_RENDER_PASS_RENDER_TARGET_DESC[8];
            var vertexBuffers = stackalloc D3D12_VERTEX_BUFFER_VIEW[8];
            var barriers = stackalloc D3D12_RESOURCE_BARRIER[MaxBarrierCount];


            D3D12PipelineState pipelineState = default;

            D3D12View view;
            uint i, j;

            ref var mapper = ref _device.GetMapperRef();

            var initPipeline = cmdBuffs[0].FirstPipeline is null ? default : mapper.GetInfo(cmdBuffs[0].FirstPipeline!.Value);

            if (FAILED(pEncode->Reset(pAllocator, initPipeline.IsRaytracing ? null : (ID3D12PipelineState*)initPipeline.PipelineState)))
            {
                Debug.Fail("AAAAAAAAAA");
            }

            if (initPipeline.IsRaytracing)
            {
                pEncode->SetPipelineState1((ID3D12StateObject*)initPipeline.PipelineState);
            }

            foreach (var cmdBuff in cmdBuffs)
            {
                var buff = cmdBuff.Buffer.Span;
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
                                        ThrowGraphicsException("Invalid bind point?");
                                        break;
                                }

                                AdvanceVariableCommand(pConstants, pConstants->Values, pConstants->Num32BitValues);
                                break;



                            case CommandType.BindVirtualAddress:
                                var pBindVirtualAddress = &cmd->BindVirtualAddress;

                                RootParameterType type = pipelineState.RootParameters![(int)pBindVirtualAddress->ParamIndex].Type;
                                switch (pBindVirtualAddress->BindPoint)
                                {
                                    case BindPoint.Graphics:
                                        switch (type)

                                        {
                                            case RootParameterType.ConstantBufferView:
                                                pEncode->SetGraphicsRootConstantBufferView(pBindVirtualAddress->ParamIndex, pBindVirtualAddress->VirtualAddress);
                                                break;
                                            case RootParameterType.ShaderResourceView:
                                                pEncode->SetGraphicsRootShaderResourceView(pBindVirtualAddress->ParamIndex, pBindVirtualAddress->VirtualAddress);
                                                break;
                                            case RootParameterType.UnorderedAccessView:
                                                pEncode->SetGraphicsRootUnorderedAccessView(pBindVirtualAddress->ParamIndex, pBindVirtualAddress->VirtualAddress);
                                                break;
                                        }
                                        break;
                                    case BindPoint.Compute:
                                        switch (type)
                                        {
                                            case RootParameterType.ConstantBufferView:
                                                pEncode->SetComputeRootConstantBufferView(pBindVirtualAddress->ParamIndex, pBindVirtualAddress->VirtualAddress);
                                                break;
                                            case RootParameterType.ShaderResourceView:
                                                pEncode->SetComputeRootShaderResourceView(pBindVirtualAddress->ParamIndex, pBindVirtualAddress->VirtualAddress);
                                                break;
                                            case RootParameterType.UnorderedAccessView:
                                                pEncode->SetComputeRootUnorderedAccessView(pBindVirtualAddress->ParamIndex, pBindVirtualAddress->VirtualAddress);
                                                break;
                                        }
                                        break;
                                    default:
                                        ThrowGraphicsException("Invalid bind point?");
                                        break;
                                }

                                AdvanceCommand(pBindVirtualAddress);
                                break;

                            case CommandType.BindDescriptors:
                                var pBindDescriptors = &cmd->BindDescriptors;

                                switch (pBindDescriptors->BindPoint)
                                {
                                    case BindPoint.Graphics:
                                        for (i = 0u; i < pBindDescriptors->SetCount; i++)
                                        {
                                            var (offset, _, _) = Helpers.Unpack2x32To2x24_16(
                                                pBindDescriptors->Sets[i].Generational.Generation,
                                                pBindDescriptors->Sets[i].Generational.Id
                                            );
                                            pEncode->SetGraphicsRootDescriptorTable(i, _device.GetShaderDescriptorForGpu(offset));
                                        }
                                        break;
                                    case BindPoint.Compute:
                                        for (i = 0u; i < pBindDescriptors->SetCount; i++)
                                        {
                                            var (offset, _, _) = Helpers.Unpack2x32To2x24_16(
                                                pBindDescriptors->Sets[i].Generational.Generation,
                                                pBindDescriptors->Sets[i].Generational.Id
                                            );
                                            pEncode->SetComputeRootDescriptorTable(i, _device.GetShaderDescriptorForGpu(offset));
                                        }
                                        break;
                                    default:
                                        ThrowGraphicsException("Invalid bind point?");
                                        break;
                                }

                                AdvanceVariableCommand(pBindDescriptors, pBindDescriptors->Sets, pBindDescriptors->SetCount);
                                break;

                            case CommandType.BufferCopy:
                                var pCopy = &cmd->BufferCopy;
                                pEncode->CopyBufferRegion(mapper.GetResourcePointer(pCopy->Dest), pCopy->DestOffset, mapper.GetResourcePointer(pCopy->Source), pCopy->SourceOffset, pCopy->Length);

                                AdvanceCommand(pCopy);
                                break;

                            case CommandType.TextureCopy:
                                break;

                            case CommandType.BufferToTextureCopy:
                                var pBufToTexCopy = &cmd->BufferToTextureCopy;

                                var src = new D3D12_TEXTURE_COPY_LOCATION
                                {
                                    Type = D3D12_TEXTURE_COPY_TYPE.D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT,
                                    pResource = mapper.GetResourcePointer(pBufToTexCopy->Source),
                                    PlacedFootprint = new D3D12_PLACED_SUBRESOURCE_FOOTPRINT
                                    {
                                        Offset = pBufToTexCopy->SourceOffset,
                                        Footprint = new D3D12_SUBRESOURCE_FOOTPRINT
                                        {
                                            Format = (DXGI_FORMAT)pBufToTexCopy->SourceFormat,
                                            Height = pBufToTexCopy->SourceHeight,
                                            Width = pBufToTexCopy->SourceWidth,
                                            Depth = pBufToTexCopy->SourceDepth,
                                            RowPitch = pBufToTexCopy->SourceRowPitch,
                                        }
                                    }
                                };

                                var dest = new D3D12_TEXTURE_COPY_LOCATION
                                {
                                    Type = D3D12_TEXTURE_COPY_TYPE.D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX,
                                    pResource = mapper.GetResourcePointer(pBufToTexCopy->Dest),
                                    SubresourceIndex = pBufToTexCopy->DestSubresource
                                };

                                pEncode->CopyTextureRegion(
                                    &dest,
                                    pBufToTexCopy->DestX,
                                    pBufToTexCopy->DestY,
                                    pBufToTexCopy->DestZ,
                                    &src,
                                    pBufToTexCopy->HasBox ? (D3D12_BOX*)pBufToTexCopy->Box : null
                                );

                                AdvanceVariableCommand(pBufToTexCopy, pBufToTexCopy->Box, pBufToTexCopy->HasBox ? 1 : 0);
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
                                D3D12_CLEAR_FLAGS flags = default;
                                flags |= pDepthClear->Flags.HasFlag(DepthStencilClearFlags.ClearStencil) ? D3D12_CLEAR_FLAGS.D3D12_CLEAR_FLAG_STENCIL : 0;
                                flags |= pDepthClear->Flags.HasFlag(DepthStencilClearFlags.ClearDepth) ? D3D12_CLEAR_FLAGS.D3D12_CLEAR_FLAG_DEPTH : 0;
                                pEncode->ClearDepthStencilView(mapper.GetInfo(pDepthClear->View).DepthStencil, 0, pDepthClear->Depth, pDepthClear->Stencil, pDepthClear->RectangleCount, (RECT*)(&pDepthClear->RectangleCount + sizeof(uint)));

                                AdvanceVariableCommand(pDepthClear, pDepthClear->Rectangles, pDepthClear->RectangleCount);
                                break;

                            case CommandType.ClearTextureInteger:
                                var pIntegerClear = &cmd->ClearTextureInteger;
                                view = mapper.GetInfo(pIntegerClear->View);

                                pEncode->ClearUnorderedAccessViewUint(
                                    mapper.GetShaderDescriptor(pIntegerClear->Descriptor),
                                    view.UnorderedAccess,
                                    view.Resource,
                                    pIntegerClear->ClearValue,
                                    pIntegerClear->RectangleCount,
                                    (RECT*)pIntegerClear->Rectangles
                                );


                                AdvanceVariableCommand(pIntegerClear, pIntegerClear->Rectangles, pIntegerClear->RectangleCount);
                                break;

                            case CommandType.ClearTexture:
                                var pClear = &cmd->ClearTexture;
                                view = mapper.GetInfo(pClear->View);

                                pEncode->ClearUnorderedAccessViewFloat(
                                    mapper.GetShaderDescriptor(pClear->Descriptor),
                                    view.UnorderedAccess,
                                    view.Resource,
                                    pClear->ClearValue,
                                    pClear->RectangleCount,
                                    (RECT*)pClear->Rectangles
                                );


                                AdvanceVariableCommand(pClear, pClear->Rectangles, pClear->RectangleCount);
                                break;

                            case CommandType.ClearBuffer:
                                var pClearBuffer = &cmd->ClearBuffer;
                                view = mapper.GetInfo(pClearBuffer->View);

                                pEncode->ClearUnorderedAccessViewFloat(
                                    mapper.GetShaderDescriptor(pClearBuffer->Descriptor),
                                    view.UnorderedAccess,
                                    view.Resource,
                                    pClearBuffer->ClearValue,
                                    0,
                                    null
                                );

                                AdvanceCommand(pClearBuffer);
                                break;

                            case CommandType.ClearBufferInteger:
                                var pClearBufferInteger = &cmd->ClearBufferInteger;
                                view = mapper.GetInfo(pClearBufferInteger->View);

                                pEncode->ClearUnorderedAccessViewUint(
                                    mapper.GetShaderDescriptor(pClearBufferInteger->Descriptor),
                                    view.UnorderedAccess,
                                    view.Resource,
                                    pClearBufferInteger->ClearValue,
                                    0,
                                    null
                                );

                                AdvanceCommand(pClearBufferInteger);
                                break;

                            case CommandType.SetIndexBuffer:
                                var pIndexBuffer = &cmd->SetIndexBuffer;

                                var indexView = new D3D12_INDEX_BUFFER_VIEW
                                {
                                    BufferLocation = mapper.GetResourcePointer(pIndexBuffer->Buffer)->GetGPUVirtualAddress(),
                                    Format = (DXGI_FORMAT)pIndexBuffer->Format,
                                    SizeInBytes = pIndexBuffer->Length
                                };

                                pEncode->IASetIndexBuffer(&indexView);

                                AdvanceCommand(pIndexBuffer);
                                break;

                            case CommandType.SetVertexBuffer:
                                var pVertexBuffers = &cmd->SetVertexBuffers;

                                for (i = 0; i < pVertexBuffers->Count; i++)
                                {
                                    var target = pVertexBuffers->Buffers[i];
                                    vertexBuffers[i] = new D3D12_VERTEX_BUFFER_VIEW
                                    {
                                        BufferLocation = mapper.GetInfo(target.Buffer).GpuAddress,
                                        StrideInBytes = target.Stride,
                                        SizeInBytes = target.Length
                                    };
                                }

                                pEncode->IASetVertexBuffers(pVertexBuffers->FirstBufferIndex, pVertexBuffers->Count, vertexBuffers);

                                AdvanceVariableCommand(pVertexBuffers, pVertexBuffers->Buffers, pVertexBuffers->Count);
                                break;

                            case CommandType.SetPipeline:
                                var pPipeline = &cmd->SetPipeline;
                                var pipeline = pPipeline->Pipeline;
                                pipelineState = mapper.GetInfo(pipeline);

                                if (ComPtr.TryQueryInterface(pipelineState.PipelineState, out ID3D12StateObject* state))
                                {
                                    pEncode->SetPipelineState1(state);
                                    state->Release();
                                }
                                else
                                {
                                    //Debug.Assert(state.HasInterface<ID3D12PipelineState>());
                                    pEncode->SetPipelineState((ID3D12PipelineState*)pipelineState.PipelineState);
                                }

                                var rootSig = pipelineState.RootSignature;
                                switch (pipelineState.BindPoint)
                                {
                                    case BindPoint.Graphics:
                                        pEncode->SetGraphicsRootSignature(rootSig);
                                        pEncode->IASetPrimitiveTopology(pipelineState.Topology);
                                        break;
                                    case BindPoint.Compute:
                                        pEncode->SetComputeRootSignature(rootSig);
                                        break;
                                    default:
                                        ThrowGraphicsException("bad bind point");
                                        break;
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
                                pEncode->RSSetShadingRateImage(mapper.GetResourcePointer(pShadingRateImage->ShadingRateImage));

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

                            case CommandType.BeginRenderPass:
                                var pBeginRenderPass = &cmd->BeginRenderPass;

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

                                D3D12_RENDER_PASS_DEPTH_STENCIL_DESC depthStencil;
                                _ = &depthStencil;


                                if (pBeginRenderPass->HasDepthStencil)
                                {
                                    var dsv = mapper.GetInfo(depthStencilDesc.View);
                                    var depthStencilClear = CreateDepthStencilClear(dsv.Format, depthStencilDesc.Depth, depthStencilDesc.Stencil);

                                    depthStencil = new()
                                    {
                                        cpuDescriptor = dsv.DepthStencil,
                                        DepthBeginningAccess = new D3D12_RENDER_PASS_BEGINNING_ACCESS
                                        {
                                            Type = (D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE)depthStencilDesc.DepthLoad,
                                        },
                                        DepthEndingAccess = new D3D12_RENDER_PASS_ENDING_ACCESS
                                        {
                                            Type = (D3D12_RENDER_PASS_ENDING_ACCESS_TYPE)depthStencilDesc.DepthStore
                                        },
                                        StencilBeginningAccess = new D3D12_RENDER_PASS_BEGINNING_ACCESS
                                        {
                                            Type = (D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE)depthStencilDesc.DepthLoad,
                                        },
                                        StencilEndingAccess = new D3D12_RENDER_PASS_ENDING_ACCESS
                                        {
                                            Type = (D3D12_RENDER_PASS_ENDING_ACCESS_TYPE)depthStencilDesc.DepthStore
                                        },
                                    };

                                    if (depthStencilDesc.DepthLoad == LoadOperation.Clear)
                                    {
                                        depthStencil.DepthBeginningAccess.Clear = depthStencilClear;
                                    }
                                    if (depthStencilDesc.StencilLoad == LoadOperation.Clear)
                                    {
                                        depthStencil.StencilBeginningAccess.Clear = depthStencilClear;
                                    }
                                }


                                uint width = 0, height = 0;

                                // see top of method for renderTargets stackalloc
                                for (i = 0; i < pBeginRenderPass->RenderTargetCount; i++)
                                {
                                    var target = pBeginRenderPass->RenderTargets[i];
                                    var targetInfo = mapper.GetInfo(target.View);

                                    width = Math.Max(width, targetInfo.Width);
                                    height = Math.Max(height, targetInfo.Height);

                                    var load = target.Load;
                                    var store = target.Store;

                                    renderTargets[i] = new D3D12_RENDER_PASS_RENDER_TARGET_DESC
                                    {
                                        cpuDescriptor = targetInfo.RenderTarget,
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
                                    pBeginRenderPass->HasDepthStencil ? &depthStencil : null,
                                    pBeginRenderPass->AllowTextureWrites ? D3D12_RENDER_PASS_FLAGS.D3D12_RENDER_PASS_FLAG_ALLOW_UAV_WRITES : 0
                                );


                                var fullTargetRect = new RECT
                                {
                                    top = 0,
                                    left = 0,
                                    bottom = (int)height,
                                    right = (int)width
                                };

                                var fullTargetViewport = new D3D12_VIEWPORT
                                {
                                    MaxDepth = 1,
                                    MinDepth = 0,
                                    Height = height,
                                    Width = width,
                                    TopLeftX = 0,
                                    TopLeftY = 0
                                };

                                pEncode->RSSetScissorRects(1, &fullTargetRect);
                                pEncode->RSSetViewports(1, &fullTargetViewport);

                                AdvanceVariableCommand(pBeginRenderPass, pBeginRenderPass->RenderTargets, pBeginRenderPass->RenderTargetCount);
                                break;

                            case CommandType.EndRenderPass:
                                pEncode->EndRenderPass();

                                AdvanceEmptyCommand();
                                break;

                            case CommandType.ReadTimestamp:
                                var pReadTimestamp = &cmd->ReadTimestamp;
                                pEncode->EndQuery(
                                    mapper.GetInfo(pReadTimestamp->QueryHeap).QueryHeap,
                                    D3D12_QUERY_TYPE.D3D12_QUERY_TYPE_TIMESTAMP,
                                    pReadTimestamp->Index
                                );

                                AdvanceCommand(pReadTimestamp);
                                break;

                            case CommandType.BeginQuery:
                                var pBeginQuery = &cmd->BeginQuery;
                                pEncode->BeginQuery(
                                    mapper.GetInfo(pBeginQuery->QueryHeap).QueryHeap,
                                    (D3D12_QUERY_TYPE)pBeginQuery->QueryType,
                                    pBeginQuery->Index
                                );

                                AdvanceCommand(pBeginQuery);
                                break;

                            case CommandType.EndQuery:
                                var pEndQuery = &cmd->EndQuery;
                                pEncode->EndQuery(
                                    mapper.GetInfo(pEndQuery->QueryHeap).QueryHeap,
                                    (D3D12_QUERY_TYPE)pEndQuery->QueryType,
                                    pEndQuery->Index
                                );

                                AdvanceCommand(pEndQuery);
                                break;

                            case CommandType.BeginConditionalRendering:
                                var pConditionalRender = &cmd->BeginConditionalRendering;
                                pEncode->SetPredication(
                                    mapper.GetResourcePointer(pConditionalRender->Buffer),
                                    pConditionalRender->Offset,
                                    pConditionalRender->Predicate ? D3D12_PREDICATION_OP.D3D12_PREDICATION_OP_NOT_EQUAL_ZERO : D3D12_PREDICATION_OP.D3D12_PREDICATION_OP_EQUAL_ZERO
                                );

                                AdvanceCommand(pConditionalRender);
                                break;

                            case CommandType.EndConditionalRendering:
                                pEncode->SetPredication(null, 0, 0);

                                AdvanceEmptyCommand();
                                break;


                            case CommandType.InsertMarker:
                                var pInsertMarker = &cmd->InsertMarker;
                                pEncode->SetMarker(D3D12_EVENT_METADATA, pInsertMarker->Data, pInsertMarker->Length);

                                AdvanceVariableCommand(pInsertMarker, pInsertMarker->Data, pInsertMarker->Length);
                                break;

                            case CommandType.BeginEvent:
                                var pBeginEvent = &cmd->BeginEvent;
                                pEncode->BeginEvent(D3D12_EVENT_METADATA, pBeginEvent->Data, pBeginEvent->Length);

                                AdvanceVariableCommand(pBeginEvent, pBeginEvent->Data, pBeginEvent->Length);
                                break;

                            case CommandType.EndEvent:
                                pEncode->EndEvent();

                                AdvanceEmptyCommand();
                                break;

                            case CommandType.CopyAccelerationStructure:
                                ThrowHelper.ThrowNotImplementedException();
                                break;
                            case CommandType.BuildAccelerationStructure:
                                ThrowHelper.ThrowNotImplementedException();
                                break;

                            case CommandType.ExecuteIndirect:
                                var pExecuteIndirect = &cmd->ExecuteIndirect;

                                var info = mapper.GetInfo(pExecuteIndirect->IndirectCommand);


                                var hasGpuCount = pExecuteIndirect->HasCountSpecifier;

                                pEncode->ExecuteIndirect(
                                    info.IndirectCommand,
                                    pExecuteIndirect->Count,
                                    mapper.GetResourcePointer(pExecuteIndirect->Arguments),
                                    pExecuteIndirect->Offset,
                                    hasGpuCount ? mapper.GetResourcePointer(pExecuteIndirect->CountSpecifier->CountBuffer) : null,
                                    hasGpuCount ? pExecuteIndirect->CountSpecifier->Offset : 0
                                );

                                AdvanceVariableCommand(pExecuteIndirect, pExecuteIndirect->CountSpecifier, hasGpuCount ? 1 : 0);
                                break;

                            case CommandType.CompactAccelerationStructure:
                                ThrowHelper.ThrowNotImplementedException();
                                break;
                            case CommandType.SerializeAccelerationStructure:
                                ThrowHelper.ThrowNotImplementedException();
                                break;
                            case CommandType.DeserializeAccelerationStructure:
                                ThrowHelper.ThrowNotImplementedException();
                                break;

                            case CommandType.WriteConstants:
                                var pWriteConstants = &cmd->WriteConstants;

                                pEncode->WriteBufferImmediate(
                                    (uint)pWriteConstants->Count,
                                    (D3D12_WRITEBUFFERIMMEDIATE_PARAMETER*)pWriteConstants->Parameters,
                                    (D3D12_WRITEBUFFERIMMEDIATE_MODE*)pWriteConstants->Modes
                                );

                                AdvanceVariableCommand2(pWriteConstants, pWriteConstants->Parameters, pWriteConstants->Modes, (uint)pWriteConstants->Count);
                                break;

                            case CommandType.ResolveQuery:
                                var pResolveQuery = &cmd->ResolveQuery;

                                var queryHeap = mapper.GetInfo(pResolveQuery->QueryHeap);

                                var (start, count) = pResolveQuery->Queries.GetOffsetAndLength((int)queryHeap.Length);

                                pEncode->ResolveQueryData(
                                    queryHeap.QueryHeap,
                                    (D3D12_QUERY_TYPE)pResolveQuery->QueryType,
                                    (uint)start,
                                    (uint)count,
                                    mapper.GetResourcePointer(pResolveQuery->Dest),
                                    pResolveQuery->Offset
                                );

                                AdvanceCommand(pResolveQuery);
                                break;

                            case CommandType.Transition:
                                var pTransition = &cmd->Transitions;

                                i = 0U;
                                for (; i < pTransition->Count; i += MaxBarrierCount)
                                {
                                    j = 0u;
                                    for (; j < MaxBarrierCount && j < pTransition->Count; j++)
                                    {
                                        ref readonly var transition = ref pTransition->Transitions[i + j];
                                        barriers[j] = D3D12_RESOURCE_BARRIER.InitTransition(
                                            mapper.GetResourcePointer(transition.Resource),
                                            (D3D12_RESOURCE_STATES)transition.Before,
                                            (D3D12_RESOURCE_STATES)transition.After,
                                            transition.Subresource
                                        );
                                    }

                                    pEncode->ResourceBarrier(
                                        j,
                                        barriers
                                    );
                                }

                                AdvanceVariableCommand(pTransition, pTransition->Transitions, pTransition->Count);
                                break;

                            case CommandType.WriteBarrier:
                                var pWriteBarrier = &cmd->WriteBarriers;

                                i = 0U;
                                for (; i < pWriteBarrier->Count; i += MaxBarrierCount)
                                {
                                    j = 0u;
                                    for (; j < MaxBarrierCount && j < pWriteBarrier->Count; j++)
                                    {
                                        barriers[j] = D3D12_RESOURCE_BARRIER.InitUAV(mapper.GetResourcePointer(pWriteBarrier->Resources[i + j]));
                                    }

                                    pEncode->ResourceBarrier(
                                        j,
                                        barriers
                                    );
                                }

                                AdvanceVariableCommand(pWriteBarrier, pWriteBarrier->Resources, pWriteBarrier->Count);
                                break;

                            case CommandType.AliasingBarrier:
                                var pAliasing = &cmd->AliasingBarriers;

                                i = 0U;
                                for (; i < pAliasing->Count; i += MaxBarrierCount)
                                {
                                    j = 0u;
                                    for (; j < MaxBarrierCount && j < pAliasing->Count; j++)
                                    {
                                        ref readonly var aliasing = ref pAliasing->Resources[i + j];
                                        barriers[j] = D3D12_RESOURCE_BARRIER.InitAliasing(
                                            mapper.GetResourcePointer(aliasing.Before),
                                            mapper.GetResourcePointer(aliasing.After)
                                        );
                                    }

                                    pEncode->ResourceBarrier(
                                        j,
                                        barriers
                                    );
                                }

                                AdvanceVariableCommand(pAliasing, pAliasing->Resources, pAliasing->Count);
                                break;

                            case CommandType.SetViewports:
                                var pSetViewports = &cmd->SetViewports;

                                pEncode->RSSetViewports(
                                    pSetViewports->Count,
                                    (D3D12_VIEWPORT*)pSetViewports->Viewports
                                );

                                AdvanceVariableCommand(pSetViewports, pSetViewports->Viewports, pSetViewports->Count);
                                break;

                            case CommandType.SetScissorRectangles:
                                var pScissorRectangles = &cmd->SetScissorRectangles;

                                pEncode->RSSetScissorRects(
                                    pScissorRectangles->Count,
                                    (RECT*)pScissorRectangles->Rectangles
                                );

                                AdvanceVariableCommand(pScissorRectangles, pScissorRectangles->Rectangles, pScissorRectangles->Count);
                                break;
                            default:
                                ThrowHelper.ThrowNotSupportedException($"Command '{cmd->Type}' is unsupported");
                                break;

                                //static bool IsSafeToFpRecast(Vector128<float> pVal) => Vector.IsFinite(pVal).AllTrue();
                        }

                        // pervert peek command
                        //CommandType* PeekCommand<T>(T* pVal)
                        //    where T : unmanaged => (CommandType*)(&cmd->Arguments + sizeof(T));




                        //CommandType* PeekVariableCommand<T, TVariable>(T* pVal, TVariable* pVariable, uint pVariableCount)
                        //    where T : unmanaged where TVariable : unmanaged => (CommandType*)(&cmd->Arguments + sizeof(T) + (sizeof(TVariable) * pVariableCount));

                        //CommandType* PeekVariableCommand2<T, TVariable1, TVariable2>(T* pVal, TVariable1* pVariable1, TVariable2* pVariable2, uint pVariableCount)
                        //    where T : unmanaged where TVariable1 : unmanaged where TVariable2 : unmanaged => (CommandType*)(&cmd->Arguments + sizeof(T) + (sizeof(TVariable1) * pVariableCount) + (sizeof(TVariable2) * pVariableCount));

                        //CommandType* PeekEmptyCommand() => (CommandType*)&cmd->Arguments;


                        const int CommandAlignment = sizeof(CommandType);

                        void AdvanceCommand<T>(T* pVal)
                            where T : unmanaged => pPacketStart = MathHelpers.AlignUp(&cmd->Arguments + sizeof(T), CommandAlignment);

                        void AdvanceVariableCommand<T, TVariable>(T* pVal, TVariable* pVariable, uint pVariableCount)
                            where T : unmanaged where TVariable : unmanaged => pPacketStart = MathHelpers.AlignUp(&cmd->Arguments + sizeof(T) + (sizeof(TVariable) * pVariableCount), CommandAlignment);

                        void AdvanceVariableCommand2<T, TVariable1, TVariable2>(T* pVal, TVariable1* pVariable1, TVariable2* pVariable2, uint pVariableCount)
                            where T : unmanaged where TVariable1 : unmanaged where TVariable2 : unmanaged => pPacketStart = MathHelpers.AlignUp(&cmd->Arguments + sizeof(T) + (sizeof(TVariable1) * pVariableCount) + (sizeof(TVariable2) * pVariableCount), CommandAlignment);

                        void AdvanceEmptyCommand() => pPacketStart = &cmd->Arguments;
                    }
                }
            }
        }

        private static void ThrowGraphicsException(string _)
        {
        }
    }
}
