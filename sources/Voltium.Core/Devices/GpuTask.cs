using System;
using System.Runtime.CompilerServices;
using System.Threading;
using TerraFX.Interop;
using Voltium.Common;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Voltium.Core.Memory
{
    /// <summary>
    /// Indicates a point in GPU execution
    /// </summary>
    public unsafe struct GpuTask
    {
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
                _finished = false;
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
                    ThreadPool.UnsafeQueueUserWorkItem(state => Unsafe.As<Action>(state)!.Invoke(), continuation);
                }
                else
                {
                    Task.Factory.StartNew(continuation, CancellationToken.None, TaskCreationOptions.None, _scheduler);
                }
            }

            /// <summary>
            /// This property should not be used except by compilers
            /// </summary>
            [EditorBrowsable(EditorBrowsableState.Never)]
            public bool IsCompleted => _finished || (_finished = _fence.Get()->GetCompletedValue() >= _reached);


            /// <summary>
            /// This method should not be used except by compilers
            /// </summary>
            [EditorBrowsable(EditorBrowsableState.Never)]
            public void GetResult()
            {
                if (!IsCompleted)
                {
                    Guard.ThrowIfFailed(_fence.Get()->SetEventOnCompletion(_reached, default));
                }
            }
        }

        private Awaiter _awaiter;

        internal GpuTask(ComPtr<ID3D12Fence> fence, FenceMarker marker)
        {
            _awaiter = new(fence, marker.FenceValue, TaskScheduler.Default);
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
        /// Whether the GPU has reached the point represented by this <see cref="GpuTask"/>
        /// </summary>
        public bool IsCompleted => _awaiter.IsCompleted;
    }
}
