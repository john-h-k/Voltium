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
    internal static unsafe class D3D12DeletionNotification
    {
        public static void RegisterForDeletionCallback<T>(T* ptr, delegate* stdcall<void*, void> callback, object? data = null) where T : unmanaged
        {
            if (!ComPtr.TryQueryInterface<T, ID3DDestructionNotifier>(ptr, out var notifier))
            {
                ThrowHelper.ThrowArgumentException("Type could not query for interface ID3DDestructionNotifier");
            }

            try
            { 
                var handle = default(IntPtr);
                if (data is object)
                {
                    handle = GCHandle.ToIntPtr(GCHandle.Alloc(handle));
                }

                Guard.ThrowIfFailed(notifier->RegisterDestructionCallback(callback, (void*)handle, null));
            }
            finally
            {
                _ = notifier->Release();
            }
        }

        public static void BreakOnDeletion<T>(T* ptr, object? data = null) where T : unmanaged
        {
            if (data is null)
            {
                data =
#if REFLECTION
                    $"{typeof(T).Name}"
#else
                    $"D3D object"
#endif
                    + $" with name '{DirectXHelpers.GetObjectName(ptr)}' is being deleted";
            }

            RegisterForDeletionCallback(ptr, (delegate* stdcall<void*, void>)(delegate* <IntPtr, void>)&BreakOnDeletion, data);
        }

        [UnmanagedCallersOnly]
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
