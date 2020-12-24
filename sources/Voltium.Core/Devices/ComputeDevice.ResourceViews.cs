using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Memory;
using Voltium.Core.Views;
using static TerraFX.Interop.Windows;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core.Devices
{

    public unsafe partial class ComputeDevice
    {
        private uint ResourceCount; // 8 million lol

        private protected virtual void CreateDescriptorHeaps()
        {
            Resources = CreateDescriptorHeap(DescriptorHeapType.ConstantBufferShaderResourceOrUnorderedAccessView, ResourceCount, true);
        }

        public void CopyDescriptors(DescriptorSpan source, DescriptorSpan dest)
        {
            if (source.Type != dest.Type)
            {
                ThrowHelper.ThrowArgumentException(nameof(dest), "Destination DescriptorSpan was of a different type to source");
            }

            CopyDescriptors((uint)source.Length, dest.Cpu, source.Cpu, (D3D12_DESCRIPTOR_HEAP_TYPE)source.Type);
        }

        /// <summary>
        /// Allocates a range of descriptor handles in the resource descriptor heap, used for CBVs, SRVs, and UAVs
        /// </summary>
        /// <param name="descriptorCount"></param>
        /// <returns></returns>
        public DescriptorAllocation AllocateResourceDescriptors(int descriptorCount)
        {
            var handles = Resources.AllocateHandles(descriptorCount);
            return handles;
        }

        public void CreateConstantBufferView(in Buffer resource, in DescriptorHandle descriptor)
        {
            var desc = new D3D12_CONSTANT_BUFFER_VIEW_DESC
            {
                BufferLocation = resource.GpuAddress,
                SizeInBytes = resource.Length
            };

            DevicePointer->CreateConstantBufferView(&desc, descriptor.CpuHandle);
        }

        public void CreateConstantBufferView(in Buffer resource, uint offset, in DescriptorHandle descriptor)
            => CreateConstantBufferView(resource, offset, resource.Length - offset, descriptor);

        public void CreateConstantBufferView(in Buffer resource, uint offset, uint length, in DescriptorHandle descriptor)
        {
            var desc = new D3D12_CONSTANT_BUFFER_VIEW_DESC
            {
                BufferLocation = resource.GpuAddress + offset,
                SizeInBytes = length
            };

            DevicePointer->CreateConstantBufferView(&desc, descriptor.CpuHandle);
        }

        /// <summary>
        /// Creates a shader resource view to a <see cref="Buffer"/>
        /// </summary>
        /// <param name="resource">The <see cref="Buffer"/> resource to create the view for</param>
        /// <param name="desc">The <see cref="BufferShaderResourceViewDesc"/> describing the metadata used to create the view</param>
        /// <param name="descriptor">The <see cref="DescriptorHandle"/> to create the view at</param>
        public void CreateShaderResourceView(in Buffer resource, in BufferShaderResourceViewDesc desc, in DescriptorHandle descriptor)
        {
            Unsafe.SkipInit(out D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc);
            srvDesc.Format = (DXGI_FORMAT)desc.Format;
            srvDesc.ViewDimension = D3D12_SRV_DIMENSION.D3D12_SRV_DIMENSION_BUFFER;
            srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING; // TODO
            srvDesc.Anonymous.Buffer.FirstElement = desc.Offset;
            srvDesc.Anonymous.Buffer.Flags = desc.IsRaw ? D3D12_BUFFER_SRV_FLAGS.D3D12_BUFFER_SRV_FLAG_RAW : D3D12_BUFFER_SRV_FLAGS.D3D12_BUFFER_SRV_FLAG_NONE;
            srvDesc.Anonymous.Buffer.NumElements = desc.ElementCount;
            srvDesc.Anonymous.Buffer.StructureByteStride = desc.ElementStride;

            DevicePointer->CreateShaderResourceView(resource.GetResourcePointer(), &srvDesc, descriptor.CpuHandle);
        }

        /// <summary>
        /// Creates a shader resource view to a <see cref="Buffer"/>
        /// </summary>
        /// <param name="resource">The <see cref="Buffer"/> resource to create the view for</param>
        /// <param name="descriptor">The <see cref="DescriptorHandle"/> to create the view at</param>
        public void CreateShaderResourceView(in Buffer resource, in DescriptorHandle descriptor)
        {
            DevicePointer->CreateShaderResourceView(resource.GetResourcePointer(), null, descriptor.CpuHandle);
        }

        /// <summary>
        /// Creates a unordered access view to a <see cref="Buffer"/>
        /// </summary>
        /// <param name="resource">The <see cref="Buffer"/> resource to create the view for</param>
        /// <param name="descriptor">The <see cref="DescriptorHandle"/> to create the view at</param>
        public void CreateUnorderedAccessView(in Buffer resource, in DescriptorHandle descriptor)
        {
            DevicePointer->CreateUnorderedAccessView(resource.GetResourcePointer(), null, null, descriptor.CpuHandle);
        }


        /// <summary>
        /// Creates a unordered access view to a <see cref="Buffer"/>
        /// </summary>
        /// <param name="resource">The <see cref="Buffer"/> resource to create the view for</param>
        /// <param name="counter">The resource to use as a counter</param>
        /// <param name="descriptor">The <see cref="DescriptorHandle"/> to create the view at</param>
        public void CreateUnorderedAccessView(in Buffer resource, in Buffer counter, in DescriptorHandle descriptor)
        {
            DevicePointer->CreateUnorderedAccessView(resource.GetResourcePointer(), counter.GetResourcePointer(), null, descriptor.CpuHandle);
        }
    }
}
