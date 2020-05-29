using System.Diagnostics.CodeAnalysis;
using TerraFX.Interop;
#pragma warning disable 1591

namespace Voltium.Core.d3dx12
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class CD3DX12_RASTERIZER_DESC
    {
        public static D3D12_RASTERIZER_DESC Create(in D3D12_RASTERIZER_DESC o)
        {
            return new D3D12_RASTERIZER_DESC
            {
                FillMode = o.FillMode,
                CullMode = o.CullMode,
                FrontCounterClockwise = o.FrontCounterClockwise,
                DepthBias = o.DepthBias,
                DepthBiasClamp = o.DepthBiasClamp,
                SlopeScaledDepthBias = o.SlopeScaledDepthBias,
                DepthClipEnable = o.DepthClipEnable,
                MultisampleEnable = o.MultisampleEnable,
                AntialiasedLineEnable = o.AntialiasedLineEnable,
                ForcedSampleCount = o.ForcedSampleCount,
                ConservativeRaster = o.ConservativeRaster
            };
        }

        public static D3D12_RASTERIZER_DESC Create(CD3DX12_DEFAULT _)
        {
            return new D3D12_RASTERIZER_DESC
            {
                FillMode = D3D12_FILL_MODE.D3D12_FILL_MODE_SOLID,
                CullMode = D3D12_CULL_MODE.D3D12_CULL_MODE_BACK,
                FrontCounterClockwise = TerraFX.Interop.Windows.FALSE,
                DepthBias = /* D3D12_DEFAULT_DEPTH_BIAS */ 0,
                DepthBiasClamp = /* D3D12_DEFAULT_DEPTH_BIAS_CLAMP */ 0.0F,
                SlopeScaledDepthBias = /* D3D12_DEFAULT_SLOPE_SCALED_DEPTH_BIAS */ 0.0F,
                DepthClipEnable = TerraFX.Interop.Windows.TRUE,
                MultisampleEnable = TerraFX.Interop.Windows.FALSE,
                AntialiasedLineEnable = TerraFX.Interop.Windows.FALSE,
                ForcedSampleCount = 0,
                ConservativeRaster = D3D12_CONSERVATIVE_RASTERIZATION_MODE.D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF
            };
        }

        public static D3D12_RASTERIZER_DESC Create(
            D3D12_FILL_MODE fillMode,
            D3D12_CULL_MODE cullMode,
            int frontCounterClockwise,
            int depthBias,
            float depthBiasClamp,
            float slopeScaledDepthBias,
            int depthClipEnable,
            int multisampleEnable,
            int antialiasedLineEnable,
            uint forcedSampleCount,
            D3D12_CONSERVATIVE_RASTERIZATION_MODE conservativeRaster)
        {
            return new D3D12_RASTERIZER_DESC
            {
                FillMode = fillMode,
                CullMode = cullMode,
                FrontCounterClockwise = frontCounterClockwise,
                DepthBias = depthBias,
                DepthBiasClamp = depthBiasClamp,
                SlopeScaledDepthBias = slopeScaledDepthBias,
                DepthClipEnable = depthClipEnable,
                MultisampleEnable = multisampleEnable,
                AntialiasedLineEnable = antialiasedLineEnable,
                ForcedSampleCount = forcedSampleCount,
                ConservativeRaster = conservativeRaster
            };
        }
    }
}
