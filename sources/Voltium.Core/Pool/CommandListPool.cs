using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Pool
{
    /// <summary>
    /// A pool of <see cref="ID3D12CommandAllocator"/>
    /// </summary>
    internal unsafe sealed class CommandListPool : ThreadSafeComPool<ID3D12GraphicsCommandList, CommandListPool.ListCreationParams>
    {
        private ComPtr<ID3D12Device> _device;

        public CommandListPool(ComPtr<ID3D12Device> device)
        {
            Debug.Assert(device.Exists);
            _device = device.Move();
        }

        public struct ListCreationParams
        {
            public ListCreationParams(
                ExecutionContext context,
                ID3D12CommandAllocator* allocator,
                ID3D12PipelineState* pso
            )
            {
                Debug.Assert(allocator != null);

                Type = (D3D12_COMMAND_LIST_TYPE)context;
                Allocator = allocator;
                Pso = pso;
            }

            internal D3D12_COMMAND_LIST_TYPE Type;
            internal ID3D12CommandAllocator* Allocator;
            internal ID3D12PipelineState* Pso;
        }

        public ComPtr<ID3D12GraphicsCommandList> Rent(
            ExecutionContext context,
            ID3D12CommandAllocator* allocator,
            ID3D12PipelineState* pso
        )
            => Rent(new ListCreationParams(context, allocator, pso));

        private int _listCount = 0;
        protected override ComPtr<ID3D12GraphicsCommandList> Create(ListCreationParams state)
        {
            using ComPtr<ID3D12GraphicsCommandList> list = default;
            Guard.ThrowIfFailed(_device.Get()->CreateCommandList(
                0, // TODO: MULTI-GPU
                state.Type,
                state.Allocator,
                state.Pso,
                list.Guid,
                ComPtr.GetVoidAddressOf(&list)
            ));

            Logger.LogDebug($"New command list allocated (this is the #{_listCount++} list)");

            DirectXHelpers.SetObjectName(list.Get(), $"Pooled list #{_listCount}");

            // 'ManageRent' expects closed list
            Guard.ThrowIfFailed(list.Get()->Close());

            return list.Move();
        }

        protected sealed override void InternalDispose()
        {
            base.InternalDispose();
            _device.Dispose();
        }

        protected override void ManageRent(ref ComPtr<ID3D12GraphicsCommandList> value, ListCreationParams state)
        {
            Guard.ThrowIfFailed(value.Get()->Reset(state.Allocator, state.Pso));
        }

        protected override void ManageReturn(ref ComPtr<ID3D12GraphicsCommandList> value)
        {
        }
    }
}
