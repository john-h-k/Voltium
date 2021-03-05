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
        public TBufferWriter Writer;

        public ContextEncoder(TBufferWriter writer) => Writer = writer;

        public unsafe void EmitVariable<TCommand, TVariable1, TVariable2>(TCommand* value, TVariable1* pVariable1, TVariable2* pVariable2, uint variableCount)
            where TCommand : unmanaged, ICommand
            where TVariable1 : unmanaged
            where TVariable2 : unmanaged
        {
            var commandLength = MathHelpers.AlignUp(sizeof(CommandType) + sizeof(TCommand) + (sizeof(TVariable1) * (int)variableCount) + (sizeof(TVariable2) * (int)variableCount), Command.Alignment);
            var buffer = Writer.GetSpan(commandLength);

            ref var start = ref buffer[0];
            Unsafe.As<byte, CommandType>(ref start) = value->Type;
            Unsafe.As<byte, TCommand>(ref Unsafe.Add(ref start, sizeof(CommandType))) = *value;
            Unsafe.CopyBlockUnaligned(ref Unsafe.Add(ref start, sizeof(CommandType) + sizeof(TCommand)), ref *(byte*)pVariable1, (uint)sizeof(TVariable1) * variableCount);
            Unsafe.CopyBlockUnaligned(ref Unsafe.Add(ref start, sizeof(CommandType) + sizeof(TCommand)), ref *(byte*)pVariable2, (uint)sizeof(TVariable2) * variableCount);

            Writer.Advance(commandLength);
        }

        public unsafe void EmitVariable<TCommand, TVariable>(TCommand* value, TVariable* pVariable, uint variableCount)
            where TCommand : unmanaged, ICommand
            where TVariable : unmanaged
        {
            var commandLength = MathHelpers.AlignUp(sizeof(CommandType) + sizeof(TCommand) + (sizeof(TVariable) * (int)variableCount), Command.Alignment);
            var buffer = Writer.GetSpan(commandLength);

            ref var start = ref buffer[0];
            Unsafe.As<byte, CommandType>(ref start) = value->Type;
            Unsafe.As<byte, TCommand>(ref Unsafe.Add(ref start, sizeof(CommandType))) = *value;
            Unsafe.CopyBlockUnaligned(ref Unsafe.Add(ref start, sizeof(CommandType) + sizeof(TCommand)), ref *(byte*)pVariable, (uint)sizeof(TVariable) * variableCount);
            Writer.Advance(commandLength);
        }

        public unsafe void Emit<TCommand>(TCommand* value) where TCommand : unmanaged, ICommand
        {
            var commandLength = MathHelpers.AlignUp(sizeof(CommandType) + sizeof(TCommand), Command.Alignment);
            var buffer = Writer.GetSpan(commandLength);

            ref var start = ref buffer[0];
            Unsafe.As<byte, CommandType>(ref start) = value->Type;
            Unsafe.As<byte, TCommand>(ref Unsafe.Add(ref start, sizeof(CommandType))) = *value;
            Writer.Advance(commandLength);
        }

        public unsafe void EmitEmpty(CommandType command)
        {
            var commandLength = sizeof(CommandType);
            var buffer = Writer.GetSpan(commandLength);

            ref var start = ref buffer[0];
            Unsafe.As<byte, CommandType>(ref start) = command;
            Writer.Advance(commandLength);
        }
    }


    internal interface IHandleMapper
    {

    }

    internal unsafe struct D3D12View
    {
        public ID3D12Resource* Resource;
        public ulong Length;
        public uint Height, Width, DepthOrArraySize;
        public DXGI_FORMAT Format;
        public D3D12_CPU_DESCRIPTOR_HANDLE DepthStencil, RenderTarget, UnorderedAccess, ShaderResource, ConstantBuffer;
    }

    internal unsafe struct D3D12Fence
    {
        public ID3D12Fence* Fence;
        public D3D12_FENCE_FLAGS Flags;
    }

    internal unsafe struct D3D12Buffer
    {
        public ID3D12Resource* Buffer;
        public D3D12_RESOURCE_FLAGS Flags;
        public void* CpuAddress;
        public ulong GpuAddress;
        public ulong Length;
    }

    internal unsafe struct D3D12Texture
    {
        public ID3D12Resource* Texture;
        public uint Width, Height, DepthOrArraySize;
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
        public bool IsRaytracing;
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
            _fences = new(capacity);
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
        private GenerationHandleAllocator<FenceHandle, D3D12Fence> _fences;

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
        public D3D12Fence GetAndFree(FenceHandle handle) => _fences.GetAndFreeHandle(handle);

        public FenceHandle Create(in D3D12Fence data) => _fences.AllocateHandle(data);
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
        public D3D12Fence GetInfo(FenceHandle handle) => _fences.GetHandleData(handle);
        public D3D12View GetInfo(ViewHandle handle) => _views.GetHandleData(handle);
        public D3D12ViewSet GetInfo(ViewSetHandle handle) => _viewSets.GetHandleData(handle);
        public D3D12QueryHeap GetInfo(QuerySetHandle handle) => _queryHeaps.GetHandleData(handle);
        public D3D12PipelineState GetInfo(PipelineHandle handle) => _pipelines.GetHandleData(handle);
        public D3D12Buffer GetInfo(BufferHandle handle) => _buffers.GetHandleData(handle);
        public D3D12Texture GetInfo(TextureHandle handle) => _textures.GetHandleData(handle);
        public D3D12RaytracingAccelerationStructure GetInfo(RaytracingAccelerationStructureHandle handle) => _accelarationStructures.GetHandleData(handle);
        public D3D12Heap GetInfo(HeapHandle handle) => _heaps.GetHandleData(handle);
        public D3D12RootSignature GetInfo(RootSignatureHandle handle) => _signatures.GetHandleData(handle);

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
}
