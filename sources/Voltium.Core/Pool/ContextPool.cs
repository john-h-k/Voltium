using System;
using System.Collections.Generic;
using System.Linq;
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
using ZLogger;
using SpinLock = Voltium.Common.Threading.SpinLockWrapped;

namespace Voltium.Core.Pool
{
    internal unsafe sealed class ContextPool
    {
        private ComputeDevice _device;

        private static SpinLock GetLock() => new SpinLock(EnvVars.IsDebug);

        private struct CommandAllocator
        {
            public ComPtr<ID3D12CommandAllocator> Allocator;
            public GpuTask Task;
        }

        private LockedQueue<CommandAllocator, SpinLock> _copyAllocators = new(GetLock());
        private LockedQueue<CommandAllocator, SpinLock> _computeAllocators = new(GetLock());
        private LockedQueue<CommandAllocator, SpinLock> _directAllocators = new(GetLock());

        private LockedQueue<ComPtr<ID3D12GraphicsCommandList>, SpinLock> _copyLists = new(GetLock());
        private LockedQueue<ComPtr<ID3D12GraphicsCommandList>, SpinLock> _computeLists = new(GetLock());
        private LockedQueue<ComPtr<ID3D12GraphicsCommandList>, SpinLock> _directLists = new(GetLock());

        public static readonly Guid Guid_AllocatorType = new Guid("5D16E61C-E2BF-4118-BB1D-8F804EC4F03D");

        public ContextPool(ComputeDevice device)
        {
            _device = device;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public GpuContext Rent(ExecutionContext context, PipelineStateObject? pso, bool executeOnClose)
        {
            var allocators = GetAllocatorPoolsForContext(context);
            var lists = GetListPoolsForContext(context);

            if (allocators.TryDequeue(out var allocator, &IsAllocatorFinished))
            {
                Guard.ThrowIfFailed(allocator.Allocator.Get()->Reset());
            }
            else
            {
                allocator = new CommandAllocator { Allocator = CreateAllocator(context), Task = GpuTask.Completed };
            }

            if (lists.TryDequeue(out var list))
            {
                Guard.ThrowIfFailed(list.Get()->Reset(allocator.Allocator.Get(), pso is null ? null : pso.GetPso()));
            }
            else
            {
                list = CreateList(context, allocator.Allocator.Get(), pso is null ? null : pso.GetPso());
            }

            return new GpuContext(_device, list, allocator.Allocator, context, executeOnClose);
        }

        private static bool IsAllocatorFinished(ref CommandAllocator allocator) => allocator.Task.IsCompleted;

        public void Return(in GpuContext gpuContext, in GpuTask contextFinish)
        {
            var context = (ExecutionContext)gpuContext.List->GetType();

            var allocators = GetAllocatorPoolsForContext(context);
            var lists = GetListPoolsForContext(context);

            lists.Enqueue(gpuContext._list);
            allocators.Enqueue(new CommandAllocator { Allocator = gpuContext._allocator, Task = contextFinish });
        }

        private int _allocatorCount;
        private int _listCount;
        private ComPtr<ID3D12CommandAllocator> CreateAllocator(ExecutionContext context)
        {
            using ComPtr<ID3D12CommandAllocator> allocator = _device.CreateAllocator(context);

            LogHelper.LogDebug($"New command allocator allocated (this is the #{_allocatorCount++} allocator)");

            DebugHelpers.SetName(allocator.Get(), $"Pooled allocator #{_allocatorCount}");
            DebugHelpers.SetPrivateData(allocator.Get(), Guid_AllocatorType, context);

            return allocator.Move();
        }

        private ComPtr<ID3D12GraphicsCommandList> CreateList(ExecutionContext context, ID3D12CommandAllocator* allocator, ID3D12PipelineState* pso)
        {
            using ComPtr<ID3D12GraphicsCommandList> list = _device.CreateList(context, allocator, pso);

            LogHelper.LogDebug($"New command list allocated (this is the #{_listCount++} list)");

            DebugHelpers.SetName(list.Get(), $"Pooled list #{_listCount}");

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

        private LockedQueue<ComPtr<ID3D12GraphicsCommandList>, SpinLock> GetListPoolsForContext(ExecutionContext context)
            => context switch
            {
                ExecutionContext.Copy => _copyLists,
                ExecutionContext.Compute => _computeLists,
                ExecutionContext.Graphics => _directLists,
                _ => default
            };
    }
}
