using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Contexts;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;
using Voltium.Core.Pool;
using Voltium.TextureLoading;
using static TerraFX.Interop.Vulkan;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core
{
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

    public unsafe sealed class IndirectCommand : IInternalD3D12Object
    {
        private UniqueComPtr<ID3D12CommandSignature> _value;

        internal IndirectCommand(UniqueComPtr<ID3D12CommandSignature> value, RootSignature? rootSignature, uint commandSizeInBytes, ReadOnlyMemory<IndirectArgument> indirectArguments)
        {
            _value = value;
            RootSignature = rootSignature;
            CommandSizeInBytes = commandSizeInBytes;
            IndirectArguments = indirectArguments;
        }

        public RootSignature? RootSignature { get; }
        public uint CommandSizeInBytes { get; }
        public ReadOnlyMemory<IndirectArgument> IndirectArguments { get; }


        internal ID3D12CommandSignature* GetCommandSignature() => _value.Ptr;
        ID3D12Object* IInternalD3D12Object.GetPointer() => (ID3D12Object*)GetCommandSignature();
    }

    /// <summary>
    /// Represents a context on which GPU commands can be recorded
    /// </summary>
    public unsafe partial class ComputeContext : CopyContext
    {
        internal ComputeContext(in ContextParams @params) : base(@params)
        {

        }

        //AtomicCopyBufferUINT
        //AtomicCopyBufferUINT64
        //CopyBufferRegion
        //CopyResource
        //CopyTextureRegion
        //CopyTiles
        //EndQuery
        //ResolveQueryData
        //ResourceBarrier
        //SetProtectedResourceSession
        //WriteBufferImmediate

        //BuildRaytracingAccelerationStructure
        //ClearState
        //ClearUnorderedAccessViewFloat
        //ClearUnorderedAccessViewUint
        //CopyRaytracingAccelerationStructure
        //DiscardResource
        //Dispatch
        //DispatchRays
        //EmitRaytracingAccelerationStructurePostbuildInfo
        //ExecuteIndirect
        //ExecuteMetaCommand
        //InitializeMetaCommand
        //ResolveQueryData
        //ResourceBarrier
        //SetComputeRoot32BitConstant
        //SetComputeRoot32BitConstants
        //SetComputeRootConstantBufferView
        //SetComputeRootDescriptorTable
        //SetComputeRootShaderResourceView
        //SetComputeRootSignature
        //SetComputeRootUnorderedAccessView
        //SetDescriptorHeaps
        //SetPipelineState
        //SetPipelineState1
        //SetPredication

        //BeginEvent
        //BeginQuery
        //ClearState
        //ClearUnorderedAccessViewFloat
        //ClearUnorderedAccessViewUint
        //Close
        //CopyBufferRegion
        //CopyResource
        //CopyTextureRegion
        //Dispatch
        //EndEvent
        //EndQuery
        //Reset
        //ResolveQueryData
        //ResourceBarrier
        //SetComputeRoot32BitConstant
        //SetComputeRoot32BitConstants
        //SetComputeRootConstantBufferView
        //SetComputeRootDescriptorTable
        //SetComputeRootShaderResourceView
        //SetComputeRootSignature
        //SetComputeRootUnorderedAccessView
        //SetDescriptorHeaps
        //SetMarker
        //SetPipelineState
        //SetPredication

        public void ExecuteIndirect(
            IndirectCommand command,
            [RequiresResourceState(ResourceState.IndirectArgument)] in Buffer commandBuffer,
            uint maxCommandCount = 1
        ) => ExecuteIndirect(command, commandBuffer, 0, maxCommandCount);


        public void ExecuteIndirect(
            IndirectCommand command,
            [RequiresResourceState(ResourceState.IndirectArgument)] in Buffer commandBuffer,
            uint commandBufferOffset,
            uint maxCommandCount = 1
        )
        {
#if D3D12
            List->ExecuteIndirect(
                command.GetCommandSignature(),
                maxCommandCount,
                commandBuffer.GetResourcePointer(),
                commandBufferOffset,
                null,
                0
            );
#else
            foreach (ref readonly var argument in command.IndirectArguments.Span)
            {
                switch (argument.Desc.Type)
                {
                    case D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DRAW:
                        vkCmdDrawIndirect(List, commandBuffer.GetResourcePointer(), commandBufferOffset, maxCommandCount, command.CommandSizeInBytes);
                        break;
                    case D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DRAW_INDEXED:
                        vkCmdDrawIndexedIndirect(List, commandBuffer.GetResourcePointer(), commandBufferOffset, maxCommandCount, command.CommandSizeInBytes);
                        break;
                    case D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DISPATCH:
                        for (var i = 0u; i < maxCommandCount; i++)
                        {
                            vkCmdDispatchIndirect(List, commandBuffer.GetResourcePointer(), commandBufferOffset + (i * command.CommandSizeInBytes));
                        }
                        break;
                    case D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DISPATCH_RAYS:
                        ThrowHelper.ThrowPlatformNotSupportedException("vkCmdTraceRaysIndirectKHR unfortunately sucks");
                        break;
                    case D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DISPATCH_MESH:
                        ThrowHelper.ThrowPlatformNotSupportedException("vkCmdMeshWorkIndirectNV unfortunately sucks");
                        break;
                }
            }
#endif

        }

        public void ExecuteIndirect(
            IndirectCommand command,
           [RequiresResourceState(ResourceState.IndirectArgument)] in Buffer commandBuffer,
            [RequiresResourceState(ResourceState.IndirectArgument)] in Buffer commandCountBuffer,
            uint maxCommandCount = 1
        ) => ExecuteIndirect(command, commandBuffer, 0, commandCountBuffer, 0, maxCommandCount);

        public void ExecuteIndirect(
            IndirectCommand command,
            [RequiresResourceState(ResourceState.IndirectArgument)] in Buffer commandBuffer,
            uint commandBufferOffset,
            [RequiresResourceState(ResourceState.IndirectArgument)] in Buffer commandCountBuffer,
            uint maxCommandCount = 1
        ) => ExecuteIndirect(command, commandBuffer, commandBufferOffset, commandCountBuffer, 0, maxCommandCount);

        public void ExecuteIndirect(
            IndirectCommand command,
           [RequiresResourceState(ResourceState.IndirectArgument)] in Buffer commandBuffer,
            [RequiresResourceState(ResourceState.IndirectArgument)] in Buffer commandCountBuffer,
            uint commandCountBufferOffset,
            uint maxCommandCount = 1
        ) => ExecuteIndirect(command, commandBuffer, 0, commandCountBuffer, commandCountBufferOffset, maxCommandCount);

        public void ExecuteIndirect(
            IndirectCommand command,
            [RequiresResourceState(ResourceState.IndirectArgument)] in Buffer commandBuffer,
            uint commandBufferOffset,
            [RequiresResourceState(ResourceState.IndirectArgument)] in Buffer commandCountBuffer,
            in uint commandCountBufferOffset,
            uint maxCommandCount = 1
        )
        {
#if D3D12
            List->ExecuteIndirect(
                command.GetCommandSignature(),
                maxCommandCount,
                commandBuffer.GetResourcePointer(),
                commandBufferOffset,
                commandCountBuffer.GetResourcePointer(),
                commandCountBufferOffset
            );
            
#else
            foreach (ref readonly var argument in command.IndirectArguments.Span)
            {
                switch (argument.Desc.Type)
                {
                    case D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DRAW:
                        vkCmdDrawIndirectCount(
                            List,
                            commandBuffer.GetResourcePointer(),
                            commandBufferOffset,
                            commandCountBuffer.GetResourcePointer(),
                            commandCountBufferOffset,
                            maxCommandCount,
                            command.CommandSizeInBytes
                        );
                        break;
                    case D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DRAW_INDEXED:
                        vkCmdDrawIndexedIndirectCount(
                            List,
                            commandBuffer.GetResourcePointer(),
                            commandBufferOffset,
                            commandCountBuffer.GetResourcePointer(),
                            commandCountBufferOffset,
                            maxCommandCount,
                            command.CommandSizeInBytes
                        );
                        break;
                    case D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DISPATCH:
                        for (var i = 0u; i < maxCommandCount; i++)
                        {
                            ThrowHelper.ThrowPlatformNotSupportedException("vkCmdDispatchIndirectCountKHR does not exist");
                        }
                        break;
                    case D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DISPATCH_RAYS:
                        ThrowHelper.ThrowPlatformNotSupportedException("vkCmdTraceRaysIndirectKHR unfortunately sucks");
                        break;
                    case D3D12_INDIRECT_ARGUMENT_TYPE.D3D12_INDIRECT_ARGUMENT_TYPE_DISPATCH_MESH:
                        ThrowHelper.ThrowPlatformNotSupportedException("vkCmdMeshWorkIndirectNV unfortunately sucks");
                        break;
                }
            }
#endif
        }

        /// <summary>
        /// Sets the bound <see cref="DescriptorHeap"/> for the command list
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="samplers"></param>
        /// <remarks>Minimise changing the bound heaps, as on some hardware they can force a full hardware flush</remarks>
        public void SetDescriptorHeaps(DescriptorHeap? resources = null, DescriptorHeap? samplers = null)
        {
            var pHeaps = stackalloc ID3D12DescriptorHeap*[2];
            uint numHeaps = 0;

            if (resources is not null)
            {
                pHeaps[numHeaps++] = resources.GetHeap();
            }
            if (samplers is not null)
            {
                pHeaps[numHeaps++] = samplers.GetHeap();
            }

            List->SetDescriptorHeaps(numHeaps, pHeaps);
        }

        /// <summary>
        /// Clears an unordered-access view to a specified <see cref="Vector128{UInt32}"/> of values
        /// </summary>
        /// <param name="shaderVisible">A <see cref="DescriptorHandle"/> to <paramref name="tex"/> which <b>must</b> be shader-visible</param>
        /// <param name="shaderOpaque">A <see cref="DescriptorHandle"/> to <paramref name="tex"/> which <b>must not</b> be shader-visible</param>
        /// <param name="tex">The <see cref="Texture"/> to clear</param>
        /// <param name="values">The <see cref="Vector128{UInt32}"/> to clear <paramref name="tex"/> to</param>
        public void ClearUnorderedAccessViewUInt32(
            DescriptorHandle shaderVisible,
            DescriptorHandle shaderOpaque,
            [RequiresResourceState(ResourceState.UnorderedAccess)] in Texture tex,
            Vector128<uint> values = default
        )
        {
#if D3D12
            List->ClearUnorderedAccessViewUint(shaderVisible.GpuHandle, shaderOpaque.CpuHandle, tex.GetResourcePointer(), (uint*)&values, 0, null);
#else
            vkCmdClearColorImage(List, tex.GetResourcePointer(), VkImageLayout.VK_IMAGE_LAYOUT_GENERAL, (VkClearColorValue*)&values, 0, null);
#endif
        }


        /// <summary>
        /// Clears an unordered-access view to a specified <see cref="Rgba128"/> of values
        /// </summary>
        /// <param name="shaderVisible">A <see cref="DescriptorHandle"/> to <paramref name="tex"/> which <b>must</b> be shader-visible</param>
        /// <param name="shaderOpaque">A <see cref="DescriptorHandle"/> to <paramref name="tex"/> which <b>must not</b> be shader-visible</param>
        /// <param name="tex">The <see cref="Texture"/> to clear</param>
        /// <param name="values">The <see cref="Rgba128"/> to clear <paramref name="tex"/> to</param>
        public void ClearUnorderedAccessViewSingle(
            DescriptorHandle shaderVisible,
            DescriptorHandle shaderOpaque,
            [RequiresResourceState(ResourceState.UnorderedAccess)] in Texture tex,
            Rgba128 values = default
        )
        {
#if D3D12
            List->ClearUnorderedAccessViewFloat(shaderVisible.GpuHandle, shaderOpaque.CpuHandle, tex.GetResourcePointer(), (float*)&values, 0, null);
#else
            vkCmdClearColorImage(List, tex.GetResourcePointer(), VkImageLayout.VK_IMAGE_LAYOUT_GENERAL, (VkClearColorValue*)&values, 0, null);
#endif
        }

        /// <summary>
        /// Sets the current pipeline state
        /// </summary>
        /// <param name="pso">The <see cref="PipelineStateObject"/> to set</param>
        public void SetPipelineState(PipelineStateObject pso)
        {
#if D3D12
            if (pso is RaytracingPipelineStateObject rtPso)
            {
                List->SetPipelineState1((ID3D12StateObject*)rtPso.Pointer.Ptr);

            }
            else
            {
                List->SetPipelineState((ID3D12PipelineState*)pso.Pointer.Ptr);
            }
#else
            var bindPoint = pso switch
            {
                RaytracingPipelineStateObject => VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_RAY_TRACING_KHR,
                MeshPipelineStateObject or GraphicsPipelineStateObject => VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS,
                ComputePipelineStateObject => VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_COMPUTE,
                _ => default
            };

            vkCmdBindPipeline(List, bindPoint, pso.Pointer);
#endif
        }

        /// <summary>
        /// Sets a directly-bound shader resource buffer view descriptor to the graphics pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="cbuffer">The <see cref="Buffer"/> containing the buffer to add</param>
        public void SetShaderResourceBuffer(uint paramIndex, [RequiresResourceState(ResourceState.NonPixelShaderResource, ResourceState.PixelShaderResource)] in Buffer cbuffer)
            => SetShaderResourceBuffer<byte>(paramIndex, cbuffer, 0);


        /// <summary>
        /// Sets a directly-bound shader resource buffer view descriptor to the graphics pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="cbuffer">The <see cref="Buffer"/> containing the buffer to add</param>
        public void SetRaytracingAccelerationStructure(uint paramIndex, in RaytracingAccelerationStructure cbuffer)
            => SetShaderResourceBuffer<byte>(paramIndex, cbuffer.Buffer, 0);

        /// <summary>
        /// Sets a directly-bound shader resource buffer view descriptor to the graphics pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="cbuffer">The <see cref="Buffer"/> containing the buffer to add</param>
        /// <param name="offset">The offset in elements of <typeparamref name="T"/> to start the view at</param>
        public void SetShaderResourceBuffer<T>(uint paramIndex, [RequiresResourceState(ResourceState.NonPixelShaderResource, ResourceState.PixelShaderResource)] in Buffer cbuffer, uint offset = 0) where T : unmanaged
        {
            List->SetComputeRootShaderResourceView(paramIndex, cbuffer.GpuAddress + (ulong)(sizeof(T) * offset));
        }

        /// <summary>
        /// Sets a directly-bound constant buffer view descriptor to the graphics pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="cbuffer">The <see cref="Buffer"/> containing the buffer to add</param>
        /// <param name="offset">The offset in bytes to start the view at</param>
        public void SetShaderResourceBufferByteOffset(uint paramIndex, [RequiresResourceState(ResourceState.NonPixelShaderResource, ResourceState.PixelShaderResource)] in Buffer cbuffer, uint offset = 0)
        {
            List->SetComputeRootUnorderedAccessView(paramIndex, cbuffer.GpuAddress + offset);
        }



        /// <summary>
        /// Sets a directly-bound unordered access buffer view descriptor to the graphics pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="cbuffer">The <see cref="Buffer"/> containing the buffer to add</param>
        public void SetUnorderedAccessBuffer(uint paramIndex, [RequiresResourceState(ResourceState.UnorderedAccess)] in Buffer cbuffer)
            => SetUnorderedAccessBuffer<byte>(paramIndex, cbuffer, 0);

        /// <summary>
        /// Sets a directly-bound unordered access buffer view descriptor to the graphics pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="cbuffer">The <see cref="Buffer"/> containing the buffer to add</param>
        /// <param name="offset">The offset in elements of <typeparamref name="T"/> to start the view at</param>
        public void SetUnorderedAccessBuffer<T>(uint paramIndex, [RequiresResourceState(ResourceState.UnorderedAccess)] in Buffer cbuffer, uint offset = 0) where T : unmanaged
        {
            List->SetComputeRootUnorderedAccessView(paramIndex, cbuffer.GpuAddress + (ulong)(sizeof(T) * offset));
        }

        /// <summary>
        /// Sets a directly-bound unordered access view descriptor to the graphics pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="cbuffer">The <see cref="Buffer"/> containing the buffer to add</param>
        /// <param name="offset">The offset in bytes to start the view at</param>
        public void SetUnorderedAccessBuffer(uint paramIndex, [RequiresResourceState(ResourceState.UnorderedAccess)] in Buffer cbuffer, uint offset = 0)
        {
            List->SetComputeRootUnorderedAccessView(paramIndex, cbuffer.GpuAddress + offset);
        }

        /// <summary>
        /// Sets a directly-bound constant buffer view descriptor to the graphics pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="cbuffer">The <see cref="Buffer"/> containing the buffer to add</param>
        public void SetConstantBuffer(uint paramIndex, [RequiresResourceState(ResourceState.ConstantBuffer)] in Buffer cbuffer)
            => SetConstantBuffer<byte>(paramIndex, cbuffer, 0);

        /// <summary>
        /// Sets a directly-bound constant buffer view descriptor to the graphics pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="cbuffer">The <see cref="Buffer"/> containing the buffer to add</param>
        /// <param name="offset">The offset in elements of <typeparamref name="T"/> to start the view at</param>
        public void SetConstantBuffer<T>(uint paramIndex, [RequiresResourceState(ResourceState.ConstantBuffer)] in Buffer cbuffer, uint offset = 0) where T : unmanaged
        {
            var alignedSize = (sizeof(T) + 255) & ~255;

            List->SetComputeRootConstantBufferView(paramIndex, cbuffer.GpuAddress + (ulong)(alignedSize * offset));
        }

        /// <summary>
        /// Sets a directly-bound constant buffer view descriptor to the graphics pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="cbuffer">The <see cref="Buffer"/> containing the buffer to add</param>
        /// <param name="offset">The offset in bytes to start the view at</param>
        public void SetConstantBufferByteOffset(uint paramIndex, [RequiresResourceState(ResourceState.ConstantBuffer)] in Buffer cbuffer, uint offset = 0)
        {
            List->SetComputeRootConstantBufferView(paramIndex, cbuffer.GpuAddress + offset);
        }

        /// <summary>
        /// Sets a descriptor table to the graphics pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="handle">The <see cref="DescriptorHandle"/> containing the first view</param>
        public void SetRootDescriptorTable(uint paramIndex, DescriptorHandle handle)
        {
            List->SetComputeRootDescriptorTable(paramIndex, handle.GpuHandle);
        }

        /// <summary>
        /// Sets a group of 32 bit values to the graphics pipeline
        /// </summary>
        /// <typeparam name="T">The type of the elements used. This must have a size that is a multiple of 4</typeparam>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which these constants represents</param>
        /// <param name="value">The 32 bit values to set</param>
        /// <param name="offset">The offset, in 32 bit offsets, to bind this at</param>
        public void SetRoot32BitConstants<T>(uint paramIndex, in T value, uint offset = 0) where T : unmanaged
        {
            if (sizeof(T) % 4 != 0)
            {
                ThrowHelper.ThrowArgumentException(
                    $"Type '{typeof(T).Name}' has size '{sizeof(T)}' but {nameof(SetRoot32BitConstants)} requires param '{nameof(value)} '" +
                    "to have size divisble by 4"
                );
            }

            fixed (T* pValue = &value)
            {
#if D3D12
                List->SetComputeRoot32BitConstants(paramIndex, (uint)sizeof(T) / 4, pValue, offset);
#else
                vkCmdPushConstants(List, Layout, (uint)VkShaderStageFlagBits.VK_SHADER_STAGE_ALL, offset / 4, (uint)sizeof(T), pValue);
#endif
            }
        }


        /// <summary>
        /// Sets a 32 bit value to the graphics pipeline
        /// </summary>
        /// <typeparam name="T">The type of the element used. This must have a size that is 4</typeparam>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which these constants represents</param>
        /// <param name="value">The 32 bit value to set</param>
        /// <param name="offset">The offset, in 32 bit offsets, to bind this at</param>
        public void SetRoot32BitConstant<T>(uint paramIndex, T value, uint offset = 0) where T : unmanaged
        {
            if (sizeof(T) != 4)
            {
                ThrowHelper.ThrowArgumentException(
                    $"Type '{typeof(T).Name}' has size '{sizeof(T)}' but {nameof(SetRoot32BitConstant)} requires param '{nameof(value)} '" +
                    "to have size 4"
                );
            }

#if D3D12
            List->SetComputeRoot32BitConstant(paramIndex, Unsafe.As<T, uint>(ref value), offset);
#else
            vkCmdPushConstants(List, Layout, (uint)VkShaderStageFlagBits.VK_SHADER_STAGE_ALL, offset / 4, 4, &value);
#endif
        }

        protected VkPipelineLayout Layout;

        /// <summary>
        /// Set the graphics root signature for the command list
        /// </summary>
        /// <param name="signature">The signature to set to</param>
        public void SetRootSignature(RootSignature signature)
        {
#if D3D12
            List->SetComputeRootSignature(signature.Value);
#else
            Layout = signature.Value;
#endif
        }

        /// <inheritdoc cref="Dispatch(uint, uint, uint)"/>
        public void Dispatch(int x, int y = 1, int z = 1)
            => Dispatch((uint)x, (uint)y, (uint)z);

        /// <summary>
        /// Dispatches thread groups
        /// </summary>
        /// <param name="x">How many thread groups should be dispatched in the X direction</param>
        /// <param name="y">How many thread groups should be dispatched in the Y direction</param>
        /// <param name="z">How many thread groups should be dispatched in the Z direction</param>
        public void Dispatch(uint x, uint y = 1, uint z = 1)
        {
            FlushBarriers();
#if D3D12
            List->Dispatch(x, y, z);
#else
            vkCmdDispatch(List, x, y, z);
#endif
        }
    }
}
