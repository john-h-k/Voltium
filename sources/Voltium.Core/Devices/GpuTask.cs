using System;
using System.Runtime.CompilerServices;
using System.Threading;
using TerraFX.Interop;
using Voltium.Common;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Voltium.Core.Memory
{
    /// <summary>
    /// Indicates a point in GPU execution
    /// </summary>
    public unsafe struct GpuTask
    {
        /// <summary>
        /// Represents an already completed <see cref="GpuTask"/>
        /// </summary>
        public static GpuTask Completed => new GpuTask(default, 0);

        /// <summary>
        /// This type should not be used except by compilers
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public struct Awaiter : ICriticalNotifyCompletion
        {
            private ComPtr<ID3D12Fence> _fence;
            private ulong _reached;
            private TaskScheduler _scheduler;
            private bool _finished;

            internal Awaiter(ComPtr<ID3D12Fence> fence, ulong reached, TaskScheduler scheduler)
            {
                _fence = fence;
                _reached = reached;
                _scheduler = scheduler;
                _finished =  /* if the fence is null, this is a dummy completed task */ !fence.Exists;
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
            /// This property should not be used except by compilers
            /// </summary>
            [EditorBrowsable(EditorBrowsableState.Never)]
            public bool IsCompleted => /* null fence = completed */ !_fence.Exists || _finished || (_finished = _fence.Get()->GetCompletedValue() >= _reached);

            private struct CallbackData
            {
                public delegate*<object?, void> FnPtr;
                public IntPtr ObjectHandle;
            }

            internal void RegisterCallback<T>(T state, delegate*<T, void> onFinished) where T : class?
            {
                IntPtr newHandle;
                IntPtr handle = Windows.CreateEventW(null, Windows.FALSE, Windows.FALSE, null);

                var gcHandle = GCHandle.Alloc(state);

                // see below, we store the managed object handle and fnptr target in this little block
                var context = Helpers.Alloc<CallbackData>();
                context->FnPtr = (delegate*<object?, void>)onFinished;
                context->ObjectHandle = GCHandle.ToIntPtr(gcHandle);

                Guard.ThrowIfFailed(_fence.Get()->SetEventOnCompletion(_reached, handle));
                int err = Windows.RegisterWaitForSingleObject(
                    &newHandle,
                    handle,
                    (delegate* stdcall<void*, byte, void>)(delegate*<CallbackData*, byte, void>)&CallbackWrapper,
                    context,
                    Windows.INFINITE,
                    0
                );

                if (err == 0)
                {
                    ThrowHelper.ThrowWin32Exception("RegisterWaitForSingleObject failed");
                }
            }

            [UnmanagedCallersOnly]
            private static void CallbackWrapper(CallbackData* context, byte _)
            {
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
                    Guard.ThrowIfFailed(_fence.Get()->SetEventOnCompletion(_reached, default));
                }
            }

            internal void GetFenceAndMarker(out ID3D12Fence* fence, out ulong marker)
            {
                fence = _fence.Get();
                marker = _reached;
            }
        }

        private Awaiter _awaiter;

        internal GpuTask(ComPtr<ID3D12Fence> fence, ulong marker)
        {
            _awaiter = new(fence, marker, TaskScheduler.Default);
        }

        /// <summary>
        /// This method should not be used except by compilers
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Awaiter GetAwaiter() => _awaiter;

        /// <summary>
        /// Synchronously blocks until the GPU has reached the point represented by this <see cref="GpuTask"/>
        /// </summary>
        public void Block() => _awaiter.GetResult();

        /// <summary>
        /// Registers a callback for when this <see cref="GpuTask"/> completes
        /// </summary>
        /// <typeparam name="T">The type of the state to be passed to <paramref name="onFinished"/></typeparam>
        /// <param name="state">The <typeparamref name="T"/> to pass to <paramref name="onFinished"/></param>
        /// <param name="onFinished">The callback to invoke when this <see cref="GpuTask"/> finishes</param>
        public void RegisterCallback<T>(T state, delegate*<T, void> onFinished) where T : class => _awaiter.RegisterCallback(state, onFinished);


        /// <summary>
        /// Whether the GPU has reached the point represented by this <see cref="GpuTask"/>
        /// </summary>
        public bool IsCompleted => _awaiter.IsCompleted;

        internal void GetFenceAndMarker(out ID3D12Fence* fence, out ulong marker) => _awaiter.GetFenceAndMarker(out fence, out marker);
    }

    /// <summary>
    /// Indicates a point in GPU execution
    /// </summary>
    public unsafe struct GpuTask<T>
    {
        /// <summary>
        /// Represents an already completed <see cref="GpuTask"/>
        /// </summary>
        public static GpuTask<T> FromResult(T value) => new GpuTask<T>(default, 0, value);

        /// <summary>
        /// This type should not be used except by compilers
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public struct Awaiter : ICriticalNotifyCompletion
        {
            private ComPtr<ID3D12Fence> _fence;
            private ulong _reached;
            private TaskScheduler _scheduler;
            private bool _finished;
            private delegate*<T> _getResult;
            private T _value;

            internal Awaiter(ComPtr<ID3D12Fence> fence, ulong reached, TaskScheduler scheduler, T value)
            {
                _fence = fence;
                _reached = reached;
                _scheduler = scheduler;
                _finished =  /* if the fence is null, this is a dummy completed task */ !fence.Exists;
                _getResult = null;
                _value = value;
            }

            internal Awaiter(ComPtr<ID3D12Fence> fence, ulong reached, TaskScheduler scheduler, delegate*<T> getResult)
            {
                _fence = fence;
                _reached = reached;
                _scheduler = scheduler;
                _finished =  /* if the fence is null, this is a dummy completed task */ !fence.Exists;
                _getResult = getResult;
                Unsafe.SkipInit(out _value);
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
            /// This property should not be used except by compilers
            /// </summary>
            [EditorBrowsable(EditorBrowsableState.Never)]
            public bool IsCompleted => /* null fence = completed */ !_fence.Exists || _finished || (_finished = _fence.Get()->GetCompletedValue() >= _reached);

            /// <summary>
            /// This method should not be used except by compilers
            /// </summary>
            [EditorBrowsable(EditorBrowsableState.Never)]
            public T GetResult()
            {
                if (!IsCompleted)
                {
                    // blocks current thread until we complete
                    Guard.ThrowIfFailed(_fence.Get()->SetEventOnCompletion(_reached, default));
                }

                // Make sure we only call it once
                if (_getResult != null)
                {
                    _value = _getResult();
                    _getResult = null;
                }

                return _value;
            }

            internal void GetFenceAndMarker(out ID3D12Fence* fence, out ulong marker)
            {
                fence = _fence.Get();
                marker = _reached;
            }
        }

        private Awaiter _awaiter;

        internal GpuTask(ComPtr<ID3D12Fence> fence, ulong marker, delegate*<T> getResult)
        {
            _awaiter = new(fence, marker, TaskScheduler.Default, getResult);
        }

        internal GpuTask(ComPtr<ID3D12Fence> fence, ulong marker, T value)
        {
            _awaiter = new(fence, marker, TaskScheduler.Default, value);
        }

        /// <summary>
        /// This method should not be used except by compilers
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Awaiter GetAwaiter() => _awaiter;

        /// <summary>
        /// Synchronously blocks until the GPU has reached the point represented by this <see cref="GpuTask"/>
        /// </summary>
        public T Block() => _awaiter.GetResult();


        /// <summary>
        /// Whether the GPU has reached the point represented by this <see cref="GpuTask"/>
        /// </summary>
        public bool IsCompleted => _awaiter.IsCompleted;

        internal void GetFenceAndMarker(out ID3D12Fence* fence, out ulong marker) => _awaiter.GetFenceAndMarker(out fence, out marker);
    }
}
