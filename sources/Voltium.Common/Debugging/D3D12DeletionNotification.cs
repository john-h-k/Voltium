using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;

namespace Voltium.Common.Debugging
{
    internal unsafe static class D3D12DeletionNotification
    {
        public static void RegisterForDeletionCallback<T>(ComPtr<T> ptr, delegate* stdcall<void*, void> callback, object? data = null) where T : unmanaged
        {
            if (!ptr.TryQueryInterface<ID3DDestructionNotifier>(out var notifier))
            {
                ThrowHelper.ThrowArgumentException("Type could not query for interface ID3DDestructionNotifier");
            }

            using (notifier)
            {
                var handle = default(IntPtr);
                if (data is object)
                {
                    handle = GCHandle.ToIntPtr(GCHandle.Alloc(handle));
                }

                Guard.ThrowIfFailed(notifier.Get()->RegisterDestructionCallback(callback, (void*)handle, null));
            }
        }

        public static void BreakOnDeletion<T>(ComPtr<T> ptr, object? data = null) where T : unmanaged
        {
            if (data is null)
            {
                data = $"'{typeof(T).Name} with name '{DirectXHelpers.GetObjectName(ptr.Get())}' is being deleted";
            }

            RegisterForDeletionCallback(ptr, (delegate* stdcall<void*, void>)(delegate* <IntPtr, void>)&BreakOnDeletion, data);
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
