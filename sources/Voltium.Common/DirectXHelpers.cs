using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TerraFX.Interop;

namespace Voltium.Common
{
    internal static class DirectXHelpers
    {
        [Conditional("DEBUG")]
        [Conditional("EXTENDED_ERROR_INFORMATION")]
        public static unsafe void SetObjectName<T>(T* obj, [CallerArgumentExpression("obj")] string name) where T : unmanaged
        {
            fixed (char* p = name)
            {
                    // this will assert is is a valid cast in debug anyway
                    Guard.ThrowIfFailed(ComPtr.UpCast<T, ID3D12Object>(obj).Get()->SetName((ushort*)p));
            }
        }

        public static unsafe string GetObjectName<T>(T* obj) where T : unmanaged
        {
            int size = StackSentinel.MaxStackallocBytes;

            byte* buff = stackalloc byte[size];

            var guid = Windows.WKPDID_D3DDebugObjectNameW;

            return new ReadOnlySpan<char>(buff, Math.Max(0, (size / sizeof(char)) - 1) /* remove null char */).ToString();
        }
    }
}
