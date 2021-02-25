using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using MathSharp;
using Microsoft.Collections.Extensions;
using SixLabors.ImageSharp;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Common.Pix;
using Voltium.Core.CommandBuffer;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;
using Voltium.Core.Queries;
using Vector = MathSharp.Vector;

namespace Voltium.Core.Contexts
{
    internal static class ContextEncoder
    {
        public static ContextEncoder<TBufferWriter> Create<TBufferWriter>(TBufferWriter writer) where TBufferWriter : IBufferWriter<byte> => new(writer);
    }

    internal struct ContextEncoder<TBufferWriter> where TBufferWriter : IBufferWriter<byte>
    {
        private TBufferWriter _writer;

        public ContextEncoder(TBufferWriter writer) => _writer = writer;

        public unsafe void EmitVariable<TCommand, TVariable1, TVariable2>(TCommand* value, TVariable1* pVariable1, TVariable2* pVariable2, uint variableCount)
            where TCommand : unmanaged, ICommand
            where TVariable1 : unmanaged
            where TVariable2 : unmanaged
        {
            var commandLength = MathHelpers.AlignUp(sizeof(CommandType) + sizeof(TCommand) + (sizeof(TVariable1) * (int)variableCount) + (sizeof(TVariable2) * (int)variableCount), Command.Alignment);
            var buffer = _writer.GetSpan(commandLength);

            ref var start = ref buffer[0];
            Unsafe.As<byte, CommandType>(ref start) = value->Type;
            Unsafe.As<byte, TCommand>(ref Unsafe.Add(ref start, sizeof(CommandType))) = *value;
            Unsafe.CopyBlockUnaligned(ref Unsafe.Add(ref start, sizeof(CommandType) + sizeof(TCommand)), ref *(byte*)pVariable1, (uint)sizeof(TVariable1) * variableCount);
            Unsafe.CopyBlockUnaligned(ref Unsafe.Add(ref start, sizeof(CommandType) + sizeof(TCommand)), ref *(byte*)pVariable2, (uint)sizeof(TVariable2) * variableCount);

            _writer.Advance(commandLength);
        }

        public unsafe void EmitVariable<TCommand, TVariable>(TCommand* value, TVariable* pVariable, uint variableCount)
            where TCommand : unmanaged, ICommand
            where TVariable : unmanaged
        {
            var commandLength = MathHelpers.AlignUp(sizeof(CommandType) + sizeof(TCommand) + (sizeof(TVariable) * (int)variableCount), Command.Alignment);
            var buffer = _writer.GetSpan(commandLength);

            ref var start = ref buffer[0];
            Unsafe.As<byte, CommandType>(ref start) = value->Type;
            Unsafe.As<byte, TCommand>(ref Unsafe.Add(ref start, sizeof(CommandType))) = *value;
            Unsafe.CopyBlockUnaligned(ref Unsafe.Add(ref start, sizeof(CommandType) + sizeof(TCommand)), ref *(byte*)pVariable, (uint)sizeof(TVariable) * variableCount);
            _writer.Advance(commandLength);
        }

        public unsafe void Emit<TCommand>(TCommand* value) where TCommand : unmanaged, ICommand
        {
            var commandLength = MathHelpers.AlignUp(sizeof(CommandType) + sizeof(TCommand), Command.Alignment);
            var buffer = _writer.GetSpan(commandLength);

            ref var start = ref buffer[0];
            Unsafe.As<byte, CommandType>(ref start) = value->Type;
            Unsafe.As<byte, TCommand>(ref Unsafe.Add(ref start, sizeof(CommandType))) = *value;
            _writer.Advance(commandLength);
        }

        public unsafe void EmitEmpty(CommandType command)
        {
            var commandLength = sizeof(CommandType);
            var buffer = _writer.GetSpan(commandLength);

            ref var start = ref buffer[0];
            Unsafe.As<byte, CommandType>(ref start) = command;
            _writer.Advance(commandLength);
        }
    }


    internal interface IHandleMapper
    {

    }

    internal unsafe struct D3D12View
    {
        public ID3D12Resource* Resource;
        public DXGI_FORMAT Format;
        public D3D12_CPU_DESCRIPTOR_HANDLE DepthStencil, RenderTarget, UnorderedAccess, ShaderResource, ConstantBuffer;
    }

    internal unsafe struct D3D12Buffer
    {
        public ID3D12Resource* Buffer;
        public D3D12_RESOURCE_FLAGS Flags;
        public ulong Address;
        public ulong Length;
    }

    internal unsafe struct D3D12Texture
    {
        public ID3D12Resource* Texture;
        public DXGI_FORMAT Format;
        public D3D12_RESOURCE_FLAGS Flags;
    }

    internal unsafe struct D3D12RaytracingAccelerationStructure
    {
        public ID3D12Resource* RaytracingAccelerationStructure;
        public ulong Address;
        public ulong Length;
    }

    internal unsafe struct D3D12QueryHeap
    {
        public ID3D12QueryHeap* QueryHeap;
        public uint Length;
    }

    internal unsafe struct D3D12RenderPass
    {
        public ID3D12QueryHeap* QueryHeap;
    }
    internal unsafe struct D3D12PipelineState
    {
        public ID3D12Object* PipelineState;
        public ID3D12RootSignature* RootSignature;
        public BindPoint BindPoint;
        public ImmutableArray<RootParameter> RootParameters;
    }

    internal unsafe struct D3D12Heap
    {
        public ID3D12Heap* Heap;
        public D3D12_HEAP_PROPERTIES Properties;
        public D3D12_HEAP_FLAGS Flags;
        public ulong Alignment;
        public ulong Length;
    }


    internal unsafe struct D3D12RootSignature
    {
        public ID3D12RootSignature* RootSignature;
        public ImmutableArray<RootParameter> RootParameters;
    }
    internal unsafe struct D3D12IndirectCommand
    {
        public ID3D12CommandSignature* IndirectCommand;
        public ID3D12RootSignature* RootSignature;
        public uint ByteStride;
    }



    internal unsafe struct D3D12ViewSet
    {
        public ID3D12DescriptorHeap* RenderTarget, DepthStencil, ShaderResources;
        public D3D12_CPU_DESCRIPTOR_HANDLE FirstRenderTarget, FirstDepthStencil, FirstShaderResources;
        public uint Length;
    }

    internal unsafe struct GenerationalHandleMapper : IHandleMapper
    {
        public GenerationalHandleMapper(bool _)
        {
            const int capacity = 32;
            _buffers = new(capacity);
            _textures = new(capacity);
            _accelarationStructures = new(capacity);
            _queryHeaps = new(capacity);
            _renderPasses = new(capacity);
            _pipelines = new(capacity);
            _viewSets = new(capacity);
            _views = new(capacity);
            _heaps = new(capacity);
            _indirectCommands = new(capacity);
            _signatures = new(capacity);
        }

        private GenerationHandleAllocator<BufferHandle, D3D12Buffer> _buffers;
        private GenerationHandleAllocator<TextureHandle, D3D12Texture> _textures;
        private GenerationHandleAllocator<RaytracingAccelerationStructureHandle, D3D12RaytracingAccelerationStructure> _accelarationStructures;
        private GenerationHandleAllocator<QuerySetHandle, D3D12QueryHeap> _queryHeaps;
        private GenerationHandleAllocator<RenderPassHandle, D3D12RenderPass> _renderPasses;
        private GenerationHandleAllocator<RootSignatureHandle, D3D12RootSignature> _signatures;
        private GenerationHandleAllocator<PipelineHandle, D3D12PipelineState> _pipelines;
        private GenerationHandleAllocator<ViewSetHandle, D3D12ViewSet> _viewSets;
        private GenerationHandleAllocator<ViewHandle, D3D12View> _views;
        private GenerationHandleAllocator<HeapHandle, D3D12Heap> _heaps;
        private GenerationHandleAllocator<IndirectCommandHandle, D3D12IndirectCommand> _indirectCommands;

        public D3D12Buffer GetAndFree(BufferHandle handle) => _buffers.GetAndFreeHandle(handle);
        public D3D12Texture GetAndFree(TextureHandle handle) => _textures.GetAndFreeHandle(handle);
        public D3D12RaytracingAccelerationStructure GetAndFree(RaytracingAccelerationStructureHandle handle) => _accelarationStructures.GetAndFreeHandle(handle);
        public D3D12QueryHeap GetAndFree(QuerySetHandle handle) => _queryHeaps.GetAndFreeHandle(handle);
        public D3D12RenderPass GetAndFree(RenderPassHandle handle) => _renderPasses.GetAndFreeHandle(handle);
        public D3D12RootSignature GetAndFree(RootSignatureHandle handle) => _signatures.GetAndFreeHandle(handle);
        public D3D12PipelineState GetAndFree(PipelineHandle handle) => _pipelines.GetAndFreeHandle(handle);
        public D3D12View GetAndFree(ViewHandle handle) => _views.GetAndFreeHandle(handle);
        public D3D12ViewSet GetAndFree(ViewSetHandle handle) => _viewSets.GetAndFreeHandle(handle);
        public D3D12Heap GetAndFree(HeapHandle handle) => _heaps.GetAndFreeHandle(handle);
        public D3D12IndirectCommand GetAndFree(IndirectCommandHandle handle) => _indirectCommands.GetAndFreeHandle(handle);

        public ViewSetHandle Create(in D3D12ViewSet data) => _viewSets.AllocateHandle(data);
        public BufferHandle Create(in D3D12Buffer data) => _buffers.AllocateHandle(data);
        public TextureHandle Create(in D3D12Texture data) => _textures.AllocateHandle(data);
        public RaytracingAccelerationStructureHandle Create(in D3D12RaytracingAccelerationStructure data) => _accelarationStructures.AllocateHandle(data);
        public QuerySetHandle Create(in D3D12QueryHeap data) => _queryHeaps.AllocateHandle(data);
        public RenderPassHandle Create(in D3D12RenderPass data) => _renderPasses.AllocateHandle(data);
        public PipelineHandle Create(in D3D12PipelineState data) => _pipelines.AllocateHandle(data);
        public ViewHandle Create(in D3D12View data) => _views.AllocateHandle(data);
        public HeapHandle Create(in D3D12Heap data) => _heaps.AllocateHandle(data);
        public IndirectCommandHandle Create(in D3D12IndirectCommand data) => _indirectCommands.AllocateHandle(data);
        public RootSignatureHandle Create(in D3D12RootSignature handle) => _signatures.AllocateHandle(handle);

        public D3D12IndirectCommand GetInfo(IndirectCommandHandle handle) => _indirectCommands.GetHandleData(handle);
        public D3D12View GetInfo(ViewHandle handle) => _views.GetHandleData(handle);
        public D3D12ViewSet GetInfo(ViewSetHandle handle) => _viewSets.GetHandleData(handle);
        public D3D12QueryHeap GetInfo(QuerySetHandle handle) => _queryHeaps.GetHandleData(handle);
        public D3D12PipelineState GetInfo(PipelineHandle handle) => _pipelines.GetHandleData(handle);
        public D3D12Buffer GetInfo(BufferHandle handle) => _buffers.GetHandleData(handle);
        public D3D12Texture GetInfo(TextureHandle handle) => _textures.GetHandleData(handle);
        public D3D12RaytracingAccelerationStructure GetInfo(RaytracingAccelerationStructureHandle handle) => _accelarationStructures.GetHandleData(handle);
        public D3D12Heap GetInfo(HeapHandle handle) => _heaps.GetHandleData(handle);
        public D3D12RootSignature GetInfo(RootSignatureHandle handle) => _signatures.GetAndFreeHandle(handle);

        public ID3D12Resource* GetResourcePointer(in ResourceHandle handle) => handle.Type switch
        {
            ResourceHandleType.Buffer => GetResourcePointer(handle.Buffer),
            ResourceHandleType.Texture => GetResourcePointer(handle.Texture),
            ResourceHandleType.RaytracingAccelerationStructure => GetResourcePointer(handle.RaytracingAccelerationStructure),

            /* oh fizzlesticks */ _ => null
        };



        public ID3D12Resource* GetResourcePointer(BufferHandle handle) => _buffers.GetHandleData(handle).Buffer;
        public ID3D12Resource* GetResourcePointer(TextureHandle handle) => _textures.GetHandleData(handle).Texture;
        public ID3D12Resource* GetResourcePointer(RaytracingAccelerationStructureHandle handle) => _accelarationStructures.GetHandleData(handle).RaytracingAccelerationStructure;

        public D3D12_GPU_DESCRIPTOR_HANDLE GetShaderDescriptor(DescriptorHandle handle) => handle.GpuHandle;
    }


    internal interface IDecoderContext
    {

    }

    internal abstract partial class CommandBufferDecoder<TDecoder, TDecoderContext>
        where TDecoder : unmanaged, IDecoderContext
        where TDecoderContext : unmanaged
    {
        // Each command in CommandType has a method generated for it
        // E.g
        // CommandType.DrawInstanced becomes
        // private abstract void VisitDrawInstanced(TDecoderContext* pContext, CommandDrawInstanced* pDrawInstanced);
        // Then, the command buffer walker is generated seperately for each child type, which removes the cost of constant virtual calls
        // you get in a standard visitor pattern
    }


    internal unsafe sealed class D3D12ContextDecoder<THandleMapper> where THandleMapper : struct, IHandleMapper
    {
        private ComputeDevice _device;
        private GenerationalHandleMapper _mapper;

        public D3D12ContextDecoder(ComputeDevice device)
        {
            _device = device;
        }


        public const uint WIN_EVENT_3BLOB_VERSION = 2;
        public const uint D3D12_EVENT_METADATA = WIN_EVENT_3BLOB_VERSION;


        public enum StateKey
        {
            Pipeline,
            StencilRef,
            BlendFactor,
            DepthBounds,
            Topology,
            ShadingRate,
            ShadingRateImage,
            SamplePositions,
            ViewInstanceMask,
            Viewports,
            Scissors,
            IndexBuffer,
            VertexBuffers,
            Constants
        }

        private struct StateTracker
        {
            public (PipelineHandle handle, PipelineStateObject Object) Pso;
        }

        private StateTracker _tracker;

        public void Encode(ReadOnlySpan<byte> buff, ID3D12GraphicsCommandList6* pEncode)
        {
            const int MaxBarrierCount = 32;

            // for BeginRenderPass. No stackalloc in loops remember!
            var renderTargets = stackalloc D3D12_RENDER_PASS_RENDER_TARGET_DESC[8];
            var vertexBuffers = stackalloc D3D12_VERTEX_BUFFER_VIEW[8];
            var barriers = stackalloc D3D12_RESOURCE_BARRIER[MaxBarrierCount];


            D3D12PipelineState pipelineState = default;

            D3D12View view;
            uint i, j;

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
                                    _device.ThrowGraphicsException("Invalid bind point?");
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
                                        pEncode->SetGraphicsRootDescriptorTable(i, pBindDescriptors->Sets[i].GpuHandle);
                                    }
                                    break;
                                case BindPoint.Compute:
                                    for (i = 0u; i < pBindDescriptors->SetCount; i++)
                                    {
                                        pEncode->SetComputeRootDescriptorTable(i, pBindDescriptors->Sets[i].GpuHandle);
                                    }
                                    break;
                                default:
                                    _device.ThrowGraphicsException("Invalid bind point?");
                                    break;
                            }

                            AdvanceVariableCommand(pBindDescriptors, pBindDescriptors->Sets, pBindDescriptors->SetCount);
                            break;

                        case CommandType.BufferCopy:
                            var pCopy = &cmd->BufferCopy;
                            pEncode->CopyBufferRegion(_mapper.GetResourcePointer(pCopy->Dest), pCopy->DestOffset, _mapper.GetResourcePointer(pCopy->Source), pCopy->SourceOffset, pCopy->Length);

                            AdvanceCommand(pCopy);
                            break;

                        case CommandType.TextureCopy:
                            break;

                        case CommandType.BufferToTextureCopy:
                            var pBufToTexCopy = &cmd->BufferToTextureCopy;

                            var src = new D3D12_TEXTURE_COPY_LOCATION
                            {
                                Type = D3D12_TEXTURE_COPY_TYPE.D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT,
                                pResource = _mapper.GetResourcePointer(pBufToTexCopy->Source),
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
                                pResource = _mapper.GetResourcePointer(pBufToTexCopy->Dest),
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
                            pEncode->ClearDepthStencilView(_mapper.GetInfo(pDepthClear->View).DepthStencil, 0, pDepthClear->Depth, pDepthClear->Stencil, pDepthClear->RectangleCount, (RECT*)(&pDepthClear->RectangleCount + sizeof(uint)));

                            AdvanceVariableCommand(pDepthClear, pDepthClear->Rectangles, pDepthClear->RectangleCount);
                            break;

                        case CommandType.ClearTextureInteger:
                            var pIntegerClear = &cmd->ClearTextureInteger;
                            view = _mapper.GetInfo(pIntegerClear->View);

                            pEncode->ClearUnorderedAccessViewUint(
                                _mapper.GetShaderDescriptor(pIntegerClear->Descriptor),
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
                            view = _mapper.GetInfo(pClear->View);

                            pEncode->ClearUnorderedAccessViewFloat(
                                _mapper.GetShaderDescriptor(pClear->Descriptor),
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
                            view = _mapper.GetInfo(pClearBuffer->View);

                            pEncode->ClearUnorderedAccessViewFloat(
                                _mapper.GetShaderDescriptor(pClearBuffer->Descriptor),
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
                            view = _mapper.GetInfo(pClearBufferInteger->View);

                            pEncode->ClearUnorderedAccessViewUint(
                                _mapper.GetShaderDescriptor(pClearBufferInteger->Descriptor),
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
                                BufferLocation = _mapper.GetResourcePointer(pIndexBuffer->Buffer)->GetGPUVirtualAddress(),
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
                                    BufferLocation = _mapper.GetInfo(target.Buffer).Address,
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
                            pipelineState = _mapper.GetInfo(pipeline);

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
                                    break;
                                case BindPoint.Compute:
                                    pEncode->SetComputeRootSignature(rootSig);
                                    break;
                                default:
                                    _device.ThrowGraphicsException("bad bind point");
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
                            pEncode->RSSetShadingRateImage(_mapper.GetResourcePointer(pShadingRateImage->ShadingRateImage));

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
                                var dsv = _mapper.GetInfo(depthStencilDesc.View);
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

                            // see top of method for renderTargets stackalloc
                            for (i = 0; i < pBeginRenderPass->RenderTargetCount; i++)
                            {
                                var target = pBeginRenderPass->RenderTargets[i];

                                var load = target.Load;
                                var store = target.Store;

                                renderTargets[i] = new D3D12_RENDER_PASS_RENDER_TARGET_DESC
                                {
                                    cpuDescriptor = _mapper.GetInfo(target.View).RenderTarget,
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

                            AdvanceVariableCommand(pBeginRenderPass, pBeginRenderPass->RenderTargets, pBeginRenderPass->RenderTargetCount);
                            break;

                        case CommandType.EndRenderPass:
                            pEncode->EndRenderPass();

                            AdvanceEmptyCommand();
                            break;

                        case CommandType.ReadTimestamp:
                            var pReadTimestamp = &cmd->ReadTimestamp;
                            pEncode->EndQuery(
                                _mapper.GetInfo(pReadTimestamp->QueryHeap).QueryHeap,
                                D3D12_QUERY_TYPE.D3D12_QUERY_TYPE_TIMESTAMP,
                                pReadTimestamp->Index
                            );

                            AdvanceCommand(pReadTimestamp);
                            break;

                        case CommandType.BeginQuery:
                            var pBeginQuery = &cmd->BeginQuery;
                            pEncode->BeginQuery(
                                _mapper.GetInfo(pBeginQuery->QueryHeap).QueryHeap,
                                (D3D12_QUERY_TYPE)pBeginQuery->Type,
                                pBeginQuery->Index
                            );

                            AdvanceCommand(pBeginQuery);
                            break;

                        case CommandType.EndQuery:
                            var pEndQuery = &cmd->EndQuery;
                            pEncode->EndQuery(
                                _mapper.GetInfo(pEndQuery->QueryHeap).QueryHeap,
                                (D3D12_QUERY_TYPE)pEndQuery->Type,
                                pEndQuery->Index
                            );

                            AdvanceCommand(pEndQuery);
                            break;

                        case CommandType.BeginConditionalRendering:
                            var pConditionalRender = &cmd->BeginConditionalRendering;
                            pEncode->SetPredication(
                                _mapper.GetResourcePointer(pConditionalRender->Buffer),
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

                            var info = _mapper.GetInfo(pExecuteIndirect->IndirectCommand);


                            var hasGpuCount = pExecuteIndirect->HasCountSpecifier;

                            pEncode->ExecuteIndirect(
                                info.IndirectCommand,
                                pExecuteIndirect->Count,
                                _mapper.GetResourcePointer(pExecuteIndirect->Arguments),
                                pExecuteIndirect->Offset,
                                hasGpuCount ? _mapper.GetResourcePointer(pExecuteIndirect->CountSpecifier->CountBuffer) : null,
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

                            var queryHeap = _mapper.GetInfo(pResolveQuery->QueryHeap);

                            var (start, count) = pResolveQuery->Queries.GetOffsetAndLength((int)queryHeap.Length);

                            pEncode->ResolveQueryData(
                                queryHeap.QueryHeap,
                                (D3D12_QUERY_TYPE)pResolveQuery->QueryType,
                                (uint)start,
                                (uint)count,
                                _mapper.GetResourcePointer(pResolveQuery->Dest),
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
                                        _mapper.GetResourcePointer(transition.Resource),
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
                                    barriers[j] = D3D12_RESOURCE_BARRIER.InitUAV(_mapper.GetResourcePointer(pWriteBarrier->Resources[i + j]));
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
                                        _mapper.GetResourcePointer(aliasing.Before),
                                        _mapper.GetResourcePointer(aliasing.After)
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
}
