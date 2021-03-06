using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Memory;
using Voltium.Core.Infrastructure;
using Voltium.Core.Pipeline;
using static TerraFX.Interop.Windows;
using Voltium.Core.Contexts;
using System.Runtime.InteropServices;
using Voltium.Common.Threading;
using Voltium.Core.NativeApi;

namespace Voltium.Core.Devices
{
    /// <summary>
    /// The top-level manager for application resources
    /// </summary>
    public unsafe partial class GraphicsDevice : ComputeDevice
    {
        public DeviceInfo Info { get; }

        /// <summary>
        /// The default allocator for the device
        /// </summary>
        public new GraphicsAllocator Allocator => Unsafe.As<GraphicsAllocator>(base.Allocator);


        /// <summary>
        /// The default <see cref="IndirectCommand"/> for performing an indirect dispatch.
        /// It changes no root signature bindings and has a command size of <see langword="sizeof"/>(<see cref="IndirectDispatchArguments"/>)
        /// </summary>
        public IndirectCommand DispatchIndirect { get; }

        /// <summary>
        /// The default <see cref="IndirectCommand"/> for performing an indirect draw.
        /// It changes no root signature bindings and has a command size of <see langword="sizeof"/>(<see cref="IndirectDrawArguments"/>)
        /// </summary>
        public IndirectCommand DrawIndirect { get; }

        /// <summary>
        /// The default <see cref="IndirectCommand"/> for performing an indirect indexed draw.
        /// It changes no root signature bindings and has a command size of <see langword="sizeof"/>(<see cref="IndirectDrawIndexedArguments"/>)
        /// </summary>
        public IndirectCommand DrawIndexedIndirect { get; }

        /// <summary>
        /// The default <see cref="IndirectCommand"/> for performing an indirect dispatch rays.
        /// It changes no root signature bindings and has a command size of <see langword="sizeof"/>(<see cref="IndirectDispatchRaysArguments"/>)
        /// </summary>
        public IndirectCommand DispatchRaysIndirect { get; }

        /// <summary>
        /// The default <see cref="IndirectCommand"/> for performing an indirect dispatch mesh.
        /// It changes no root signature bindings and has a command size of <see langword="sizeof"/>(<see cref="IndirectDispatchMeshArguments"/>)
        /// </summary>
        public IndirectCommand DispatchMeshIndirect { get; }


        public RaytracingAccelerationStructure AllocateRaytracingAccelerationStructure(ulong length)
        {
            var accelerationStructure = _device.AllocateRaytracingAccelerationStructure(length);

            static void Dispose(object o, ref RaytracingAccelerationStructureHandle handle)
            {
                Debug.Assert(o is GraphicsDevice);
                Unsafe.As<GraphicsDevice>(o)._device.DisposeRaytracingAccelerationStructure(handle);
            }

            return new RaytracingAccelerationStructure(length, accelerationStructure, new(this, &Dispose));
        }

        public RaytracingAccelerationStructure AllocateRaytracingAccelerationStructure(ulong length, in Heap heap, ulong offset)
        {
            var accelerationStructure = _device.AllocateRaytracingAccelerationStructure(length, heap.Handle, offset);

            static void Dispose(object o, ref RaytracingAccelerationStructureHandle handle)
            {
                Debug.Assert(o is GraphicsDevice);
                Unsafe.As<GraphicsDevice>(o)._device.DisposeRaytracingAccelerationStructure(handle);
            }

            return new RaytracingAccelerationStructure(length, accelerationStructure, new(this, &Dispose));
        }

        internal RaytracingAccelerationStructure AllocateRaytracingAccelerationStructure(ulong length, in Heap heap, ulong offset, Disposal<RaytracingAccelerationStructureHandle> dispose)
        {
            var accelerationStructure = _device.AllocateRaytracingAccelerationStructure(length, heap.Handle, offset);

            return new RaytracingAccelerationStructure(length, accelerationStructure, dispose);
        }

        public PipelineStateObject CreatePipelineStateObject(in RootSignature rootSignature, in GraphicsPipelineDesc desc)
        {
            static void Dispose(object o, ref PipelineHandle handle)
            {
                Debug.Assert(o is GraphicsDevice);
                Unsafe.As<GraphicsDevice>(o)._device.DisposePipeline(handle);
            }

            return new PipelineStateObject(_device.CreatePipeline(rootSignature.Handle, desc), new(this, &Dispose));
        }


        public RootSignature CreateRootSignature(ReadOnlySpan<RootParameter> rootParams, ReadOnlySpan<StaticSampler> samplers, RootSignatureFlags flags = RootSignatureFlags.None)
        {
            static void Dispose(object o, ref RootSignatureHandle handle)
            {
                Debug.Assert(o is GraphicsDevice);
                Unsafe.As<GraphicsDevice>(o)._device.DisposeRootSignature(handle);
            }

            return new RootSignature(_device.CreateRootSignature(rootParams, samplers, flags), new(this, &Dispose));
        }

        public Texture AllocateTexture(in TextureDesc desc, ResourceState initialState)
        {
            var texture = _device.AllocateTexture(desc, initialState);

            static void Dispose(object o, ref TextureHandle handle)
            {
                Debug.Assert(o is GraphicsDevice);
                Unsafe.As<GraphicsDevice>(o)._device.DisposeTexture(handle);
            }

            return new Texture(texture, desc, new(this, &Dispose));
        }

        public Texture AllocateTexture(in TextureDesc desc, ResourceState initialState, in Heap heap, ulong offset)
        {
            var texture = _device.AllocateTexture(desc, initialState, heap.Handle, offset);

            static void Dispose(object o, ref TextureHandle handle)
            {
                Debug.Assert(o is GraphicsDevice);
                Unsafe.As<GraphicsDevice>(o)._device.DisposeTexture(handle);
            }

            return new Texture(texture, desc, new(this, &Dispose));
        }

        internal Texture AllocateTexture(in TextureDesc desc, ResourceState initialState, in Heap heap, ulong offset, Disposal<TextureHandle> dispose)
        {
            var texture = _device.AllocateTexture(desc, initialState, heap.Handle, offset);

            return new Texture(texture, desc, dispose);
        }


        //internal UniqueComPtr<ID3D12CommandSignature> CreateCommandSignature(RootSignature? rootSignature, ReadOnlySpan<IndirectArgument> arguments, uint commandStride)
        //{
        //    fixed (void* pArguments = arguments)
        //    {
        //        var desc = new D3D12_COMMAND_SIGNATURE_DESC
        //        {
        //            ByteStride = commandStride,
        //            pArgumentDescs = (D3D12_INDIRECT_ARGUMENT_DESC*)pArguments,
        //            NumArgumentDescs = (uint)arguments.Length,
        //            NodeMask = 0 // TODO: MULTI-GPU
        //        };

        //        using UniqueComPtr<ID3D12CommandSignature> signature = default;
        //        ThrowIfFailed(DevicePointer->CreateCommandSignature(&desc, rootSignature is null ? null : rootSignature.Value, signature.Iid, (void**)&signature));

        //        return signature.Move();
        //    }
        //}

        //protected int CalculateCommandStride(ReadOnlySpan<IndirectArgument> arguments)
        //{
        //    int total = 0;
        //    foreach (ref readonly var argument in arguments)
        //    {
        //        total += argument.Desc.Type switch
        //        {
        //            D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DRAW => sizeof(D3D12_DRAW_ARGUMENTS),
        //            D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DRAW_INDEXED => sizeof(D3D12_DRAW_INDEXED_ARGUMENTS),
        //            D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DISPATCH => sizeof(D3D12_DISPATCH_ARGUMENTS),
        //            D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_VERTEX_BUFFER_VIEW => sizeof(D3D12_VERTEX_BUFFER_VIEW),
        //            D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_INDEX_BUFFER_VIEW => sizeof(D3D12_INDEX_BUFFER_VIEW),
        //            D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_CONSTANT => (int)argument.Desc.Constant.Num32BitValuesToSet * sizeof(uint),
        //            D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_CONSTANT_BUFFER_VIEW => sizeof(ulong),
        //            D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_SHADER_RESOURCE_VIEW => sizeof(ulong),
        //            D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_UNORDERED_ACCESS_VIEW => sizeof(ulong),
        //            D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DISPATCH_RAYS => sizeof(D3D12_DISPATCH_RAYS_DESC),
        //            D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DISPATCH_MESH => sizeof(D3D12_DISPATCH_MESH_ARGUMENTS),
        //            _ => 0 // unreachable
        //        };
        //    }

        //    return total;
        //}


        //public IndirectCommand CreateIndirectCommand(in IndirectArgument argument, int commandStride = -1)
        //    => CreateIndirectCommand(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in argument), 1), commandStride);
        //public IndirectCommand CreateIndirectCommand(ReadOnlySpan<IndirectArgument> arguments, int commandStride = -1)
        //    => CreateIndirectCommand(null, arguments, commandStride);


        //public IndirectCommand CreateIndirectCommand(RootSignature? rootSignature, in IndirectArgument argument, int commandStride = -1)
        //    => CreateIndirectCommand(rootSignature, MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in argument), 1), commandStride);

        //public IndirectCommand CreateIndirectCommand(RootSignature? rootSignature, ReadOnlySpan<IndirectArgument> arguments, int commandStride = -1)
        //{
        //    if (commandStride == -1)
        //    {
        //        commandStride = CalculateCommandStride(arguments);
        //    }

        //    if (arguments.Length == 1)
        //    {
        //        var cmd = arguments[0].Desc.Type switch
        //        {
        //            D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DRAW when commandStride == sizeof(IndirectDrawArguments) => DrawIndirect,
        //            D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DRAW_INDEXED when commandStride == sizeof(IndirectDrawIndexedArguments) => DrawIndexedIndirect,
        //            D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DISPATCH when commandStride == sizeof(IndirectDispatchArguments) => DispatchIndirect,
        //            D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DISPATCH_RAYS when commandStride == sizeof(IndirectDispatchRaysArguments) => DispatchRaysIndirect,
        //            D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DISPATCH_MESH when commandStride == sizeof(IndirectDispatchMeshArguments) => DispatchMeshIndirect,
        //            _ => null
        //        };

        //        if (cmd is not null)
        //        {
        //            return cmd;
        //        }
        //    }

        //    return new IndirectCommand(CreateCommandSignature(rootSignature, arguments, (uint)commandStride).Move(), rootSignature, (uint)commandStride, arguments.ToArray());
        //}

        public CommandQueue GraphicsQueue { get; }
        public CommandQueue ComputeQueue { get; }
        public CommandQueue CopyQueue { get; }

        public void GetTextureInformation(in TextureDesc desc, out ulong sizeInBytes, out ulong alignment)
        {
            InternalAllocDesc alloc;
            Allocator.CreateAllocDesc(desc, &alloc, ResourceState.Common, AllocFlags.None);
            sizeInBytes = alloc.Size;
            alignment = alloc.Alignment;
        }

        /// <summary>
        /// Returns a <see cref="GraphicsContext"/> used for recording graphical commands
        /// </summary>
        /// <returns>A new <see cref="GraphicsContext"/></returns>
        public GraphicsContext BeginGraphicsContext(in PipelineStateObject? pso = null)
        {
            var ctx = new GraphicsContext();
            if (pso is not null)
            {
                ctx.SetPipelineState(pso.Value);
            }
            return ctx;
        }


        /// <inheritdoc cref="IDisposable"/>
        public override void Dispose()
        {
            _device.Dispose();
        }
    }
}
