using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.GpuResources;
using Voltium.Core.Memory.GpuResources;

namespace Voltium.Core.Managers
{
    public unsafe partial class GraphicsDevice
    {
        private DescriptorHeap _samplers;
        private DescriptorHeap _rtvs;
        private DescriptorHeap _dsvs;

        private const int SamplerCount = 512;
        private const int RtvOrDsvCount = 64;

        private protected override void CreateDescriptorHeaps()
        {
            base.CreateDescriptorHeaps();
            _rtvs = DescriptorHeap.Create(this, DescriptorHeapType.RenderTargetView, RtvOrDsvCount);
            _dsvs = DescriptorHeap.Create(this, DescriptorHeapType.DepthStencilView, RtvOrDsvCount);
            _samplers = DescriptorHeap.Create(this, DescriptorHeapType.Sampler, SamplerCount);
        }


        /// <summary>
        /// Creates a render target view to a <see cref="Texture"/>
        /// </summary>
        /// <param name="resource">The <see cref="Texture"/> resource to create the view for</param>
        /// <param name="desc">The <see cref="TextureShaderResourceViewDesc"/> describing the metadata used to create the view</param>
        public DescriptorHandle CreateRenderTargetView(Texture resource, in TextureRenderTargetViewDesc desc)
        {
            if (desc.IsMultiSampled)
            {
                return CreateRenderTargetView(resource);
            }

            D3D12_RENDER_TARGET_VIEW_DESC rtvDesc;

            switch (resource.Dimension)
            {
                case TextureDimension.Tex1D:
                    rtvDesc.Anonymous.Texture1D.MipSlice = desc.MipIndex;
                    rtvDesc.ViewDimension = D3D12_RTV_DIMENSION.D3D12_RTV_DIMENSION_TEXTURE1D;
                    break;

                case TextureDimension.Tex2D:
                    rtvDesc.Anonymous.Texture2D.MipSlice = desc.MipIndex;
                    rtvDesc.Anonymous.Texture2D.PlaneSlice = desc.PlaneSlice;
                    rtvDesc.ViewDimension = D3D12_RTV_DIMENSION.D3D12_RTV_DIMENSION_TEXTURE2D;
                    break;

                case TextureDimension.Tex3D:
                    rtvDesc.Anonymous.Texture3D.MipSlice = desc.MipIndex;
                    rtvDesc.ViewDimension = D3D12_RTV_DIMENSION.D3D12_RTV_DIMENSION_TEXTURE3D;
                    break;
            }

            rtvDesc.Format = (DXGI_FORMAT)desc.Format;

            var handle = _rtvs.GetNextHandle();

            DevicePointer->CreateRenderTargetView(resource.Resource.UnderlyingResource, &rtvDesc, handle.CpuHandle);

            return handle;
        }

        /// <summary>
        /// Creates a render target view to a <see cref="Texture"/>
        /// </summary>
        /// <param name="resource">The <see cref="Texture"/> resource to create the view for</param>
        public DescriptorHandle CreateRenderTargetView(Texture resource)
        {
            var handle = _rtvs.GetNextHandle();

            DevicePointer->CreateRenderTargetView(resource.Resource.UnderlyingResource, null, handle.CpuHandle);

            return handle;
        }

        /// <summary>
        /// Creates a depth stencil view to a <see cref="Texture"/>
        /// </summary>
        /// <param name="resource">The <see cref="Texture"/> resource to create the view for</param>
        /// <param name="desc">The <see cref="TextureShaderResourceViewDesc"/> describing the metadata used to create the view</param>
        public DescriptorHandle CreateDepthStencilView(Texture resource, in TextureDepthStencilViewDesc desc)
        {
            if (desc.IsMultiSampled)
            {
                return CreateDepthStencilView(resource);
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

            var handle = _dsvs.GetNextHandle();

            DevicePointer->CreateDepthStencilView(resource.GetResourcePointer(), &dsvDesc, handle.CpuHandle);

            return handle;
        }

        /// <summary>
        /// Creates a depth stencil view to a <see cref="Texture"/>
        /// </summary>
        /// <param name="resource">The <see cref="Texture"/> resource to create the view for</param>
        public DescriptorHandle CreateDepthStencilView(Texture resource)
        {
            var handle = _dsvs.GetNextHandle();

            DevicePointer->CreateDepthStencilView(resource.GetResourcePointer(), null, handle.CpuHandle);

            return handle;
        }
    }
}
