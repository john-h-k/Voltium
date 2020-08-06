using System;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;
using Voltium.Core.Pool;
using Voltium.TextureLoading;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core
{
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



        /// <summary>
        /// Sets the current pipeline state
        /// </summary>
        /// <param name="pso">The <see cref="PipelineStateObject"/> to set</param>
        public void SetPipelineState(PipelineStateObject pso)
        {
            List->SetPipelineState(pso.GetPso());
        }

        /// <summary>
        /// Sets a directly-bound shader resource buffer view descriptor to the graphics pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="cbuffer">The <see cref="Buffer"/> containing the buffer to add</param>
        public void SetBuffer(uint paramIndex, in Buffer cbuffer)
            => SetBuffer<byte>(paramIndex, cbuffer, 0);

        /// <summary>
        /// Sets a directly-bound shader resource buffer view descriptor to the graphics pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="cbuffer">The <see cref="Buffer"/> containing the buffer to add</param>
        /// <param name="offset">The offset in elements of <typeparamref name="T"/> to start the view at</param>
        public void SetBuffer<T>(uint paramIndex, in Buffer cbuffer, uint offset = 0) where T : unmanaged
        {
            List->SetComputeRootShaderResourceView(paramIndex, cbuffer.GpuAddress + (ulong)(sizeof(T) * offset));
        }

        /// <summary>
        /// Sets a directly-bound constant buffer view descriptor to the graphics pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="cbuffer">The <see cref="Buffer"/> containing the buffer to add</param>
        /// <param name="offset">The offset in bytes to start the view at</param>
        public void SetBufferByteOffset(uint paramIndex, in Buffer cbuffer, uint offset = 0)
        {
            List->SetComputeRootShaderResourceView(paramIndex, cbuffer.GpuAddress + offset);
        }

        /// <summary>
        /// Sets a directly-bound constant buffer view descriptor to the graphics pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="cbuffer">The <see cref="Buffer"/> containing the buffer to add</param>
        public void SetConstantBuffer(uint paramIndex, in Buffer cbuffer)
            => SetConstantBuffer<byte>(paramIndex, cbuffer, 0);

        /// <summary>
        /// Sets a directly-bound constant buffer view descriptor to the graphics pipeline
        /// </summary>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which this view represents</param>
        /// <param name="cbuffer">The <see cref="Buffer"/> containing the buffer to add</param>
        /// <param name="offset">The offset in elements of <typeparamref name="T"/> to start the view at</param>
        public void SetConstantBuffer<T>(uint paramIndex, in Buffer cbuffer, uint offset = 0) where T : unmanaged
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
        public void SetConstantBufferByteOffset(uint paramIndex, in Buffer cbuffer, uint offset = 0)
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
        public void SetRoot32BitConstants<T>(uint paramIndex, T value, uint offset = 0) where T : unmanaged
        {
            if (sizeof(T) % 4 != 0)
            {
                ThrowHelper.ThrowArgumentException(
                    $"Type '{typeof(T).Name}' has size '{sizeof(T)}' but {nameof(SetRoot32BitConstants)} requires param '{nameof(value)} '" +
                    "to have size divisble by 4"
                );
            }

            List->SetComputeRoot32BitConstants(paramIndex, (uint)sizeof(T) / 4, &value, offset);
        }

        /// <summary>
        /// Sets a group of 32 bit values to the graphics pipeline
        /// </summary>
        /// <typeparam name="T">The type of the elements used. This must have a size that is a multiple of 4</typeparam>
        /// <param name="paramIndex">The index in the <see cref="RootSignature"/> which these constants represents</param>
        /// <param name="value">The 32 bit values to set</param>
        /// <param name="offset">The offset, in 32 bit offsets, to bind this at</param>
        public void SetRoot32BitConstants<T>(uint paramIndex, ref T value, uint offset = 0) where T : unmanaged
        {
            if (sizeof(T) % 4 != 0)
            {
                ThrowHelper.ThrowArgumentException(
                    $"Type '{typeof(T).Name}' has size '{sizeof(T)}' but {nameof(SetRoot32BitConstants)} requires param '{nameof(value)} '" +
                    "to have size divisble by 4"
                );
            }

            fixed (void* pValue = &value)
            {
                List->SetComputeRoot32BitConstants(paramIndex, (uint)sizeof(T) / 4, pValue, offset);
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

            List->SetComputeRoot32BitConstant(paramIndex, Unsafe.As<T, uint>(ref value), offset);
        }

        /// <summary>
        /// Set the graphics root signature for the command list
        /// </summary>
        /// <param name="signature">The signature to set to</param>
        public void SetRootSignature(RootSignature signature)
        {
            List->SetComputeRootSignature(signature.Value);
        }

        /// <summary>
        /// Dispatches thread groups
        /// </summary>
        /// <param name="x">How many thread groups should be dispatched in the X direction</param>
        /// <param name="y">How many thread groups should be dispatched in the Y direction</param>
        /// <param name="z">How many thread groups should be dispatched in the Z direction</param>
        public void Dispatch(uint x, uint y, uint z)
        {
            FlushBarriers();
            List->Dispatch(x, y, z);
        }
    }
}
