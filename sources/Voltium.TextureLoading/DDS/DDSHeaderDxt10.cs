using TerraFX.Interop;

#pragma warning disable 649

namespace Voltium.TextureLoading.DDS
{
    internal struct DDSHeaderDxt10
    {
        public DXGI_FORMAT DxgiFormat;
        public D3D11_RESOURCE_DIMENSION ResourceDimension;
        public D3D11_RESOURCE_MISC_FLAG MiscFlag; // see D3D11_RESOURCE_MISC_FLAG
        public uint ArraySize;
        public uint MiscFlags2;
    }
}
