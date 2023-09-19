using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TerraFX.Interop.DirectX;

namespace Voltium.Common.Debugging
{
    internal unsafe static class LifetimeHelpers
    {
        private const string DoNotUseWithoutDebugger = "Should not use this without a debugger";

        internal static void RegisterForDeletionCallback<T>(T* ptr, delegate* unmanaged<void*, void> callback, object? data = null) where T : unmanaged

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
                    handle = GCHandle.ToIntPtr(GCHandle.Alloc(data));
                }

                Guard.ThrowIfFailed(notifier->RegisterDestructionCallback(callback, (void*)handle, null));
            }
            finally
            {
                _ = notifier->Release();
            }
        }

        internal static void BreakOnDeletion<T>(T* ptr, object? data = null) where T : unmanaged
        {
            if (data is null)
            {
                data =
#if REFLECTION
                    $"{typeof(T).Name}"
#else
                    $"D3D object"
#endif
                    + $" with name '{DebugHelpers.GetName(ptr)}' is being deleted";
            }

            RegisterForDeletionCallback(ptr, &BreakOnDeletion, data);
        }

        [UnmanagedCallersOnly]
        private static void BreakOnDeletion(void* data)
        {
            string? text = null;
            if (data != default)
            {
                object obj = GCHandle.FromIntPtr((IntPtr)data);
                text = obj as string;
            }

            if (Debugger.IsAttached)
            {
                // Inspect 'text'
                GC.KeepAlive(text);
                Debugger.Break();
            }
            else
            {
                throw new InvalidOperationException(DoNotUseWithoutDebugger);
            }
        }
    }
}
