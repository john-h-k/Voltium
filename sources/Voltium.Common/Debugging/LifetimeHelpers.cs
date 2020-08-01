using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TerraFX.Interop;

namespace Voltium.Common.Debugging
{
    internal unsafe static class LifetimeHelpers
    {
        private const string DoNotUseWithoutDebugger = "Should not use this without a debugger";

        public static bool HasSingleRef<T>(in T value) where T : IInternalD3D12Object
            => GetRefCount(value) == 1;

        public static int GetRefCount<T>(in T value) where T : IInternalD3D12Object
        {
            var ptr = value.GetPointer();
            _ = ptr->AddRef();
            return (int)ptr->Release();
        }

        public static void RegisterForDeletionCallback<T>(in T ptr, delegate* stdcall<void*, void> callback, object? data = null) where T : unmanaged, IInternalD3D12Object
            => RegisterForDeletionCallback(ptr.GetPointer(), callback, data);

        internal static void RegisterForDeletionCallback<T>(T* ptr, delegate* stdcall<void*, void> callback, object? data = null) where T : unmanaged

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

        public static void BreakOnDeletion<T>(in T ptr, object? data = null) where T : unmanaged, IInternalD3D12Object
            => BreakOnDeletion(ptr.GetPointer(), data);

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

            RegisterForDeletionCallback(ptr, (delegate* stdcall<void*, void>)(delegate*<IntPtr, void>)&BreakOnDeletion, data);
        }

        [UnmanagedCallersOnly]
        private static void BreakOnDeletion(IntPtr data)
        {
            string? text = null;
            if (data != default)
            {
                object obj = GCHandle.FromIntPtr(data);
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
