using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TerraFX.Interop;

namespace Voltium.Common
{
    /// <summary>
    /// Provides debugging utilities
    /// </summary>
    public static class DebugHelpers
    {
        /// <summary>
        /// When <c>DEBUG</c> or <c>EXTENDED_ERROR_INFORMATION</c> is defined, sets the object name
        /// </summary>
        /// <typeparam name="T">The type of the object to name</typeparam>
        /// <param name="value">The object to name</param>
        /// <param name="name">The name</param>
        [Conditional("DEBUG")]
        [Conditional("EXTENDED_ERROR_INFORMATION")]
        public static unsafe void SetName<T>(this ref T value, string name) where T : struct, IInternalD3D12Object
            => SetName(value.GetPointer(), name);

        [Conditional("DEBUG")]
        [Conditional("EXTENDED_ERROR_INFORMATION")]
        internal static unsafe void SetName<T>(T* obj, [CallerArgumentExpression("obj")] string name) where T : unmanaged
        {
            fixed (char* p = name)
            {
                var result = (ID3D12Object*)obj;

                var guid = Windows.WKPDID_D3DDebugObjectNameW;
                Guard.ThrowIfFailed(result->SetPrivateData(&guid, ((uint)name.Length * sizeof(char)) + 1, p));
            }
        }

        [Conditional("DEBUG")]
        [Conditional("EXTENDED_ERROR_INFORMATION")]
        internal static unsafe void SetName<T>(this T obj, [CallerArgumentExpression("obj")] string name) where T : class, IInternalD3D12Object
            => SetName(obj.GetPointer(), name);

        internal static unsafe string GetName<T>(T* obj) where T : unmanaged
        {
            var guid = Windows.WKPDID_D3DDebugObjectNameW;

            if (!ComPtr.TryQueryInterface(obj, out ID3D12Object* result))
            {
                return "Not ID3D12Object";
            }

            uint size;
            int hr = result->GetPrivateData(&guid, &size, null);

            if (hr == Windows.DXGI_ERROR_NOT_FOUND)
            {
                return "<Unnamed>";
            }

            Guard.ThrowIfFailed(hr, "result->GetPrivateData(&guid, &size, null)");

            return string.Create((int)size, (UniqueComPtr<ID3D12Object>)result, (buff, ptr) =>
            {
                var guid = Windows.WKPDID_D3DDebugObjectNameW;
                int size = buff.Length * sizeof(char);
                fixed (char* pBuff = buff)
                {
                    Guard.ThrowIfFailed(result->GetPrivateData(&guid, (uint*)&size, pBuff));
                }
            });
        }

        internal static unsafe void SetPrivateData<T, TData>(T obj, in Guid did, in TData data) where T : IInternalD3D12Object where TData : unmanaged
            => SetPrivateData(obj.GetPointer(), did, data);

        internal static unsafe void SetPrivateData<T, TData>(T* obj, in Guid did, in TData data) where T : unmanaged where TData : unmanaged
        {
            var unknown = (ID3D12Object*)obj;

            fixed (Guid* piid = &did)
            fixed (TData* pData = &data)
            {
                Guard.ThrowIfFailed(unknown->SetPrivateData(piid, (uint)sizeof(TData), pData));
            }
        }

        internal static unsafe TData GetPrivateData<T, TData>(this ref T obj, in Guid did, out int bytesWritten) where T : struct, IInternalD3D12Object where TData : unmanaged
            => GetPrivateData<ID3D12Object, TData>(obj.GetPointer(), did, out bytesWritten);

        internal static unsafe TData GetPrivateData<T, TData>(T* obj, in Guid did, out int bytesWritten) where T : unmanaged where TData : unmanaged
        {
            var unknown = (ID3D12Object*)obj;

            uint size;
            TData data;

            fixed (Guid* piid = &did)
            {
                int hr = unknown->GetPrivateData(piid, &size, &data);
                if (hr == Windows.DXGI_ERROR_NOT_FOUND)
                {
                    ThrowHelper.ThrowKeyNotFoundException($"Key '{did}' had not been set");
                }

                Guard.ThrowIfFailed(hr, "unknown->GetPrivateData(piid, &size, &data)");
            }

            bytesWritten = (int)size;
            return data;
        }

        internal static unsafe string GetName<T>(this T obj) where T : IInternalD3D12Object
            => GetName(obj.GetPointer());
    }
}
