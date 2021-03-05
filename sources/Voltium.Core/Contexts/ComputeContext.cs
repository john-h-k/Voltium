using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.CommandBuffer;
using Voltium.Core.Contexts;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;
using Voltium.Core.Pool;
using Voltium.TextureLoading;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core
{
    public readonly struct Devirt_ArrayBufferWriter<T> : IBufferWriter<T>
    {
        public readonly ArrayBufferWriter<T> Base;

        public Devirt_ArrayBufferWriter(ArrayBufferWriter<T> @base) => Base = @base;
        public Devirt_ArrayBufferWriter(int initialCapacity) => Base = new(initialCapacity);

        public int Capacity => Base.Capacity;

        public int FreeCapacity => Base.FreeCapacity;

        public int WrittenCount => Base.WrittenCount;

        public ReadOnlyMemory<T> WrittenMemory => Base.WrittenMemory;

        public ReadOnlySpan<T> WrittenSpan => Base.WrittenSpan;

        public void Advance(int count) => Base.Advance(count);

        public void Clear() => Base.Clear();

        public Memory<T> GetMemory(int sizeHint = 0) => Base.GetMemory(sizeHint);
        public Span<T> GetSpan(int sizeHint = 0) => Base.GetSpan(sizeHint);
    }

    public struct IndirectArgument
    {
        internal D3D12_INDIRECT_ARGUMENT_DESC Desc;

        private IndirectArgument(D3D12_INDIRECT_ARGUMENT_TYPE type) => Desc = new() { Type = type };

        public static IndirectArgument CreateDraw() => new(D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DRAW);
        public static IndirectArgument CreateDrawIndexed() => new(D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DRAW_INDEXED);
        public static IndirectArgument CreateDispatch() => new(D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DISPATCH);
        public static IndirectArgument CreateDispatchRays() => new(D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DISPATCH_RAYS);
        public static IndirectArgument CreateDispatchMesh() => new(D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DISPATCH_MESH);
        public static IndirectArgument CreateIndexBuffer() => new(D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_INDEX_BUFFER_VIEW);


        public static IndirectArgument CreateVertexBuffer(uint slot)
        {
            var desc = new IndirectArgument(D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DISPATCH_MESH);
            desc.Desc.VertexBuffer.Slot = slot;
            return desc;
        }

        public static unsafe IndirectArgument CreateConstants<T>(uint parameterIndex, uint offsetIn32BitValues = 0) where T : unmanaged
        {
            if (sizeof(T) % 4 != 0)
            {
                ThrowHelper.ThrowArgumentException(
                    $"Type '{typeof(T).Name}' has size '{sizeof(T)}' but {nameof(CreateConstants)} requires typeparam '{nameof(T)} '" +
                    "to have size divisble by 4"
                );
            }

            return CreateConstants(parameterIndex, (uint)sizeof(T), offsetIn32BitValues);
        }

        public static IndirectArgument CreateConstants(uint parameterIndex, uint num32BitsValues, uint offsetIn32BitValues = 0)
        {
            var desc = new IndirectArgument(D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_CONSTANT);
            desc.Desc.Constant.RootParameterIndex = parameterIndex;
            desc.Desc.Constant.DestOffsetIn32BitValues = offsetIn32BitValues;
            desc.Desc.Constant.Num32BitValuesToSet = num32BitsValues;
            return desc;
        }

        public static IndirectArgument CreateConstantBufferView(uint parameterIndex)
        {
            var desc = new IndirectArgument(D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_CONSTANT_BUFFER_VIEW);
            desc.Desc.ConstantBufferView.RootParameterIndex = parameterIndex;
            return desc;
        }

        public static IndirectArgument CreateShaderResourceView(uint parameterIndex)
        {
            var desc = new IndirectArgument(D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_CONSTANT_BUFFER_VIEW);
            desc.Desc.ShaderResourceView.RootParameterIndex = parameterIndex;
            return desc;
        }

        public static IndirectArgument CreateUnorderedAccessView(uint parameterIndex)
        {
            var desc = new IndirectArgument(D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_CONSTANT_BUFFER_VIEW);
            desc.Desc.UnorderedAccessView.RootParameterIndex = parameterIndex;
            return desc;
        }
    }

    public struct IndirectDrawArguments
    {
        public uint VertexCountPerInstance;
        public uint InstanceCount;
        public uint StartVertexLocation;
        public uint StartInstanceLocation;
    }


    public struct IndirectDrawIndexedArguments
    {
        public uint IndexCountPerInstance;
        public uint InstanceCount;
        public uint StartIndexLocation;
        public int BaseVertexLocation;
        public uint StartInstanceLocation;
    }

    public struct IndirectDispatchArguments
    {
        public uint X, Y, Z;
    }

    public struct IndirectDispatchMeshArguments
    {
        public uint X, Y, Z;
    }

    public struct IndirectVertexBufferViewArguments
    {
        public ulong BufferLocation;
        public uint SizeInBytes;
        public uint StrideInBytes;
    }

    public struct IndirectIndexBufferViewArguments
    {
        public ulong BufferLocation;
        public uint SizeInBytes;
        public IndexFormat Format;
    }

    // TODO
    public struct IndirectDispatchRaysArguments
    {
        //private RayDispatchDesc Desc;
    }

    public struct IndirectConstantArgument<T>
    {
        public T Constant;
    }

    public struct IndirectConstantBufferViewArguments { public ulong BufferLocation; }
    public struct IndirectShaderResourceViewArguments { public ulong BufferLocation; }
    public struct IndirectUnorderedAccessViewArguments { public ulong BufferLocation; }



    public unsafe struct IndirectCommand : IDisposable
    {
        internal IndirectCommandHandle Handle;
        private Disposal<IndirectCommandHandle> _dispose;

        internal IndirectCommand(IndirectCommandHandle value, Disposal<IndirectCommandHandle> dispose ,in RootSignature? rootSignature, uint commandSizeInBytes, ReadOnlyMemory<IndirectArgument> indirectArguments)
        {
            Handle = value;
            _dispose = dispose;
            RootSignature = rootSignature;
            CommandSizeInBytes = commandSizeInBytes;
            IndirectArguments = indirectArguments;
        }

        public RootSignature? RootSignature { get; }
        public uint CommandSizeInBytes { get; }
        public ReadOnlyMemory<IndirectArgument> IndirectArguments { get; }


        public void Dispose() => _dispose.Dispose(ref Handle);
    }

    /// <summary>
    /// Represents a context on which GPU commands can be recorded
    /// </summary>
    public unsafe partial class ComputeContext : CopyContext
    {
        internal ComputeContext() : base()
        {

        }
        public void ExecuteIndirect(
            in IndirectCommand command,
            [RequiresResourceState(ResourceState.IndirectArgument)] in Buffer commandBuffer,
            uint maxCommandCount = 1
        ) => ExecuteIndirect(command, commandBuffer, 0, maxCommandCount);


        public void ExecuteIndirect(
            in IndirectCommand command,
            [RequiresResourceState(ResourceState.IndirectArgument)] in Buffer commandBuffer,
            uint commandBufferOffset,
            uint maxCommandCount = 1
        )
        {
            var executeIndirect = new CommandExecuteIndirect
            {
                IndirectCommand = command.Handle,
                Arguments = commandBuffer.Handle,
                Offset = commandBufferOffset,
                Count = maxCommandCount
            };

            _encoder.Emit(&executeIndirect);
        }

        public void ExecuteIndirect(
            in IndirectCommand command,
           [RequiresResourceState(ResourceState.IndirectArgument)] in Buffer commandBuffer,
            [RequiresResourceState(ResourceState.IndirectArgument)] in Buffer commandCountBuffer,
            uint maxCommandCount = 1
        ) => ExecuteIndirect(command, commandBuffer, 0, commandCountBuffer, 0, maxCommandCount);

        public void ExecuteIndirect(
            in IndirectCommand command,
            [RequiresResourceState(ResourceState.IndirectArgument)] in Buffer commandBuffer,
            uint commandBufferOffset,
            [RequiresResourceState(ResourceState.IndirectArgument)] in Buffer commandCountBuffer,
            uint maxCommandCount = 1
        ) => ExecuteIndirect(command, commandBuffer, commandBufferOffset, commandCountBuffer, 0, maxCommandCount);

        public void ExecuteIndirect(
            in IndirectCommand command,
           [RequiresResourceState(ResourceState.IndirectArgument)] in Buffer commandBuffer,
            [RequiresResourceState(ResourceState.IndirectArgument)] in Buffer commandCountBuffer,
            uint commandCountBufferOffset,
            uint maxCommandCount = 1
        ) => ExecuteIndirect(command, commandBuffer, 0, commandCountBuffer, commandCountBufferOffset, maxCommandCount);

        public void ExecuteIndirect(
            in IndirectCommand command,
            [RequiresResourceState(ResourceState.IndirectArgument)] in Buffer commandBuffer,
            uint commandBufferOffset,
            [RequiresResourceState(ResourceState.IndirectArgument)] in Buffer commandCountBuffer,
            in uint commandCountBufferOffset,
            uint maxCommandCount = 1
        )
        {
            var executeIndirect = new CommandExecuteIndirect
            {
                IndirectCommand = command.Handle,
                Arguments = commandBuffer.Handle,
                Offset = commandBufferOffset,
                Count = maxCommandCount,
                HasCountSpecifier = true,
            };

            var countSpecifier = new CountSpecifier
            {
                CountBuffer = commandCountBuffer.Handle,
                Offset = commandCountBufferOffset,
            };

            _encoder.EmitVariable(&executeIndirect, &countSpecifier, 1);
        }

        [IllegalBundleMethod, IllegalRenderPassMethod]
        public void ClearBuffer(
            in View shaderOpaque,
            DescriptorHandle shaderVisible,
            Vector128<uint> values = default
        )
        {
            var command = new CommandClearBufferInteger
            {
                View = shaderOpaque.Handle,
                Descriptor = shaderVisible
            };

            *(Vector128<uint>*)command.ClearValue = values;

            _encoder.Emit(&command);
        }


        /// <summary>
        /// Clears an unordered-access view to a specified <see cref="Rgba128"/> of values
        /// </summary>
        /// <param name="shaderVisible">A <see cref="DescriptorHandle"/> to <paramref name="tex"/> which <b>must</b> be shader-visible</param>
        /// <param name="shaderOpaque">A <see cref="DescriptorHandle"/> to <paramref name="tex"/> which <b>must not</b> be shader-visible</param>
        /// <param name="tex">The <see cref="Texture"/> to clear</param>
        /// <param name="values">The <see cref="Rgba128"/> to clear <paramref name="tex"/> to</param>
        [IllegalBundleMethod, IllegalRenderPassMethod]
        public void ClearTexture(
            in View shaderOpaque,
            DescriptorHandle shaderVisible,
            [RequiresResourceState(ResourceState.UnorderedAccess)] in Texture tex,
            Rgba128 values = default
        )
        {
            var command = new CommandClearTextureInteger
            {
                View = shaderOpaque.Handle,
                Descriptor = shaderVisible,
                RectangleCount = 0
            };

            *(Rgba128*)command.ClearValue = values;

            _encoder.Emit(&command);
        }

        /// <summary>
        /// Sets the current pipeline state
        /// </summary>
        /// <param name="pso">The <see cref="PipelineStateObject"/> to set</param>
        public void SetPipelineState(PipelineStateObject pso)
        {
            var command = new CommandSetPipeline
            {
                Pipeline = pso.Handle
            };

            _encoder.Emit(&command);
        }

        public void BindDescriptors(uint paramIndex, in DescriptorAllocation set)
        {
            var command = new CommandBindDescriptors
            {
                FirstSetIndex = paramIndex,
                BindPoint = BindPoint.Compute,
                SetCount = 1
            };

            var handle = set.Handle;

            _encoder.EmitVariable(&command, &handle, 1);
        }

        public void BindDescriptors(uint paramIndex, ReadOnlySpan<DescriptorAllocation> sets)
        {
            StackSentinel.SafeToStackalloc<DescriptorSetHandle>(sets.Length);

            var handles = stackalloc DescriptorSetHandle[sets.Length];
            uint i = 0;

            foreach (ref readonly var set in sets)
            {
                handles[i++] = set.Handle;
            }

            var command = new CommandBindDescriptors
            {
                FirstSetIndex = paramIndex,
                BindPoint = BindPoint.Compute,
                SetCount = (uint)sets.Length
            };

            _encoder.EmitVariable(&command, handles, (uint)sets.Length);
        }

        public void BindDynamicDescriptors(uint paramIndex, in DescriptorAllocation set, ReadOnlySpan<uint> offsets)
        {
            fixed (uint* pOffsets = offsets)
            {
                var command = new CommandBindDescriptors
                {
                    FirstSetIndex = paramIndex,
                    BindPoint = BindPoint.Compute,
                    SetCount = 1
                };

                var handle = set.Handle;

                _encoder.EmitVariable(&command, &handle, pOffsets, 1);
            }
        }

        /// <summary>
        /// Sets a group of 32 bit values to the compute pipeline
        /// </summary>
        /// <typeparam name="T">The type of the elements used. This must have a size that is a multiple of 4</typeparam>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which these constants represents</param>
        /// <param name="value">The 32 bit values to set</param>
        /// <param name="offset">The offset, in 32 bit offsets, to bind this at</param>
        public void BindConstants<T>(uint paramIndex, ref T value, uint offset = 0) where T : unmanaged
        {
            if (sizeof(T) % 4 != 0)
            {
                ThrowHelper.ThrowArgumentException(
                    $"Type '{typeof(T).Name}' has size '{sizeof(T)}' but {nameof(BindConstants)} requires param '{nameof(value)} '" +
                    "to have size divisble by 4"
                );
            }

            fixed (T* pValue = &value)
            {
                var command = new CommandBind32BitConstants
                {
                    BindPoint = BindPoint.Compute,
                    OffsetIn32BitValues = offset,
                    ParameterIndex = paramIndex,
                    Num32BitValues = (uint)sizeof(T) % 4
                };

                _encoder.EmitVariable(&command, pValue, 1);
            }
        }

        
        /// <summary>
        /// Sets a group of 32 bit values to the compute pipeline
        /// </summary>
        /// <typeparam name="T">The type of the elements used. This must have a size that is a multiple of 4</typeparam>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which these constants represents</param>
        /// <param name="value">The 32 bit values to set</param>
        /// <param name="offset">The offset, in 32 bit offsets, to bind this at</param>
        public void BindConstants<T>(uint paramIndex, T value, uint offset = 0) where T : unmanaged
        {
            if (sizeof(T) % 4 != 0)
            {
                ThrowHelper.ThrowArgumentException(
                    $"Type '{typeof(T).Name}' has size '{sizeof(T)}' but {nameof(BindConstants)} requires param '{nameof(value)} '" +
                    "to have size divisble by 4"
                );
            }

            var command = new CommandBind32BitConstants
            {
                BindPoint = BindPoint.Compute,
                OffsetIn32BitValues = offset,
                ParameterIndex = paramIndex,
                Num32BitValues = (uint)sizeof(T) % 4
            };

            _encoder.EmitVariable(&command, &value, 1);
        }

        /// <inheritdoc cref="Dispatch(uint, uint, uint)"/>
        [IllegalRenderPassMethod]
        public void Dispatch(int x, int y = 1, int z = 1)
            => Dispatch((uint)x, (uint)y, (uint)z);

        /// <summary>
        /// Dispatches thread groups
        /// </summary>
        /// <param name="x">How many thread groups should be dispatched in the X direction</param>
        /// <param name="y">How many thread groups should be dispatched in the Y direction</param>
        /// <param name="z">How many thread groups should be dispatched in the Z direction</param>
        [IllegalRenderPassMethod]
        public void Dispatch(uint x, uint y = 1, uint z = 1)
        {
            FlushBarriers();

            var command = new CommandDispatch
            {
                X = x,
                Y = y,
                Z = z
            };

            _encoder.Emit(&command);
        }
    }
}
