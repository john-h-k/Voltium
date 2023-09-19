//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading.Tasks;
//using TerraFX.Interop;
//using Voltium.Core.Memory;
//using static TerraFX.Interop.Windows;
//using Buffer = Voltium.Core.Memory.Buffer;

//namespace Voltium.Core.Devices
//{
//    public unsafe partial class ComputeDevice
//    {
//        private sealed class SafeSharedResourceHandle : SafeHandle
//        {
//            public SafeSharedResourceHandle(IntPtr handle) : base(default, true)
//            {
//                this.handle = handle;
//            }

//            public override bool IsInvalid => handle == default;

//            protected override bool ReleaseHandle() => CloseHandle(handle) == 0;
//        }

//        public SafeHandle CreateSharedHandle(in Buffer res, string? name = null) => CreateSharedHandle((ID3D12DeviceChild*)_mapper.GetResourcePointer(res), name);
//        public SafeHandle CreateSharedHandle(in Texture res, string? name = null) => CreateSharedHandle((ID3D12DeviceChild*)_mapper.GetResourcePointer(res), name);
//        public SafeHandle CreateSharedHandle(in Fence fence, string? name = null) => CreateSharedHandle((ID3D12DeviceChild*)((D3D12Fence)fence).GetPointer(), name);

//        private SafeHandle CreateSharedHandle(ID3D12DeviceChild* pObject, string? name)
//        {
//            fixed (char* pName = name)
//            {
//                IntPtr handle;
//                ThrowIfFailed(DevicePointer->CreateSharedHandle(pObject, null, GENERIC_ALL, (ushort*)pName, &handle));
//                return new SafeSharedResourceHandle(handle);
//            }
//        }

//        public Fence OpenSharedFence(string name) => OpenSharedFence(GetHandleForName(name));
//        public Fence OpenSharedFence(SafeHandle handle) => OpenSharedFence(handle.DangerousGetHandle());
//        private Fence OpenSharedFence(IntPtr handle)
//        {
//            using UniqueComPtr<ID3D12Fence> fence = default;
//            ThrowIfFailed(DevicePointer->OpenSharedHandle(handle, fence.Iid, (void**)&fence));
//            return new D3D12Fence(fence.Move());
//        }

//        private IntPtr GetHandleForName(string name)
//        {
//            fixed (char* pName = name)
//            {
//                IntPtr handle;
//                ThrowIfFailed(DevicePointer->OpenSharedHandleByName((ushort*)pName, GENERIC_ALL, &handle));
//                return handle;
//            }
//        }
//    }
//}
