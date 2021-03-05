//using System;
//using System.Collections.Generic;
//using System.Diagnostics;

//using System.Runtime.CompilerServices;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.Toolkit.HighPerformance.Extensions;
//using TerraFX.Interop;
//using Voltium.Common;
//using Voltium.Common.Debugging;
//using Voltium.Core.Devices;
//using Voltium.Core.Memory;
//using Voltium.Core.Pipeline;
//using Configuration = Voltium.Common.Debugging.Configuration;

//using SpinLock = Voltium.Common.Threading.SpinLockWrapped;

//namespace Voltium.Core.Pool
//{
//    internal struct ContextParams
//    {
//        public ComputeDevice Device;
//        public DeviceContext Context;
//        public ContextFlags Flags;

//        public ContextParams(
//            ComputeDevice device,
//            DeviceContext context,
//            ContextFlags flags
//        )
//        {
//            Device = device;
//            Context = context;
//            Flags = flags;
//        }
//    }

//    internal unsafe sealed class ContextPool
//    {
//        private ComputeDevice _device;

//        private static SpinLock GetLock() => new SpinLock(Configuration.IsDebug);

//        private struct CommandAllocator
//        {
//            public UniqueComPtr<ID3D12CommandAllocator> Allocator;
//            public GpuTask Task;
//        }

//        private LockedQueue<CommandAllocator, SpinLock> _copyAllocators = new(GetLock());
//        private LockedQueue<CommandAllocator, SpinLock> _computeAllocators = new(GetLock());
//        private LockedQueue<CommandAllocator, SpinLock> _directAllocators = new(GetLock());

//        private LockedQueue<CommandAllocator, SpinLock> _encodeAllocators = new(GetLock());
//        private LockedQueue<CommandAllocator, SpinLock> _processAllocators = new(GetLock());
//        private LockedQueue<CommandAllocator, SpinLock> _decodeAllocators = new(GetLock());

//        private LockedQueue<UniqueComPtr<ID3D12GraphicsCommandList6>, SpinLock> _copyLists = new(GetLock());
//        private LockedQueue<UniqueComPtr<ID3D12GraphicsCommandList6>, SpinLock> _computeLists = new(GetLock());
//        private LockedQueue<UniqueComPtr<ID3D12GraphicsCommandList6>, SpinLock> _directLists = new(GetLock());

//        private LockedQueue<UniqueComPtr<ID3D12VideoEncodeCommandList1>, SpinLock> _encodeLists = new(GetLock());
//        private LockedQueue<UniqueComPtr<ID3D12VideoProcessCommandList2>, SpinLock> _processLists = new(GetLock());
//        private LockedQueue<UniqueComPtr<ID3D12VideoDecodeCommandList2>, SpinLock> _decodeLists = new(GetLock());

//        public ContextPool(ComputeDevice device)
//        {
//            _device = device;
//        }

//        [MethodImpl(MethodImplOptions.NoInlining)]
//        public ContextParams Rent(DeviceContext context, PipelineStateObject? pso, ContextFlags flags)
//        {
//            var allocators = GetAllocatorPoolsForContext(context);
//            var lists = GetListPoolsForContext(context);

//            static bool IsAllocatorFinished(ref CommandAllocator allocator) => allocator.Task.IsCompleted;

//            if (allocators.TryDequeue(out var allocator, &IsAllocatorFinished))
//            {
//                _device.ThrowIfFailed(allocator.Allocator.Ptr->Reset());
//            }
//            else
//            {
//                allocator = new CommandAllocator { Allocator = CreateAllocator(context), Task = GpuTask.Completed };
//            }

//            using UniqueComPtr<ID3D12PipelineState> pipeline = default;
//            using UniqueComPtr<ID3D12StateObject> stateObject = default;

//            _ = pso?.Pointer.TryQueryInterface(&pipeline);
//            _ = pso?.Pointer.TryQueryInterface(&stateObject);

//            Debug.Assert(pipeline.Exists is false || (pipeline.Exists != stateObject.Exists)); // only one should be not null

//            if (lists.TryDequeue(out var list))
//            {
//                _device.ThrowIfFailed(list.Ptr->Reset(allocator.Allocator.Ptr, pipeline.Ptr));
//            }
//            else
//            {
//                list = CreateList(context, allocator.Allocator.Ptr, pipeline.Ptr);
//            }

//            if (stateObject.Exists)
//            {
//                list.As<ID3D12GraphicsCommandList5>().Ptr->SetPipelineState1(stateObject.Ptr);
//            }

//            return new ContextParams(_device, list, allocator.Allocator, pso, context, flags);
//        }

//        public void Return(ContextParams context, in GpuTask contextFinish)
//        {
//            static void FreeAttachedResources(List<IDisposable?> resources)
//            {
//                foreach (ref var resource in resources.AsSpan())
//                {
//                    resource?.Dispose();
//                    resource = null;
//                }
//            }

//            var @params = context.Params;

//            contextFinish.RegisterCallback(context.AttachedResources, &FreeAttachedResources);

//            var executionContext = @params.Context;

//            var allocators = GetAllocatorPoolsForContext(executionContext);
//            var lists = GetListPoolsForContext(executionContext);

//            lists.Enqueue(@params.List);
//            allocators.Enqueue(new CommandAllocator { Allocator = @params.Allocator, Task = contextFinish });
//        }

//        private int _allocatorCount;
//        private int _listCount;
//        private UniqueComPtr<ID3D12CommandAllocator> CreateAllocator(DeviceContext context)
//        {
//            using UniqueComPtr<ID3D12CommandAllocator> allocator = _device.CreateAllocator(context);

//            LogHelper.LogDebug($"New command allocator allocated (this is the #{_allocatorCount++} allocator)");

//            DebugHelpers.SetName(allocator.Ptr, $"Pooled allocator #{_allocatorCount}");
//            DebugHelpers.SetPrivateData(allocator.Ptr, Guid_AllocatorType, context);

//            return allocator.Move();
//        }

//        private UniqueComPtr<ID3D12GraphicsCommandList6> CreateList(DeviceContext context, ID3D12CommandAllocator* allocator, ID3D12PipelineState* pso)
//        {
//            using UniqueComPtr<ID3D12GraphicsCommandList6> list = _device.CreateList(context, allocator, pso);

//            LogHelper.LogDebug($"New command list allocated (this is the #{_listCount++} list)");

//            DebugHelpers.SetName(list.Ptr, $"Pooled list #{_listCount}");

//            return list.Move();
//        }

//        private LockedQueue<CommandAllocator, SpinLock> GetAllocatorPoolsForContext(DeviceContext context)
//            => context switch
//            {
//                DeviceContext.Copy => _copyAllocators,
//                DeviceContext.Compute => _computeAllocators,
//                DeviceContext.Graphics => _directAllocators,
//                DeviceContext.VideoDecode => _decodeAllocators,
//                DeviceContext.VideoEncode => _encodeAllocators,
//                DeviceContext.VideoProcess => _processAllocators,
//                _ => default
//            };

//        private LockedQueue<UniqueComPtr<ID3D12GraphicsCommandList6>, SpinLock> GetListPoolsForContext(DeviceContext context)
//            => context switch
//            {
//                DeviceContext.Copy => _copyLists,
//                DeviceContext.Compute => _computeLists,
//                DeviceContext.Graphics => _directLists,
//                _ => default
//            };
//    }
//}
