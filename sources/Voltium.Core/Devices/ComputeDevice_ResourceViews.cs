using System.Runtime.CompilerServices;
using System.Diagnostics;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Memory;
using static TerraFX.Interop.Windows;
using Buffer = Voltium.Core.Memory.Buffer;
using Voltium.Core.NativeApi;

namespace Voltium.Core.Devices
{
    public unsafe partial class ComputeDevice
    {
        public void UpdateDescriptors(in ViewSet views, in DescriptorAllocation descriptors)
            => UpdateDescriptors(views, descriptors, views.Length);
        public void UpdateDescriptors(in ViewSet views, in DescriptorAllocation descriptors, uint count)
            => UpdateDescriptors(views, 0, descriptors, 0, count);
        public void UpdateDescriptors(in ViewSet views, uint firstView, in DescriptorAllocation descriptors, uint firstDescriptor, uint count)
        {
            _device.UpdateDescriptors(views.Handle, firstView, descriptors.Handle, firstDescriptor, count);
        }

        public DescriptorAllocation AllocateResourceDescriptors(DescriptorType type, uint descriptorCount)
        {
            static void Dispose(object o, ref DescriptorSetHandle handle)
            {
                Debug.Assert(o is ComputeDevice);
                Unsafe.As<ComputeDevice>(o)._device.DisposeDescriptorSet(handle);
            }

            var handles = _device.CreateDescriptorSet(type, descriptorCount);
            return new(descriptorCount, type, handles, new(this, &Dispose));
        }

        public ViewSet CreateViewSet(uint count)
        {
            static void Dispose(object o, ref ViewSetHandle handle)
            {
                Debug.Assert(o is ComputeDevice);
                Unsafe.As<ComputeDevice>(o)._device.DisposeViewSet(handle);
            }

            var handles = _device.CreateViewSet(count);
            return new(handles, count, new(this, &Dispose));
        }

        public View CreateDefaultView(in ViewSet set, uint index, in Buffer buff)
        {
            static void Dispose(object o, ref ViewHandle handle)
            {
                Debug.Assert(o is ComputeDevice);
                //Unsafe.As<ComputeDevice>(o)._device.DisposeView(handle);
            }

            var view = _device.CreateView(set.Handle, index, buff.Handle);
            return new(view, new(this, &Dispose));
        }
    }
}
