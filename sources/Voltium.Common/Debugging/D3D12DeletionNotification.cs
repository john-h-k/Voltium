using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;

namespace Voltium.Common.Debugging
{
    internal unsafe static class D3D12DeletionNotification
    {
        public delegate void DeletionCallback(IntPtr pData);

        public static void RegisterForDeletionCallback<T>(ComPtr<T> ptr, DeletionCallback callback, object? data = null) where T : unmanaged
        {
            if (!ptr.TryQueryInterface<ID3DDestructionNotifier>(out var notifier))
            {
                ThrowHelper.ThrowArgumentException("Type could not query for interface ID3DDestructionNotifier");
            }

            using (notifier)
            {
                var nativeCallback = Marshal.GetFunctionPointerForDelegate(callback);

                var handle = default(IntPtr);
                if (data is object)
                {
                    handle = GCHandle.ToIntPtr(GCHandle.Alloc(handle));
                }

                notifier.Get()->RegisterDestructionCallback(nativeCallback, (void*)handle, null);
            }
        }

        public static void BreakOnDeletion<T>(ComPtr<T> ptr, object? data = null) where T : unmanaged
        {
            if (data is null)
            {
                data = $"'{typeof(T).Name} with name '{DirectXHelpers.GetObjectName(ptr.Get())}' is being deleted";
            }

            RegisterForDeletionCallback(ptr, BreakOnDeletion, data);
        }

        private static void BreakOnDeletion(IntPtr data)
        {
            if (data != default)
            {
                object obj = GCHandle.FromIntPtr(data);
                if (obj is string s)
                {
                    // inspect s for details
                }
            }

            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
            else
            {
                throw new InvalidOperationException("Should not use this without a debugger");
            }    
        }
    }
}
