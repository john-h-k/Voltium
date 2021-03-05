//using System.Diagnostics;
//using TerraFX.Interop;
//using Voltium.Common;
//using Voltium.Core.Devices;


//namespace Voltium.Core.Pool
//{
//    /// <summary>
//    /// A pool of <see cref="ID3D12CommandAllocator"/>
//    /// </summary>
//    internal unsafe sealed class CommandListPool : ThreadSafeComPool<ID3D12GraphicsCommandList, CommandListPool.ListCreationParams>
//    {
//        private ComputeDevice _device;

//        public CommandListPool(ComputeDevice device)
//        {
//            Debug.Assert(device is object);
//            _device = device;
//        }

//        public struct ListCreationParams
//        {
//            public ListCreationParams(
//                DeviceContext context,
//                ID3D12CommandAllocator* allocator,
//                ID3D12PipelineState* pso
//            )
//            {
//                Debug.Assert(allocator != null);

//                Type = (D3D12_COMMAND_LIST_TYPE)context;
//                Allocator = allocator;
//                Pso = pso;
//            }

//            internal D3D12_COMMAND_LIST_TYPE Type;
//            internal ID3D12CommandAllocator* Allocator;
//            internal ID3D12PipelineState* Pso;
//        }

//        public UniqueComPtr<ID3D12GraphicsCommandList> Rent(
//            DeviceContext context,
//            ID3D12CommandAllocator* allocator,
//            ID3D12PipelineState* pso
//        )
//            => Rent(new ListCreationParams(context, allocator, pso));

//        private int _listCount = 0;
//        protected override UniqueComPtr<ID3D12GraphicsCommandList> Create(ListCreationParams state)
//        {
//            using UniqueComPtr<ID3D12GraphicsCommandList> list = default;
//            _device.ThrowIfFailed(_device.DevicePointer->CreateCommandList(
//                0, // TODO: MULTI-GPU
//                state.Type,
//                state.Allocator,
//                state.Pso,
//                list.Iid,
//                (void**)&list
//            ));

//            LogHelper.LogDebug($"New command list allocated (this is the #{_listCount++} list)");

//            DebugHelpers.SetName(list.Ptr, $"Pooled list #{_listCount}");

//            // 'ManageRent' expects closed list
//            _device.ThrowIfFailed(list.Ptr->Close());

//            return list.Move();
//        }

//        protected sealed override void InternalDispose()
//        {
//            base.InternalDispose();
//        }

//        protected override void ManageRent(ref UniqueComPtr<ID3D12GraphicsCommandList> value, ListCreationParams state)
//        {
//            _device.ThrowIfFailed(value.Ptr->Reset(state.Allocator, state.Pso));
//        }

//        protected override void ManageReturn(ref UniqueComPtr<ID3D12GraphicsCommandList> value)
//        {
//        }
//    }
//}
