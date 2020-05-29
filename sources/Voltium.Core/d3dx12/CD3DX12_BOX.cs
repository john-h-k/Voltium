using System.Diagnostics.CodeAnalysis;
using TerraFX.Interop;
#pragma warning disable 1591

namespace Voltium.Core.d3dx12
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class CD3DX12_BOX
    {
        public static D3D12_BOX Create(
            int Left,
            int Right)
        {
            return new D3D12_BOX
            {
                left = (uint)Left,
                top = 0,
                front = 0,
                right = (uint)Right,
                bottom = 1,
                back = 1
            };
        }
        public static D3D12_BOX Create(
            int Left,
            int Top,
            int Right,
            int Bottom)
        {
            return new D3D12_BOX
            {
                left = (uint) Left,
                top = (uint) Top,
                front = 0,
                right = (uint) Right,
                bottom = (uint) Bottom,
                back = 1
            };
        }
        public static D3D12_BOX Create(
            int Left,
            int Top,
            int Front,
            int Right,
            int Bottom,
            int Back)
        {
            return new D3D12_BOX
            {
                left = (uint)Left,
                top = (uint)Top,
                front = (uint)Front,
                right = (uint)Right,
                bottom = (uint)Bottom,
                back = (uint)Back
            };
        }
    }
}
