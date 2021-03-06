using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.CommandBuffer;
using Voltium.Core.Memory;
using Voltium.Core.NativeApi;
using static TerraFX.Interop.Windows;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core.Devices
{
    public unsafe partial class GraphicsDevice
    {

        /// <summary>
        /// Allocates a range of descriptor handles in the resource descriptor heap, used for CBVs, SRVs, and UAVs
        /// </summary>
        /// <param name="descriptorCount"></param>
        /// <returns></returns>
        public DescriptorAllocation AllocateSamplerDescriptors(int descriptorCount)
        {
            throw null!;
            //var handles = _device.CreateDescriptorSet(descriptorCount);
            //return handles;
        }



        public View CreateDefaultView(in ViewSet set, uint index, in Texture buff)
        {
            static void Dispose(object o, ref ViewHandle handle)
            {
                Debug.Assert(o is GraphicsDevice);
                //Unsafe.As<GraphicsDevice>(o)._device.DisposeView(handle);
            }

            var view = _device.CreateView(set.Handle, index, buff.Handle);
            return new(view, new(this, &Dispose));
        }

        /// <summary>
        /// Creates a new <see cref="Sampler"/>
        /// </summary>
        /// <param name="sampler"></param>
        /// <param name="descriptor"></param>
        /// <returns></returns>
        public void CreateSampler(in Sampler sampler, DescriptorHandle descriptor)
        {
            throw new NotImplementedException();
        }
    }
}
