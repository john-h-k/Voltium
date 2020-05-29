using System.Diagnostics.CodeAnalysis;
using TerraFX.Interop;
#pragma warning disable 1591

namespace Voltium.Core.d3dx12
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static unsafe class CD3DX12_RESOURCE_DESC
    {
        public static D3D12_RESOURCE_DESC Create(in D3D12_RESOURCE_DESC o)
        {
            return new D3D12_RESOURCE_DESC
            {
                Dimension = o.Dimension,
                Alignment = o.Alignment,
                Width = o.Width,
                Height = o.Height,
                DepthOrArraySize = o.DepthOrArraySize,
                MipLevels = o.MipLevels,
                Format = o.Format,
                SampleDesc = o.SampleDesc,
                Layout = o.Layout,
                Flags = o.Flags
            };
        }
        public static D3D12_RESOURCE_DESC Create(
            D3D12_RESOURCE_DIMENSION dimension,
            ulong alignment,
            ulong width,
            uint height,
            ushort depthOrArraySize,
            ushort mipLevels,
            DXGI_FORMAT format,
            uint sampleCount,
            uint sampleQuality,
            D3D12_TEXTURE_LAYOUT layout,
            D3D12_RESOURCE_FLAGS flags)
        {
            return new D3D12_RESOURCE_DESC
            {
                Dimension = dimension,
                Alignment = alignment,
                Width = width,
                Height = height,
                DepthOrArraySize = depthOrArraySize,
                MipLevels = mipLevels,
                Format = format,
                Layout = layout,
                Flags = flags,
                SampleDesc = { Count = sampleCount, Quality = sampleQuality }
            };
        }
        public static D3D12_RESOURCE_DESC Buffer(
            in D3D12_RESOURCE_ALLOCATION_INFO resAllocInfo,
            D3D12_RESOURCE_FLAGS flags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_NONE)
        {
            return Create(D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER, resAllocInfo.Alignment, resAllocInfo.SizeInBytes,
                1, 1, 1, DXGI_FORMAT.DXGI_FORMAT_UNKNOWN, 1, 0, D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_ROW_MAJOR, flags);
        }
        public static D3D12_RESOURCE_DESC Buffer(
            ulong width,
            D3D12_RESOURCE_FLAGS flags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_NONE,
            ulong alignment = 0)
        {
            return Create(D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER, alignment, width, 1, 1, 1,
               DXGI_FORMAT.DXGI_FORMAT_UNKNOWN, 1, 0, D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_ROW_MAJOR, flags);
        }
        public static D3D12_RESOURCE_DESC Tex1D(
            DXGI_FORMAT format,
            ulong width,
            ushort arraySize = 1,
            ushort mipLevels = 0,
            D3D12_RESOURCE_FLAGS flags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_NONE,
            D3D12_TEXTURE_LAYOUT layout = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_UNKNOWN,
            ulong alignment = 0)
        {
            return Create(D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE1D, alignment, width, 1, arraySize,
                mipLevels, format, 1, 0, layout, flags);
        }
        public static D3D12_RESOURCE_DESC Tex2D(
            DXGI_FORMAT format,
            ulong width,
            uint height,
            ushort arraySize = 1,
            ushort mipLevels = 0,
            uint sampleCount = 1,
            uint sampleQuality = 0,
            D3D12_RESOURCE_FLAGS flags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_NONE,
            D3D12_TEXTURE_LAYOUT layout = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_UNKNOWN,
            ulong alignment = 0)
        {
            return Create(D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE2D, alignment, width, height, arraySize,
                mipLevels, format, sampleCount, sampleQuality, layout, flags);
        }
        public static D3D12_RESOURCE_DESC Tex3D(
            DXGI_FORMAT format,
            ulong width,
            uint height,
            ushort depth,
            ushort mipLevels = 0,
            D3D12_RESOURCE_FLAGS flags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_NONE,
            D3D12_TEXTURE_LAYOUT layout = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_UNKNOWN,
            ulong alignment = 0)
        {
            return Create(D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE3D, alignment, width, height, depth,
                mipLevels, format, 1, 0, layout, flags);
        }
        public static ushort Depth(this D3D12_RESOURCE_DESC obj) => (ushort)(obj.Dimension == D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE3D ? obj.DepthOrArraySize : 1);

        public static ushort ArraySize(this D3D12_RESOURCE_DESC obj) => (ushort)(obj.Dimension != D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE3D ? obj.DepthOrArraySize : 1);

        public static byte PlaneCount(this D3D12_RESOURCE_DESC obj, ID3D12Device* pDevice) => D3D12.D3D12GetFormatPlaneCount(pDevice, obj.Format);

        public static uint Subresources(this D3D12_RESOURCE_DESC obj, ID3D12Device* pDevice) => (uint)(obj.MipLevels * obj.ArraySize() * obj.PlaneCount(pDevice));

        public static uint CalcSubresource(this D3D12_RESOURCE_DESC obj, uint MipSlice, uint ArraySlice, uint PlaneSlice) => D3D12.D3D12CalcSubresource(MipSlice, ArraySlice, PlaneSlice, obj.MipLevels, obj.ArraySize());
    }
}
