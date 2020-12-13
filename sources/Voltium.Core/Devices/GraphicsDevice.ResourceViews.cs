using System.Collections.Generic;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Memory;
using Voltium.Core.Views;
using static TerraFX.Interop.Windows;

namespace Voltium.Core.Devices
{
    public unsafe partial class GraphicsDevice
    {
        private DescriptorHeap _samplers = null!;

        private const int SamplerCount = 2032;

        private protected override void CreateDescriptorHeaps()
        {
            base.CreateDescriptorHeaps();
            _samplers = CreateDescriptorHeap(DescriptorHeapType.Sampler, SamplerCount, true);
        }

        /// <summary>
        /// Allocates a range of descriptor handles in the resource descriptor heap, used for CBVs, SRVs, and UAVs
        /// </summary>
        /// <param name="descriptorCount"></param>
        /// <returns></returns>
        public DescriptorAllocation AllocateSamplerDescriptors(int descriptorCount)
        {
            var handles = _samplers.AllocateHandles(descriptorCount);
            return handles;
        }

        /// <summary>
        /// Creates a render target view to a <see cref="Buffer"/>
        /// </summary>
        /// <param name="resource">The <see cref="Buffer"/> resource to create the view for</param>
        /// <param name="descriptor">The <see cref="DescriptorHandle"/> to create the view with, or <see langword="null"/> to use the device default heap</param>
        public void CreateRenderTargetView(in Buffer resource, DescriptorHandle descriptor)
        {
            DevicePointer->CreateRenderTargetView(resource.Resource.GetResourcePointer(), null, descriptor.CpuHandle);
        }

        /// <summary>
        /// Creates a render target view to a <see cref="Buffer"/>
        /// </summary>
        /// <param name="resource">The <see cref="Buffer"/> resource to create the view for</param>
        /// <param name="desc">The <see cref="BufferRenderTargetViewDesc"/> describing the metadata used to create the view</param>
        /// <param name="descriptor">The <see cref="DescriptorHandle"/> to create the view with, or <see langword="null"/> to use the device default heap</param>
        public void CreateRenderTargetView(in Buffer resource, in BufferRenderTargetViewDesc desc, DescriptorHandle descriptor)
        {
            D3D12_RENDER_TARGET_VIEW_DESC rtvDesc;
            rtvDesc.Format = (DXGI_FORMAT)desc.Format;
            rtvDesc.ViewDimension = D3D12_RTV_DIMENSION.D3D12_RTV_DIMENSION_BUFFER;
            rtvDesc.Anonymous.Buffer.FirstElement = desc.FirstElement;
            rtvDesc.Anonymous.Buffer.NumElements = desc.NumElements;

            DevicePointer->CreateRenderTargetView(resource.Resource.GetResourcePointer(), &rtvDesc, descriptor.CpuHandle);
        }

        /// <summary>
        /// Creates a render target view to a <see cref="Texture"/>
        /// </summary>
        /// <param name="resource">The <see cref="Texture"/> resource to create the view for</param>
        /// <param name="descriptor">The <see cref="DescriptorHandle"/> to create the view with, or <see langword="null"/> to use the device default heap</param>
        public void CreateRenderTargetView(in Texture resource, DescriptorHandle descriptor)
        {
            DevicePointer->CreateRenderTargetView(resource.Resource.GetResourcePointer(), null, descriptor.CpuHandle);
        }

        /// <summary>
        /// Creates a render target view to a <see cref="Texture"/>
        /// </summary>
        /// <param name="resource">The <see cref="Texture"/> resource to create the view for</param>
        /// <param name="desc">The <see cref="TextureRenderTargetViewDesc"/> describing the metadata used to create the view</param>
        /// <param name="descriptor">The <see cref="DescriptorHandle"/> to create the view with, or <see langword="null"/> to use the device default heap</param>
        public void CreateRenderTargetView(in Texture resource, in TextureRenderTargetViewDesc desc, DescriptorHandle descriptor)
        {
            if (desc.IsMultiSampled)
            {
                CreateRenderTargetView(resource, descriptor);
            }

            D3D12_RENDER_TARGET_VIEW_DESC rtvDesc;
            _ = &rtvDesc;

            switch (resource.Dimension)
            {
                case TextureDimension.Tex1D:
                    rtvDesc.Texture1D.MipSlice = desc.MipIndex;
                    rtvDesc.ViewDimension = D3D12_RTV_DIMENSION.D3D12_RTV_DIMENSION_TEXTURE1D;
                    break;

                case TextureDimension.Tex2D:
                    rtvDesc.Texture2D.MipSlice = desc.MipIndex;
                    rtvDesc.Texture2D.PlaneSlice = desc.PlaneSlice;
                    rtvDesc.ViewDimension = D3D12_RTV_DIMENSION.D3D12_RTV_DIMENSION_TEXTURE2D;
                    break;

                case TextureDimension.Tex3D:
                    rtvDesc.Texture3D.MipSlice = desc.MipIndex;
                    rtvDesc.ViewDimension = D3D12_RTV_DIMENSION.D3D12_RTV_DIMENSION_TEXTURE3D;
                    break;
            }

            rtvDesc.Format = (DXGI_FORMAT)desc.Format;

            DevicePointer->CreateRenderTargetView(resource.GetResourcePointer(), &rtvDesc, descriptor.CpuHandle);
        }

        /// <summary>
        /// Creates a depth stencil view to a <see cref="Texture"/>
        /// </summary>
        /// <param name="resource">The <see cref="Texture"/> resource to create the view for</param>
        /// <param name="descriptor"></param>
        public void CreateDepthStencilView(in Texture resource, DescriptorHandle descriptor)
        {
            DevicePointer->CreateDepthStencilView(resource.GetResourcePointer(), null, descriptor.CpuHandle);
        }

        /// <summary>
        /// Creates a depth stencil view to a <see cref="Texture"/>
        /// </summary>
        /// <param name="resource">The <see cref="Texture"/> resource to create the view for</param>
        /// <param name="desc">The <see cref="TextureShaderResourceViewDesc"/> describing the metadata used to create the view</param>
        /// <param name="descriptor"></param>
        public void CreateDepthStencilView(in Texture resource, in TextureDepthStencilViewDesc desc, DescriptorHandle descriptor)
        {
            if (desc.IsMultiSampled)
            {
                CreateDepthStencilView(resource, descriptor);
            }

            D3D12_DEPTH_STENCIL_VIEW_DESC dsvDesc;

            switch (resource.Dimension)
            {
                case TextureDimension.Tex1D:
                    dsvDesc.Anonymous.Texture1D.MipSlice = desc.MipIndex;
                    dsvDesc.ViewDimension = D3D12_DSV_DIMENSION.D3D12_DSV_DIMENSION_TEXTURE1D;
                    break;
                case TextureDimension.Tex2D:
                    dsvDesc.Anonymous.Texture2D.MipSlice = desc.MipIndex;
                    dsvDesc.ViewDimension = D3D12_DSV_DIMENSION.D3D12_DSV_DIMENSION_TEXTURE2D;
                    break;
                case TextureDimension.Tex3D:
                    ThrowHelper.ThrowArgumentException("Cannot have 3D depth stencil view");
                    break;
            }

            dsvDesc.Format = (DXGI_FORMAT)desc.Format;

            DevicePointer->CreateDepthStencilView(resource.GetResourcePointer(), &dsvDesc, descriptor.CpuHandle);


        }

        /// <summary>
        /// Creates a new <see cref="Sampler"/>
        /// </summary>
        /// <param name="sampler"></param>
        /// <param name="descriptor"></param>
        /// <returns></returns>
        public void CreateSampler(in Sampler sampler, DescriptorHandle descriptor)
        {
            fixed (D3D12_SAMPLER_DESC* pDesc = &sampler.Desc)
            {
                DevicePointer->CreateSampler(pDesc, descriptor.CpuHandle);
            }
        }
    }
}
