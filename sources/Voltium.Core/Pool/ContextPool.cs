using System;
using System.Collections.Generic;
using System.Diagnostics;

using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Common.Debugging;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;

using SpinLock = Voltium.Common.Threading.SpinLockWrapped;

namespace Voltium.Core.Pool
{
    internal struct ContextParams
    {
        public ComputeDevice Device;
        public UniqueComPtr<ID3D12GraphicsCommandList6> List;
        public UniqueComPtr<ID3D12CommandAllocator> Allocator;
        public PipelineStateObject? PipelineStateObject;
        public ExecutionContext Context;
        public ContextFlags Flags;

        public ContextParams(
            ComputeDevice device,
            UniqueComPtr<ID3D12GraphicsCommandList6> list,
            UniqueComPtr<ID3D12CommandAllocator> allocator,
            PipelineStateObject? pipelineStateObject,
            ExecutionContext context,
            ContextFlags flags
        )
        {
            Device = device;
            List = list;
            Allocator = allocator;
            PipelineStateObject = pipelineStateObject;
            Context = context;
            Flags = flags;
        }
    }

    internal unsafe sealed class ContextPool
    {
        private ComputeDevice _device;

        private static SpinLock GetLock() => new SpinLock(EnvVars.IsDebug);

        private struct CommandAllocator
        {
            public UniqueComPtr<ID3D12CommandAllocator> Allocator;
            public GpuTask Task;
        }

        private LockedQueue<CommandAllocator, SpinLock> _copyAllocators = new(GetLock());
        private LockedQueue<CommandAllocator, SpinLock> _computeAllocators = new(GetLock());
        private LockedQueue<CommandAllocator, SpinLock> _directAllocators = new(GetLock());

        private LockedQueue<UniqueComPtr<ID3D12GraphicsCommandList6>, SpinLock> _copyLists = new(GetLock());
        private LockedQueue<UniqueComPtr<ID3D12GraphicsCommandList6>, SpinLock> _computeLists = new(GetLock());
        private LockedQueue<UniqueComPtr<ID3D12GraphicsCommandList6>, SpinLock> _directLists = new(GetLock());

        public static readonly Guid Guid_AllocatorType = new Guid("5D16E61C-E2BF-4118-BB1D-8F804EC4F03D");

        public ContextPool(ComputeDevice device)
        {
            _device = device;

            // Check list support
            var ctx = Rent(ExecutionContext.Graphics, null, ContextFlags.None);
            SupportedList = CheckListSupport(ctx.List);
            ctx.List.Ptr->Close();
            Return(ctx, GpuTask.Completed);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public ContextParams Rent(ExecutionContext context, PipelineStateObject? pso, ContextFlags flags)
        {
            var allocators = GetAllocatorPoolsForContext(context);
            var lists = GetListPoolsForContext(context);

            static bool IsAllocatorFinished(ref CommandAllocator allocator) => allocator.Task.IsCompleted;

            if (allocators.TryDequeue(out var allocator, &IsAllocatorFinished))
            {
                _device.ThrowIfFailed(allocator.Allocator.Ptr->Reset());
            }
            else
            {
                allocator = new CommandAllocator { Allocator = CreateAllocator(context), Task = GpuTask.Completed };
            }

            using UniqueComPtr<ID3D12PipelineState> pipeline = default;
            using UniqueComPtr<ID3D12StateObject> stateObject = default;

            _ = pso?.Pointer.TryQueryInterface(&pipeline);
            _ = pso?.Pointer.TryQueryInterface(&stateObject);

            Debug.Assert(pipeline.Exists is false || (pipeline.Exists != stateObject.Exists)); // only one should be not null

            if (lists.TryDequeue(out var list))
            {
                _device.ThrowIfFailed(list.Ptr->Reset(allocator.Allocator.Ptr, pipeline.Ptr));
            }
            else
            {
                list = CreateList(context, allocator.Allocator.Ptr, pipeline.Ptr);
            }

            if (stateObject.Exists)
            {
                list.As<ID3D12GraphicsCommandList5>().Ptr->SetPipelineState1(stateObject.Ptr);
            }

            return new ContextParams(_device, list, allocator.Allocator, pso, context, flags);
        }

        internal SupportedGraphicsCommandList SupportedList { get; }

        private SupportedGraphicsCommandList CheckListSupport(UniqueComPtr<ID3D12GraphicsCommandList6> list)
        {
            var supported = SupportedGraphicsCommandList.GraphicsCommandList6;
            //var supported = list switch
            //{
            //    _ when list.HasInterface<ID3D12GraphicsCommandList6>() => SupportedGraphicsCommandList.GraphicsCommandList6,
            //    _ when list.HasInterface<ID3D12GraphicsCommandList5>() => SupportedGraphicsCommandList.GraphicsCommandList5,
            //    _ when list.HasInterface<ID3D12GraphicsCommandList4>() => SupportedGraphicsCommandList.GraphicsCommandList4,
            //    _ when list.HasInterface<ID3D12GraphicsCommandList3>() => SupportedGraphicsCommandList.GraphicsCommandList3,
            //    _ when list.HasInterface<ID3D12GraphicsCommandList2>() => SupportedGraphicsCommandList.GraphicsCommandList2,
            //    _ when list.HasInterface<ID3D12GraphicsCommandList1>() => SupportedGraphicsCommandList.GraphicsCommandList1,
            //    _ => SupportedGraphicsCommandList.GraphicsCommandList
            //};

            if (supported < SupportedGraphicsCommandList.GraphicsCommandList5)
            {
                ThrowHelper.ThrowPlatformNotSupportedException("GraphicsCommandList5 is required");
            }

            return supported;
        }

        public void Return(in ContextParams gpuContext, in GpuTask contextFinish)
        {
            //static void BreakOnTaskEnd(ContextPool @this) => Debugger.Break();
            //contextFinish.RegisterCallback(this, &BreakOnTaskEnd);

            var context = gpuContext.Context;

            var allocators = GetAllocatorPoolsForContext(context);
            var lists = GetListPoolsForContext(context);

            lists.Enqueue(gpuContext.List);
            allocators.Enqueue(new CommandAllocator { Allocator = gpuContext.Allocator, Task = contextFinish });
        }

        private int _allocatorCount;
        private int _listCount;
        private UniqueComPtr<ID3D12CommandAllocator> CreateAllocator(ExecutionContext context)
        {
            using UniqueComPtr<ID3D12CommandAllocator> allocator = _device.CreateAllocator(context);

            LogHelper.LogDebug($"New command allocator allocated (this is the #{_allocatorCount++} allocator)");

            DebugHelpers.SetName(allocator.Ptr, $"Pooled allocator #{_allocatorCount}");
            DebugHelpers.SetPrivateData(allocator.Ptr, Guid_AllocatorType, context);

            return allocator.Move();
        }

        private UniqueComPtr<ID3D12GraphicsCommandList6> CreateList(ExecutionContext context, ID3D12CommandAllocator* allocator, ID3D12PipelineState* pso)
        {
            using UniqueComPtr<ID3D12GraphicsCommandList6> list = _device.CreateList(context, allocator, pso);

            LogHelper.LogDebug($"New command list allocated (this is the #{_listCount++} list)");

            DebugHelpers.SetName(list.Ptr, $"Pooled list #{_listCount}");

            return list.Move();
        }

        private LockedQueue<CommandAllocator, SpinLock> GetAllocatorPoolsForContext(ExecutionContext context)
            => context switch
            {
                ExecutionContext.Copy => _copyAllocators,
                ExecutionContext.Compute => _computeAllocators,
                ExecutionContext.Graphics => _directAllocators,
                _ => default
            };

        private LockedQueue<UniqueComPtr<ID3D12GraphicsCommandList6>, SpinLock> GetListPoolsForContext(ExecutionContext context)
            => context switch
            {
                ExecutionContext.Copy => _copyLists,
                ExecutionContext.Compute => _computeLists,
                ExecutionContext.Graphics => _directLists,
                _ => default
            };
    }
}
