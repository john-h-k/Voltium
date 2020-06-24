using System;
using System.Runtime.InteropServices;
using Voltium.Common;
using static TerraFX.Interop.Windows;

namespace Voltium.RenderEngine.Jobs
{
    internal unsafe struct NativeThread
    {
        private IntPtr _handle;
        public IntPtr GetHandle() => _handle;

        public void SetAffinity(nuint mask)
        {
            var oldMask = SetThreadAffinityMask(_handle, mask);

            if (oldMask == default)
            {
                ThrowHelper.ThrowExternalException($"Thread affinity setting failed with error: {Marshal.GetLastWin32Error()}");
            }
        }

        private const uint CREATE_SUSPENDED = 0x00000004;

        public static NativeThread Create(nuint stackSize, delegate* stdcall<void*, uint> pThreadStart, void* pThreadData, bool start = false)
        {
            uint id;

            IntPtr handle = CreateThread(
                null,
                stackSize,
                pThreadStart,
                pThreadData,
                start ? 0 : CREATE_SUSPENDED,
                &id
            );

            if (handle == default)
            {
                ThrowHelper.ThrowExternalException($"Thread creation failed with error: {Marshal.GetLastWin32Error()}");
            }

            return new NativeThread { _handle = handle };
        }
    }
}
