using System;
using System.Runtime.InteropServices;
using static TerraFX.Interop.Windows;

namespace Voltium.Core.Devices
{

    public unsafe partial class ComputeDevice
    {
        public sealed class SafeSharedResourceHandle : SafeHandle
        {
            public SafeSharedResourceHandle(IntPtr handle) : base(default, true)
            {
                this.handle = handle;
            }

            public override bool IsInvalid => handle == default;

            protected override bool ReleaseHandle() => CloseHandle(handle) == 0;
        }






    }
}
