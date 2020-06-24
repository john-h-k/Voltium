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
                Guard.True(ComPtr.TryQueryInterface(obj, out ID3D12Object* result));
                Guard.ThrowIfFailed(result->SetName((ushort*)p));
                result->Release();
            }
        }

        [Conditional("DEBUG")]
        [Conditional("EXTENDED_ERROR_INFORMATION")]
        public static unsafe void SetObjectName<T>(T obj, [CallerArgumentExpression("obj")] string name) where T : INameable
            => SetObjectName(obj.GetNameable(), name);

        public static unsafe string GetObjectName<T>(T* obj) where T : unmanaged
        {
            uint size = StackSentinel.MaxStackallocBytes;

            byte* buff = stackalloc byte[(int)size];

            var guid = Windows.WKPDID_D3DDebugObjectNameW;

            if (!ComPtr.TryQueryInterface(obj, out ID3D12Object* result))
            {
                return "Not ID3D12Object";
            }

            _ = result->GetPrivateData(&guid, &size, buff);
            result->Release();

            return new ReadOnlySpan<char>(buff, Math.Max(0, ((int)size / sizeof(char)) - 1) /* remove null char */).ToString();
        }

        public static unsafe string GetObjectName<T>(T obj) where T : INameable
            => GetObjectName(obj.GetNameable());
    }
}
