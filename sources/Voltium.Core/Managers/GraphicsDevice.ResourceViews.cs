using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.GpuResources;
using static TerraFX.Interop.Windows;
using static TerraFX.Interop.D3D12_DESCRIPTOR_HEAP_TYPE;
using Voltium.Core.Memory.GpuResources;
using Buffer = Voltium.Core.Memory.GpuResources.Buffer;
using System.Runtime.CompilerServices;
namespace Voltium.Core.Managers
{
    public unsafe partial class GraphicsDevice
    {
        private DescriptorHeap _samplers;
        private DescriptorHeap _rtvs;
        private DescriptorHeap _dsvs;
        private DescriptorHeap _cbvSrvUav;

        private void InitializeDescriptorSizes()
        {
            ConstantBufferOrShaderResourceOrUnorderedAccessViewDescriptorSize = GetIncrementSize(DescriptorHeapType.ConstantBufferShaderResourceOrUnorderedAccessView);
            RenderTargetViewDescriptorSize = GetIncrementSize(DescriptorHeapType.RenderTargetView);
            DepthStencilViewDescriptorSize = GetIncrementSize(DescriptorHeapType.DepthStencilView);
            SamplerDescriptorSize = GetIncrementSize(DescriptorHeapType.Sampler);

            int GetIncrementSize(DescriptorHeapType type) => (int)DevicePointer->GetDescriptorHandleIncrementSize((D3D12_DESCRIPTOR_HEAP_TYPE)type);
        }

        private const int SamplerCount = 512;
        private const int ResourceCount = 512;
        private void CreateDescriptorHeaps()
        {
            _rtvs = DescriptorHeap.CreateRenderTargetViewHeap(this, _config.SwapChainBufferCount * 100);
            _dsvs = DescriptorHeap.CreateDepthStencilViewHeap(this, 100);
            _samplers = DescriptorHeap.CreateSamplerHeap(this, ResourceCount);
            _cbvSrvUav = DescriptorHeap.CreateConstantBufferShaderResourceUnorderedAccessViewHeap(this, SamplerCount);
        }

        /// <summary>
        /// Creates a shader resource view to a <see cref="Texture"/>
        /// </summary>
        /// <param name="resource">The <see cref="Texture"/> resource to create the view for</param>
        public DescriptorHandle CreateShaderResourceView(Texture resource)
        {
            var handle = _cbvSrvUav.GetNextHandle();

            DevicePointer->CreateShaderResourceView(resource.Resource.UnderlyingResource, null, handle.CpuHandle);

            return handle;
        }

        /// <summary>
        /// Creates a shader resource view to a <see cref="Texture"/>
        /// </summary>
        /// <param name="resource">The <see cref="Texture"/> resource to create the view for</param>
        /// <param name="desc">The <see cref="TextureShaderResourceViewDesc"/> describing the metadata used to create the view</param>
        public DescriptorHandle CreateShaderResourceView(Texture resource, in TextureShaderResourceViewDesc desc)
        {
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

            var handle = _cbvSrvUav.GetNextHandle();

            DevicePointer->CreateShaderResourceView(resource.Resource.UnderlyingResource, &srvDesc, handle.CpuHandle);

            return handle;
        }

        /// <summary>
        /// Creates a shader resource view to a <see cref="Buffer"/>
        /// </summary>
        /// <param name="resource">The <see cref="Buffer"/> resource to create the view for</param>
        /// <param name="desc">The <see cref="BufferShaderResourceViewDesc"/> describing the metadata used to create the view</param>
        public DescriptorHandle CreateShaderResourceView(Buffer resource, in BufferShaderResourceViewDesc desc)
        {
            Unsafe.SkipInit(out D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc);
            srvDesc.Format = (DXGI_FORMAT)desc.Format;
            srvDesc.ViewDimension = D3D12_SRV_DIMENSION.D3D12_SRV_DIMENSION_BUFFER;
            srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING; // TODO
            srvDesc.Anonymous.Buffer.FirstElement = desc.Offset;
            srvDesc.Anonymous.Buffer.Flags = desc.Raw ? D3D12_BUFFER_SRV_FLAGS.D3D12_BUFFER_SRV_FLAG_RAW : D3D12_BUFFER_SRV_FLAGS.D3D12_BUFFER_SRV_FLAG_NONE;
            srvDesc.Anonymous.Buffer.NumElements = desc.ElementCount;
            srvDesc.Anonymous.Buffer.StructureByteStride = desc.ElementStride;

            var handle = _cbvSrvUav.GetNextHandle();

            DevicePointer->CreateShaderResourceView(resource.Resource.UnderlyingResource, &srvDesc, _cbvSrvUav.GetNextHandle().CpuHandle);

            return handle;
        }

        /// <summary>
        /// Creates a shader resource view to a <see cref="Buffer"/>
        /// </summary>
        /// <param name="resource">The <see cref="Buffer"/> resource to create the view for</param>
        public DescriptorHandle CreateShaderResourceView(Buffer resource)
        {
            var handle = _cbvSrvUav.GetNextHandle();

            DevicePointer->CreateShaderResourceView(resource.Resource.UnderlyingResource, null, handle.CpuHandle);

            return handle;
        }

        /// <summary>
        /// Creates a render target view to a <see cref="Texture"/>
        /// </summary>
        /// <param name="resource">The <see cref="Texture"/> resource to create the view for</param>
        /// <param name="desc">The <see cref="TextureShaderResourceViewDesc"/> describing the metadata used to create the view</param>
        public DescriptorHandle CreateRenderTargetView(Texture resource, in TextureRenderTargetViewDesc desc)
        {
            D3D12_RENDER_TARGET_VIEW_DESC rtvDesc;

            if (desc.IsMultiSampled)
            {
                switch (resource.Dimension)
                {
                    case TextureDimension.Tex1D:
                        ThrowHelper.ThrowArgumentException("Cannot multisample 1D render target view");
                        break;

                    case TextureDimension.Tex2D:
                        rtvDesc.ViewDimension = D3D12_RTV_DIMENSION.D3D12_RTV_DIMENSION_TEXTURE2DMS;
                        break;

                    case TextureDimension.Tex3D:
                        ThrowHelper.ThrowArgumentException("Cannot multisample 3D render target view");
                        break;
                }
            }
            else
            {
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
            D3D12_DEPTH_STENCIL_VIEW_DESC dsvDesc;

            if (desc.IsMultiSampled)
            {
                switch (resource.Dimension)
                {
                    case TextureDimension.Tex1D:
                        ThrowHelper.ThrowArgumentException("Cannot multisample 1D depth stencil view");
                        break;
                    case TextureDimension.Tex2D:
                        dsvDesc.ViewDimension = D3D12_DSV_DIMENSION.D3D12_DSV_DIMENSION_TEXTURE2DMS;
                        break;
                    case TextureDimension.Tex3D:
                        ThrowHelper.ThrowArgumentException("Cannot have 3D depth stencil view");
                        break;
                }
            }
            else
            {
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
