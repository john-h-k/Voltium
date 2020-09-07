using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop;

namespace Voltium.Common
{
    internal static unsafe class Linux
    {
        [DllImport("libc")]
        public static extern nint read(int fd, void* buf, nuint count);


        [DllImport("libc")]
        public static extern int poll(pollfd *fds, nuint nfds, int timeout);

        public const int POLLIN = 0x1, POLLRDNORM = 0x40;
    }

    struct pollfd
    {
        public int fd;         /* file descriptor */
        public short events;     /* requested events */
        public short revents;    /* returned events */
    };

    internal unsafe struct NativeEvent
    {
        private nint _data;

        private IntPtr Handle => _data;
        private int FileDescriptor => Unsafe.As<nint, int>(ref _data);

        public NativeEvent(IntPtr win32Handle)
        {
            Debug.Assert(PlatformInfo.IsLinux);

            _data = win32Handle;
        }

        public NativeEvent(int linuxFileDescriptor)
        {
            Debug.Assert(PlatformInfo.IsLinux);

            _data = 0;

            Unsafe.As<nint, int>(ref _data) = linuxFileDescriptor;
        }

        public void Block()
        {
            if (PlatformInfo.IsLinux)
            {
                _ = Linux.read(FileDescriptor, null, 0);
            }
            else
            {
                _ = Windows.WaitForSingleObjectEx(Handle, Windows.INFINITE, Windows.FALSE);
            }
        }

        private struct CallbackData
        {
            public delegate*<object?, void> FnPtr;
            public IntPtr ObjectHandle;
        }

        internal bool CheckIsCompleted()
        {

            if (PlatformInfo.IsLinux)
            {
                const int fdRead = Linux.POLLIN | Linux.POLLRDNORM;

                var pollfd = new pollfd
                {
                    fd = FileDescriptor,
                    events = fdRead
                };

                _ = Linux.poll(&pollfd, 1, 0);

                if ((pollfd.revents & fdRead) != 0)
                {
                    _ = Linux.read(FileDescriptor, null, 0);
                }

                return (pollfd.revents & fdRead) != 0;
            }
            else
            {
                return Windows.WaitForSingleObjectEx(Handle, 0, Windows.FALSE) == Windows.WAIT_OBJECT_0;
            }
        }

        //internal void RegisterCallback<T>(T state, delegate*<T, void> onFinished) where T : class?
        //{
        //    Debug.Assert(state is T);
        //    if (IsCompleted)
        //    {
        //        onFinished(state);
        //        return;
        //    }

        //    IntPtr newHandle;
        //    IntPtr handle = Windows.CreateEventW(null, Windows.FALSE, Windows.FALSE, null);

        //    var gcHandle = GCHandle.Alloc(state);

        //    // see below, we store the managed object handle and fnptr target in this little block
        //    var context = Helpers.Alloc<CallbackData>();
        //    context->FnPtr = (delegate*<object?, void>)onFinished;
        //    context->ObjectHandle = GCHandle.ToIntPtr(gcHandle);

        //    _device!.ThrowIfFailed(_fence.Ptr->SetEventOnCompletion(_reached, handle));
        //    int err = Windows.RegisterWaitForSingleObject(
        //        &newHandle,
        //        handle,
        //        (delegate* stdcall<void*, byte, void>)(delegate*<CallbackData*, byte, void>)&CallbackWrapper,
        //        context,
        //        Windows.INFINITE,
        //        0
        //    );

        //    if (err == 0)
        //    {
        //        ThrowHelper.ThrowWin32Exception("RegisterWaitForSingleObject failed");
        //    }
        }
    }
}
}
