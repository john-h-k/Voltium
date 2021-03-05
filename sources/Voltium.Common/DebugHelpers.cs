using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TerraFX.Interop;

namespace Voltium.Common
{
    /// <summary>
    /// Provides debugging utilities
    /// </summary>
    public static unsafe class DebugHelpers
    {
        [Conditional("DEBUG")]
        [Conditional("EXTENDED_ERROR_INFORMATION")]
        internal static unsafe void SetName<T>(T* obj, [CallerArgumentExpression("obj")] string name) where T : unmanaged
        {
            fixed (char* p = name)
            {
                var result = (ID3D12Object*)obj;

                Guard.ThrowIfFailed(result->SetName((ushort*)p));
            }
        }

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

            return string.Create((int)size, (UniqueComPtr<ID3D12Object>)result, static (buff, ptr) =>
            {
                var guid = Windows.WKPDID_D3DDebugObjectNameW;
                int size = buff.Length * sizeof(char);
                fixed (char* pBuff = buff)
                {
                    Guard.ThrowIfFailed(ptr.Ptr->GetPrivateData(&guid, (uint*)&size, pBuff));
                }
            });
        }

        internal static unsafe void SetPrivateData<T, TData>(T* obj, in Guid did, in TData data) where T : unmanaged where TData : unmanaged
        {
            var unknown = (ID3D12Object*)obj;

            fixed (Guid* piid = &did)
            fixed (TData* pData = &data)
            {
                Guard.ThrowIfFailed(unknown->SetPrivateData(piid, (uint)sizeof(TData), pData));
            }
        }


        internal static uint GetPrivateDataSize<T>(T* obj, in Guid did) where T : unmanaged
        {
            uint size;

            fixed (Guid* pid = &did)
            {
                Guard.ThrowIfFailed(((ID3D12Object*)obj)->GetPrivateData(pid, &size, null));
            }

            return size;
        }

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
    }
}
