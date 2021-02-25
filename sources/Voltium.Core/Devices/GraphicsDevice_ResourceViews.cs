using System;
using System.Collections.Generic;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.CommandBuffer;
using Voltium.Core.Memory;
using Voltium.Core.Views;
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


        /// <summary>
        /// Allocates a range of descriptor handles in the resource descriptor heap, used for CBVs, SRVs, and UAVs
        /// </summary>
        /// <param name="descriptorCount"></param>
        /// <returns></returns>
        public DescriptorAllocation AllocateResourceDescriptors(DescriptorType type, int descriptorCount)
        {
            var handles = _device.CreateDescriptorSet(type,  descriptorCount);
            return handles;
        }

        public void CreateDefaultView(ref View view, in Buffer buff)
        {
            view.Handle = _device.CreateView(view.Set, view.Index, buff.Handle);
        }
        public void CreateDefaultView(ref View view, in Texture buff)
        {
            view.Handle = _device.CreateView(view.Set, view.Index, buff.Handle);
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
