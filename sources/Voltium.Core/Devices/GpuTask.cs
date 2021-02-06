using System;
using System.Runtime.CompilerServices;
using System.Threading;
using TerraFX.Interop;
using Voltium.Common;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Voltium.Core.Devices;
using System.Diagnostics;
using Voltium.Common.Pix;
using System.Diagnostics.CodeAnalysis;
using System.Collections;
using System.Collections.Generic;

namespace Voltium.Core.Memory
{
    /// <summary>
    /// Indicates a point in GPU execution
    /// </summary>
    public unsafe struct GpuTask : ICriticalNotifyCompletion
    {
        /// <summary>
        /// Represents an already completed <see cref="GpuTask"/>
        /// </summary>
        public static GpuTask Completed => new GpuTask(null, default, 0);


        /// <summary>
        /// This method should not be used except by compilers
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public GpuTask GetAwaiter() => this;

        /// <summary>
        /// Synchronously blocks until the GPU has reached the point represented by this <see cref="GpuTask"/>
        /// </summary>
        public void Block() => GetResult();


        /// <summary>
        /// Synchronously blocks until the GPU has reached the point represented by all provided <see cref="GpuTask"/>s
        /// </summary>
        public static void BlockAll(params GpuTask[] tasks) => BlockMany(tasks, D3D12_MULTIPLE_FENCE_WAIT_FLAGS.D3D12_MULTIPLE_FENCE_WAIT_FLAG_ALL);

        /// <summary>
        /// Synchronously blocks until the GPU has reached the point represented by any provided <see cref="GpuTask"/>
        /// </summary>
        public static void BlockAny(params GpuTask[] tasks) => BlockMany(tasks, D3D12_MULTIPLE_FENCE_WAIT_FLAGS.D3D12_MULTIPLE_FENCE_WAIT_FLAG_ANY);

        private static void BlockMany(GpuTask[] tasks, D3D12_MULTIPLE_FENCE_WAIT_FLAGS flags)
        {
            ComputeDevice? device = tasks[0]._device;
            var fences = stackalloc ID3D12Fence*[tasks.Length];
            var values = stackalloc ulong[tasks.Length];
            int i = 0;

            // Get the device from one of the tasks. Some tasks (e.g GpuTask.Completed) have a null device
            // if all devices are null, they are all completed so we return
            // else we need to ensure all non-null devices are the same
            foreach (var task in tasks)
            {
                task.GetFenceAndMarker(out fences[i], out values[i]);
                if (device is null && task._device is not null)
                {
                    device = task._device;
                }
                else if (task._device is not null && device is not null && !ReferenceEquals(task._device, device))
                {
                    ThrowHelper.ThrowArgumentException("Tasks provided to BlockAll came from different devices");
                };
                i++;
            }

            if (device is null)
            {
                return;
            }

            device.ThrowIfFailed(device.As<ID3D12Device1>()->SetEventOnMultipleFenceCompletion(fences, values, (uint)tasks.Length, flags, default));
        }

        private ComputeDevice? _device;
        private UniqueComPtr<ID3D12Fence> _fence;
        private TaskScheduler _scheduler;
        private IntPtr _hEvent;
        private ulong _reached;
        //private bool _finished;

        internal GpuTask(ComputeDevice? device, UniqueComPtr<ID3D12Fence> fence, ulong marker)
            : this(device, fence, marker, TaskScheduler.Default)
        {
        }

        internal GpuTask(ComputeDevice? device, UniqueComPtr<ID3D12Fence> fence, ulong reached, TaskScheduler scheduler)
        {
            _device = device;
            _fence = fence;
            _reached = reached;
            _scheduler = scheduler;
           // _finished =  /* if the fence is null, this is a dummy completed task */ !fence.Exists;
            _hEvent = default;
        }

        /// <summary>
        /// This method should not be used except by compilers
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void OnCompleted(Action continuation)
        {
            if (_scheduler == TaskScheduler.Default)
            {
                ThreadPool.QueueUserWorkItem(state => Unsafe.As<Action>(state)!.Invoke(), continuation);
            }
            else
            {
                Task.Factory.StartNew(continuation, CancellationToken.None, TaskCreationOptions.None, _scheduler);
            }
        }

        /// <summary>
        /// This method should not be used except by compilers
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void UnsafeOnCompleted(Action continuation)
        {
            if (_scheduler == TaskScheduler.Default)
            {
                _ = ThreadPool.UnsafeQueueUserWorkItem(state => Unsafe.As<Action>(state)!.Invoke(), continuation);
            }
            else
            {
                _ = Task.Factory.StartNew(continuation, CancellationToken.None, TaskCreationOptions.None, _scheduler);
            }
        }

        /// <summary>
        /// Whether the GPU has reached the point represented by this <see cref="GpuTask"/>
        /// </summary>
        [MemberNotNullWhen(false, nameof(_device))]
        public bool IsCompleted => /* null device/fence = completed */ _device is null || !_fence.Exists || (_device = _fence.Ptr->GetCompletedValue() >= _reached ? null : _device) is null;

        private struct CallbackData
        {
            public delegate*<object?, void> FnPtr;
            public IntPtr ObjectHandle;
            public IntPtr Event;
            public IntPtr WaitHandle;
        }

        // avoid interface calls for common variants (array/list)

        public void RegisterDisposal<T>(T[] disposables) where T : IDisposable
        {
            static void _Dispose(T[] disposables)
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
            }
            RegisterCallback(disposables, &_Dispose);
        }

        public void RegisterDisposal<T>(List<T> disposables) where T : IDisposable
        {
            static void _Dispose(List<T> disposables)
            {
                for (var i = 0; i < disposables.Count; i++)
                {
                    disposables[i].Dispose();
                }
            }
            RegisterCallback(disposables, &_Dispose);
        }

        public void RegisterDisposal<T>(IEnumerable<T> disposables) where T : IDisposable
        {
            static void _Dispose(IEnumerable<T> disposables)
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
            }
            RegisterCallback(disposables, &_Dispose);
        }

        public void RegisterDisposal<T>(T disposable) where T : class?, IDisposable
        {
            static void _Dispose(T disposable) => disposable.Dispose();
            RegisterCallback(disposable, &_Dispose);
        }

        internal void RegisterCallback<T>(T state, delegate*<T, void> onFinished) where T : class?
        {
            if (IsCompleted)
            {
                onFinished(state);
                return;
            }

            if (_hEvent == default)
            {
                _hEvent = Windows.CreateEventW(null, Windows.FALSE, Windows.FALSE, null);
                _device.ThrowIfFailed(_fence.Ptr->SetEventOnCompletion(_reached, _hEvent));
            }

            var gcHandle = GCHandle.Alloc(state);

            // see below, we store the managed object handle and fnptr target in this little block
            var context = Helpers.Alloc<CallbackData>();
            IntPtr newHandle;

            int err = Windows.RegisterWaitForSingleObject(
                &newHandle,
                _hEvent,
                &CallbackWrapper,
                context,
                Windows.INFINITE,
                0
            );

            if (err == 0)
            {
                ThrowHelper.ThrowWin32Exception("RegisterWaitForSingleObject failed");
            }

            context->FnPtr = (delegate*<object?, void>)onFinished;
            context->ObjectHandle = GCHandle.ToIntPtr(gcHandle);
            context->Event = _hEvent;
            context->WaitHandle = newHandle;
        }

        [UnmanagedCallersOnly]
        private static void CallbackWrapper(void* pContext, byte _)
        {
            var context = (CallbackData*)pContext;

            PIXMethods.NotifyWakeFromFenceSignal(context->Event);

            // we know it takes a T which is a ref type. provided no one does something weird and hacky to invoke this method, we can safely assume it is a T
            delegate*<object?, void> fn = context->FnPtr;
            var val = GCHandle.FromIntPtr(context->ObjectHandle);

            // the user specified callback
            fn(val.Target);

            val.Free();
            Helpers.Free(context);
        }

        /// <summary>
        /// This method should not be used except by compilers
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void GetResult()
        {
            if (!IsCompleted)
            {
                // blocks current thread until we complete
                _device.ThrowIfFailed(_fence.Ptr->SetEventOnCompletion(_reached, default));
            }
        }

        internal readonly void GetFenceAndMarker(out ID3D12Fence* fence, out ulong marker)
        {
            fence = _fence.Ptr;
            marker = _reached;
        }
    }

    ///// <summary>
    ///// Indicates a point in GPU execution
    ///// </summary>
    //public unsafe struct GpuTask<T>
    //{
    //    /// <summary>
    //    /// Represents an already completed <see cref="GpuTask"/>
    //    /// </summary>
    //    public static GpuTask<T> FromResult(T value) => new GpuTask<T>(default, 0, value);

    //    /// <summary>
    //    /// This type should not be used except by compilers
    //    /// </summary>
    //    [EditorBrowsable(EditorBrowsableState.Never)]
    //    public struct Awaiter : ICriticalNotifyCompletion
    //    {
    //        private UniqueComPtr<ID3D12Fence> _fence;
    //        private ulong _reached;
    //        private TaskScheduler _scheduler;
    //        private bool _finished;
    //        private delegate*<T> _getResult;
    //        private T _value;

    //        internal Awaiter(UniqueComPtr<ID3D12Fence> fence, ulong reached, TaskScheduler scheduler, T value)
    //        {
    //            _fence = fence;
    //            _reached = reached;
    //            _scheduler = scheduler;
    //            _finished =  /* if the fence is null, this is a dummy completed task */ !fence.Exists;
    //            _getResult = null;
    //            _value = value;
    //        }

    //        internal Awaiter(UniqueComPtr<ID3D12Fence> fence, ulong reached, TaskScheduler scheduler, delegate*<T> getResult)
    //        {
    //            _fence = fence;
    //            _reached = reached;
    //            _scheduler = scheduler;
    //            _finished =  /* if the fence is null, this is a dummy completed task */ !fence.Exists;
    //            _getResult = getResult;
    //            Unsafe.SkipInit(out _value);
    //        }

    //        /// <summary>
    //        /// This method should not be used except by compilers
    //        /// </summary>
    //        [EditorBrowsable(EditorBrowsableState.Never)]
    //        public void OnCompleted(Action continuation)
    //        {
    //            if (_scheduler == TaskScheduler.Default)
    //            {
    //                ThreadPool.QueueUserWorkItem(state => Unsafe.As<Action>(state)!.Invoke(), continuation);
    //            }
    //            else
    //            {
    //                Task.Factory.StartNew(continuation, CancellationToken.None, TaskCreationOptions.None, _scheduler);
    //            }
    //        }

    //        /// <summary>
    //        /// This method should not be used except by compilers
    //        /// </summary>
    //        [EditorBrowsable(EditorBrowsableState.Never)]
    //        public void UnsafeOnCompleted(Action continuation)
    //        {
    //            if (_scheduler == TaskScheduler.Default)
    //            {
    //                _ = ThreadPool.UnsafeQueueUserWorkItem(state => Unsafe.As<Action>(state)!.Invoke(), continuation);
    //            }
    //            else
    //            {
    //                _ = Task.Factory.StartNew(continuation, CancellationToken.None, TaskCreationOptions.None, _scheduler);
    //            }
    //        }

    //        /// <summary>
    //        /// This property should not be used except by compilers
    //        /// </summary>
    //        [EditorBrowsable(EditorBrowsableState.Never)]
    //        public bool IsCompleted => /* null fence = completed */ !_fence.Exists || _finished || (_finished = _fence.Get()->GetCompletedValue() >= _reached);

    //        /// <summary>
    //        /// This method should not be used except by compilers
    //        /// </summary>
    //        [EditorBrowsable(EditorBrowsableState.Never)]
    //        public T GetResult()
    //        {
    //            if (!IsCompleted)
    //            {
    //                // blocks current thread until we complete
    //                _device.ThrowIfFailed(_fence.Get()->SetEventOnCompletion(_reached, default));
    //            }

    //            // Make sure we only call it once
    //            if (_getResult != null)
    //            {
    //                _value = _getResult();
    //                _getResult = null;
    //            }

    //            return _value;
    //        }

    //        internal void GetFenceAndMarker(out ID3D12Fence* fence, out ulong marker)
    //        {
    //            fence = _fence.Get();
    //            marker = _reached;
    //        }
    //    }

    //    private Awaiter _awaiter;

    //    internal GpuTask(ComputeDevice device, UniqueComPtr<ID3D12Fence> fence, ulong marker, delegate*<T> getResult)
    //    {
    //        _awaiter = new(device, fence, marker, TaskScheduler.Default, getResult);
    //    }

    //    internal GpuTask(UniqueComPtr<ID3D12Fence> fence, ulong marker, T value)
    //    {
    //        _awaiter = new(device, fence, marker, TaskScheduler.Default, value);
    //    }

    //    /// <summary>
    //    /// This method should not be used except by compilers
    //    /// </summary>
    //    [EditorBrowsable(EditorBrowsableState.Never)]
    //    public Awaiter GetAwaiter() => _awaiter;

    //    /// <summary>
    //    /// Synchronously blocks until the GPU has reached the point represented by this <see cref="GpuTask"/>
    //    /// </summary>
    //    public T Block() => _awaiter.GetResult();


    //    /// <summary>
    //    /// Whether the GPU has reached the point represented by this <see cref="GpuTask"/>
    //    /// </summary>
    //    public bool IsCompleted => _awaiter.IsCompleted;

    //    internal void GetFenceAndMarker(out ID3D12Fence* fence, out ulong marker) => _awaiter.GetFenceAndMarker(out fence, out marker);
    //}
}
