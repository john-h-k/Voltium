using System;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using TerraFX.Interop.LibC;
using Voltium.Common;
using Voltium.Common.Pix;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.Windows.WAIT;
using static TerraFX.Interop.Windows.E;

namespace Voltium.Core.Devices
{
    /// <summary>
    /// Reprents an OS event communicating between the kernelspace and the userspace
    /// </summary>
    public readonly unsafe struct OSEvent
    {
        private readonly IntPtr _hEvent; // HANDLE on windows. eventfd on linux

        private OSEvent(IntPtr hEvent) => _hEvent = hEvent;

        private OSEvent(int hEvent) => _hEvent = (IntPtr)hEvent;


        /// <summary>
        /// Creates a new instance of <see cref="OSEvent"/> from a Win32 event
        /// </summary>
        /// <param name="hEvent">The HANDLE to an event</param>
        public static OSEvent FromWin32Handle(IntPtr hEvent) => new(hEvent);


        /// <summary>
        /// Creates a new instance of <see cref="OSEvent"/> from an event file descriptor
        /// </summary>
        /// <param name="fd">The event file descriptor (eventfd)</param>
        public static OSEvent FromLinuxFileDescriptor(int fd) => new(fd);

        /// <summary>
        /// Whether the given <see cref="OSEvent"/> has completed
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                if (OperatingSystem.IsWindows())
                {
                    return WaitForSingleObject((HANDLE)_hEvent, 0) == WAIT_OBJECT_0;
                }
                else
                {
                    return ThrowHelper.ThrowPlatformNotSupportedException<bool>("Unknown OS");
                }
            }
        }

        /// <summary>
        /// Synchronously wait for the given <see cref="OSEvent"/> to finish
        /// </summary>
        public void WaitSync()
        {
            if (_hEvent == default)
            {
                return;
            }

            if (OperatingSystem.IsWindows())
            {
                _ = WaitForSingleObject((HANDLE)_hEvent, INFINITE);
            }
            else if (OperatingSystem.IsLinux())
            {
                fd_set set;
                FD_SET((int)_hEvent, &set);
                LibC.select(1, &set, null, null, null);


                static void FD_SET(int n, fd_set* p)
                {
                    nint mask = (nint)(1u << (n % 32));
                    p->fds_bits[(int)((uint)n / 32)] |= mask;
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                ThrowHelper.ThrowPlatformNotSupportedException("TODO");
            }
            else
            {
                ThrowHelper.ThrowPlatformNotSupportedException("Unknown OS");
            }
        }

        private struct CallbackData
        {
            public delegate*<object?, void> FnPtr;
            public IntPtr ObjectHandle;
            public IntPtr Event;
            public IntPtr WaitHandle;
        }

        /// <summary>
        /// Register a callback to be executed when the given <see cref="OSEvent"/> completes
        /// </summary>
        /// <typeparam name="T">The type of the opaque state passed to <paramref name="callback"/>. This must be a reference type</typeparam>
        /// <param name="state">The opaque stated passed to <paramref name="callback"/></param>
        /// <param name="callback">The callback to be executed when the gien <see cref="OSEvent"/> completes</param>
        public void RegisterCallback<T>(T state, delegate*<T, void> callback) where T : class?
        {
            if (_hEvent == default)
            {
                return;
            }


            if (IsCompleted)
            {
                callback(state);
                return;
            }

            if (OperatingSystem.IsWindows())
            {
                var gcHandle = GCHandle.Alloc(state);

                // see below, we store the managed object handle and fnptr target in this little block
                var context = Helpers.Alloc<CallbackData>();
                IntPtr newHandle;

                int err = RegisterWaitForSingleObject(
                    (HANDLE*)&newHandle,
                    (HANDLE)_hEvent,
                    &CallbackWrapper,
                    context,
                    INFINITE,
                    0
                );

                if (err == 0)
                {
                    ThrowHelper.ThrowWin32Exception("RegisterWaitForSingleObject failed");
                }

                context->FnPtr = (delegate*<object?, void>)callback;
                context->ObjectHandle = GCHandle.ToIntPtr(gcHandle);
                context->Event = _hEvent;
                context->WaitHandle = newHandle;
            }
            else if (OperatingSystem.IsLinux())
            {
                ThrowHelper.ThrowPlatformNotSupportedException("TODO");
            }
            else
            {
                ThrowHelper.ThrowPlatformNotSupportedException("Unknown OS");
            }
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

            // is this ok ???
            UnregisterWait((HANDLE)context->WaitHandle);
        }
    }
}
