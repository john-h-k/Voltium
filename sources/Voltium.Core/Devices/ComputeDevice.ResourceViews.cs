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
            UavCbvSrvs = CreateDescriptorHeap(DescriptorHeapType.ConstantBufferShaderResourceOrUnorderedAccessView, ResourceCount, true);
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
            var handles = UavCbvSrvs.AllocateHandles(descriptorCount);
            return handles;
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



            DevicePointer->CreateShaderResourceView(resource.Resource.GetResourcePointer(), &srvDesc, descriptor.CpuHandle);
        }

        /// <summary>
        /// Creates a shader resource view to a <see cref="Buffer"/>
        /// </summary>
        /// <param name="resource">The <see cref="Buffer"/> resource to create the view for</param>
        /// <param name="descriptor">The <see cref="DescriptorHandle"/> to create the view at</param>
        public void CreateShaderResourceView(in Buffer resource, in DescriptorHandle descriptor)
        {
            DevicePointer->CreateShaderResourceView(resource.Resource.GetResourcePointer(), null, descriptor.CpuHandle);
        }

        /// <summary>
        /// Creates a shader resource view to a <see cref="Texture"/>
        /// </summary>
        /// <param name="descriptor">The <see cref="DescriptorHandle"/> to create the view at</param>
        /// <param name="resource">The <see cref="Texture"/> resource to create the view for</param>
        public void CreateShaderResourceView(in Texture resource, in DescriptorHandle descriptor)
        {
            DevicePointer->CreateShaderResourceView(resource.Resource.GetResourcePointer(), null, descriptor.CpuHandle);
        }

        /// <summary>
        /// Creates a shader resource view to a <see cref="Texture"/>
        /// </summary>
        /// <param name="descriptor">The <see cref="DescriptorHandle"/> to create the view at</param>
        /// <param name="resource">The <see cref="Texture"/> resource to create the view for</param>
        /// <param name="desc">The <see cref="TextureShaderResourceViewDesc"/> describing the metadata used to create the view</param>
        public void CreateShaderResourceView( in Texture resource, in TextureShaderResourceViewDesc desc, in DescriptorHandle descriptor)
        {
            // multisampled textures can be created without a desc
            if (resource.Msaa.SampleCount > 1)
            {
                CreateShaderResourceView(resource, descriptor);
            }

            D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc;

            switch (resource.Dimension)
            {
                case TextureDimension.Tex1D:
                    srvDesc.Anonymous.Texture1D.MipLevels = desc.MipLevels;
                    srvDesc.Anonymous.Texture1D.MostDetailedMip = desc.MostDetailedMip;
                    srvDesc.Anonymous.Texture1D.ResourceMinLODClamp = desc.ResourceMinLODClamp;
                    srvDesc.ViewDimension = D3D12_SRV_DIMENSION.D3D12_SRV_DIMENSION_TEXTURE1D;
                    break;

                case TextureDimension.Tex2D:
                    srvDesc.Anonymous.Texture2D.MipLevels = desc.MipLevels;
                    srvDesc.Anonymous.Texture2D.MostDetailedMip = desc.MostDetailedMip;
                    srvDesc.Anonymous.Texture2D.ResourceMinLODClamp = desc.ResourceMinLODClamp;
                    srvDesc.Anonymous.Texture2D.PlaneSlice = desc.PlaneSlice;
                    srvDesc.ViewDimension = D3D12_SRV_DIMENSION.D3D12_SRV_DIMENSION_TEXTURE2D;
                    break;

                case TextureDimension.Tex3D:
                    srvDesc.Anonymous.Texture3D.MipLevels = desc.MipLevels;
                    srvDesc.Anonymous.Texture3D.MostDetailedMip = desc.MostDetailedMip;
                    srvDesc.Anonymous.Texture3D.ResourceMinLODClamp = desc.ResourceMinLODClamp;
                    srvDesc.ViewDimension = D3D12_SRV_DIMENSION.D3D12_SRV_DIMENSION_TEXTURE3D;
                    break;
            }

            srvDesc.Format = (DXGI_FORMAT)desc.Format;
            srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING; // TODO

            DevicePointer->CreateShaderResourceView(resource.Resource.GetResourcePointer(), &srvDesc, descriptor.CpuHandle);
        }


        /// <summary>
        /// Creates a shader resource view to a <see cref="Texture"/>
        /// </summary>
        /// <param name="descriptor">The <see cref="DescriptorHandle"/> to create the view at</param>
        /// <param name="resource">The <see cref="Texture"/> resource to create the view for</param>
        public void CreateUnorderedAccessView(in Texture resource, in DescriptorHandle descriptor)
        {
            DevicePointer->CreateUnorderedAccessView(resource.Resource.GetResourcePointer(), /* TODO: counter support? */ null, null, descriptor.CpuHandle);
        }
    }
}
