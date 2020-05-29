using System.Diagnostics.CodeAnalysis;
using TerraFX.Interop;
#pragma warning disable 1591

namespace Voltium.Core.d3dx12
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static unsafe class CD3DX12_TEXTURE_COPY_LOCATION
    {
        public static D3D12_TEXTURE_COPY_LOCATION Create(ID3D12Resource* pRes)
        {
            return new D3D12_TEXTURE_COPY_LOCATION
            {
                pResource = pRes
            };
        }

        public static D3D12_TEXTURE_COPY_LOCATION Create(ID3D12Resource* pRes,
            in D3D12_PLACED_SUBRESOURCE_FOOTPRINT Footprint)
        {
            return new D3D12_TEXTURE_COPY_LOCATION
            {
                pResource = pRes,
                Type = D3D12_TEXTURE_COPY_TYPE.D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT,
                Anonymous = { PlacedFootprint = Footprint }
            };
        }

        public static D3D12_TEXTURE_COPY_LOCATION Create(ID3D12Resource* pRes, uint Sub)
        {
            return new D3D12_TEXTURE_COPY_LOCATION
            {
                pResource = pRes,
                Type = D3D12_TEXTURE_COPY_TYPE.D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX,
                Anonymous = { SubresourceIndex = Sub }
            };
        }
    }
}
