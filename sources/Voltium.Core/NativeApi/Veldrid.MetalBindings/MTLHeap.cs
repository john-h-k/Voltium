using System;
using System.Runtime.InteropServices;

namespace Veldrid.MetalBindings
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct MTLHeap
    {
        public readonly IntPtr NativePtr;
        public MTLHeap(IntPtr ptr) => NativePtr = ptr;
        public bool IsNull => NativePtr == IntPtr.Zero;

        public void* size() => ObjectiveCRuntime.IntPtr_objc_msgSend(NativePtr, sel_size).ToPointer();
    
        private static readonly Selector sel_size = "size";
        private static readonly Selector sel_type = "type";
        private static readonly Selector sel_storageMode = "storageMode";
        private static readonly Selector sel_cpuCacheMode = "cpuCacheMode";
        private static readonly Selector sel_hazardTrackingMode = "hazardTrackingMode";
        private static readonly Selector sel_usedSize = "usedSize";
        private static readonly Selector sel_resourceOptions = "resourceOptions";
        private static readonly Selector sel_currentAllocatedSize = "currentAllocatedSize";
        private static readonly Selector sel_maxAvailableSize = "maxAvailableSize:";
    }
}