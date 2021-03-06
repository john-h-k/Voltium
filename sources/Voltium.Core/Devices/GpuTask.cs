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
using Voltium.Core.NativeApi;

namespace Voltium.Core.Memory
{
    public struct Fence
    {
        internal FenceHandle Handle;
    }

    /// <summary>
    /// Indicates a point in GPU execution
    /// </summary>
    public unsafe struct GpuTask : ICriticalNotifyCompletion
    {
        /// <summary>
        /// Represents an already completed <see cref="GpuTask"/>
        /// </summary>
        public static GpuTask Completed => new GpuTask(null!, default, 0);

        /// <summary>
        /// This method should not be used except by compilers
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public GpuTask GetAwaiter() => this;

        /// <summary>
        /// Synchronously blocks until the GPU has reached the point represented by this <see cref="GpuTask"/>
        /// </summary>
        public void Block() => GetResult();

        internal INativeDevice _device;
        private TaskScheduler _scheduler;
        internal FenceHandle _fence;
        internal ulong _reached;

        internal GpuTask(INativeDevice device, FenceHandle fence, ulong marker)
            : this(device, fence, marker, TaskScheduler.Default)
        {
        }

        internal GpuTask(INativeDevice device, FenceHandle fence, ulong reached, TaskScheduler scheduler)
        {
            _device = device;
            _fence = fence;
            _reached = reached;
            _scheduler = scheduler;
        }

        /// <summary>
        /// This method should not be used except by compilers
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void OnCompleted(Action continuation)
        {
            static void _Invoke(Action continuation) => continuation();
            RegisterCallback(continuation, &_Invoke);
        }

        /// <summary>
        /// This method should not be used except by compilers
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void UnsafeOnCompleted(Action continuation)
        {
            static void _Invoke(Action continuation) => continuation();
            RegisterCallback(continuation, &_Invoke);
        }

        /// <summary>
        /// Whether the GPU has reached the point represented by this <see cref="GpuTask"/>
        /// </summary>
        [MemberNotNullWhen(false, nameof(_fence))]
        public bool IsCompleted => _device is null || _device.GetCompletedValue(_fence) >= _reached;


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
                if (disposables is IList<T> list)
                {
                    for (var i = 0; i < list.Count; i++)
                    {
                        list[i].Dispose();
                    }
                }
                else
                {
                    foreach (var disposable in disposables)
                    {
                        disposable.Dispose();
                    }
                }
            }
            RegisterCallback(disposables, &_Dispose);
        }

        public void RegisterDisposal<T>(T disposable) where T : class?, IDisposable
        {
            static void _Dispose(T disposable) => disposable.Dispose();

            RegisterCallback(disposable, &_Dispose);
        }

        public void RegisterCallback<T>(T state, delegate* <T, void> callback) where T : class?
        {
            _device.GetEventForWait(
                stackalloc[] { _fence },
                stackalloc[] { _reached },
                WaitMode.WaitForAll
            ).RegisterCallback(state, callback);
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
                _device.Wait(
                    stackalloc[] { _fence },
                    stackalloc[] { _reached },
                    WaitMode.WaitForAll
                );
            }
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
