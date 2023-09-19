using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Voltium.Common;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.Windows.WAIT;

namespace Voltium.Core.Devices
{
    public enum TokenAllocationResult
    {
        Success,
        WrappedAround,
        NoMoreToken
    }

    internal unsafe class Win32EventTaskSource : IValueTaskSource
    {
        private (HANDLE Event, TimeSpan TimeOut)[] _hEvents = new (HANDLE, TimeSpan)[ushort.MaxValue];
        private uint _nextFree = 0;
        private bool _wrapAround;

        public Win32EventTaskSource(bool allowWrapAround = false)
        {
            _wrapAround = allowWrapAround;
        }

        public uint TokensFree => (uint)_hEvents.Length - _nextFree;

        public TokenAllocationResult AllocateToken(HANDLE hEvent, out short token)
            => AllocateToken(hEvent, Timeout.InfiniteTimeSpan, out token);

        public TokenAllocationResult AllocateToken(HANDLE hEvent, TimeSpan timeout, out short token)
        {
            var tk = Interlocked.Increment(ref _nextFree);
            var result = TokenAllocationResult.Success;
            if (tk == ushort.MaxValue)
            {
                if (!_wrapAround)
                {
                    token = default;
                    return TokenAllocationResult.NoMoreToken;
                }
                _nextFree = tk = 0;
                result = TokenAllocationResult.WrappedAround;
            }
            _hEvents[tk] = (hEvent, timeout);

            token = (short)tk;
            return result;
        }

        public void GetResult(short token)
        {
            var (hEvent, timeout) = _hEvents[(ushort)token];
            _ = hEvent == default ? 0 : WaitForSingleObject(hEvent, (uint)timeout.TotalMilliseconds) switch
            {
                WAIT_OBJECT_0 => default(int),
                WAIT_FAILED => throw new Win32Exception(),
                WAIT_ABANDONED => throw new TaskCanceledException(),
                _ => throw new Exception("Unreachable!"),
            };
        }

        public ValueTaskSourceStatus GetStatus(short token)
        {
            var (hEvent, _) = _hEvents[(ushort)token];
            return hEvent == default ? ValueTaskSourceStatus.Succeeded : WaitForSingleObject(hEvent, 0) switch
            {
                WAIT_OBJECT_0 => ValueTaskSourceStatus.Succeeded,
                WAIT_ABANDONED => ValueTaskSourceStatus.Canceled,
                WAIT_TIMEOUT => ValueTaskSourceStatus.Pending,
                WAIT_FAILED or _ => ValueTaskSourceStatus.Faulted,
            };
        }

        private struct CallbackContext
        {
            public IntPtr Callback, State, ExecutionContext;
        }

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            var (hEvent, timeout) = _hEvents[(ushort)token];
            if (_hEvents[(ushort)token] == default || WaitForSingleObject(hEvent, 0) == WAIT_OBJECT_0)
            {
                continuation(state);
                return;
            }

            var callbackContext = Helpers.Alloc<CallbackContext>();
            callbackContext->Callback = GCHandle.ToIntPtr(GCHandle.Alloc(continuation));
            callbackContext->State = GCHandle.ToIntPtr(GCHandle.Alloc(state));
            if (flags.HasFlag(ValueTaskSourceOnCompletedFlags.FlowExecutionContext))
            {
                callbackContext->ExecutionContext = GCHandle.ToIntPtr(GCHandle.Alloc(ExecutionContext.Capture()));
            }
            else if (flags.HasFlag(ValueTaskSourceOnCompletedFlags.UseSchedulingContext))
            {
                callbackContext->ExecutionContext = GCHandle.ToIntPtr(GCHandle.Alloc(SynchronizationContext.Current));
            }
            else
            {
                callbackContext->ExecutionContext = GCHandle.ToIntPtr(GCHandle.Alloc(null));
            }

            HANDLE newWait;
            if (RegisterWaitForSingleObject(&newWait, hEvent, &Callback, &callbackContext, (uint)timeout.TotalMilliseconds, 0) == 0)
            {
                ThrowHelper.ThrowWin32Exception("RegisterWaitForSingleObject failed [unexpected]");
            }


            [UnmanagedCallersOnly]
            static void Callback(void* pContext, byte _)
            {
                var pCallbackContext = (CallbackContext*)pContext;
                var hCallback = GCHandle.FromIntPtr(pCallbackContext->Callback);
                var hContext = GCHandle.FromIntPtr(pCallbackContext->State);
                var hEc = GCHandle.FromIntPtr(pCallbackContext->ExecutionContext);

                var callback = (Action<object?>)hCallback.Target!;
                var context = hCallback.Target!;
                var ec = hEc.Target!;

                if (ec is ExecutionContext o)
                {
                    ExecutionContext.Run(o, s => InvokeCallback(s), (callback, context));
                }
                else if (ec is SynchronizationContext s)
                {
                    s.Post(s => InvokeCallback(s), (callback, context));
                }
                else
                {
                    callback(context);
                }

                static void InvokeCallback(object? s)
                {
                    var (callback, state) = ((Action<object?>, object))s!;
                    callback(state);
                }

                hCallback.Free();
                hContext.Free();
                hEc.Free();

                Helpers.Free(pContext);
            }
        }
    }
}
