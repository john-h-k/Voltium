using System.Diagnostics.CodeAnalysis;
using TerraFX.Interop;
#pragma warning disable 1591

namespace Voltium.Core.d3dx12
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public struct CD3DX12_BLEND_DESC
    {
        public static D3D12_BLEND_DESC Create(in D3D12_BLEND_DESC o)
        {
            return o;
        }

        /*
           BOOL BlendEnable;
           BOOL LogicOpEnable;
           D3D12_BLEND SrcBlend;
           D3D12_BLEND DestBlend;
           D3D12_BLEND_OP BlendOp;
           D3D12_BLEND SrcBlendAlpha;
           D3D12_BLEND DestBlendAlpha;
           D3D12_BLEND_OP BlendOpAlpha;
           D3D12_LOGIC_OP LogicOp;
           UINT8 RenderTargetWriteMask;
         */
        private static readonly D3D12_RENDER_TARGET_BLEND_DESC defaultRenderTargetBlendDesc = new D3D12_RENDER_TARGET_BLEND_DESC
        {
            BlendEnable = TerraFX.Interop.Windows.FALSE,
            LogicOpEnable = TerraFX.Interop.Windows.FALSE,
            SrcBlend = D3D12_BLEND.D3D12_BLEND_ONE,
            DestBlend = D3D12_BLEND.D3D12_BLEND_ZERO,
            BlendOp = D3D12_BLEND_OP.D3D12_BLEND_OP_ADD,
            SrcBlendAlpha = D3D12_BLEND.D3D12_BLEND_ONE,
            DestBlendAlpha = D3D12_BLEND.D3D12_BLEND_ZERO,
            BlendOpAlpha = D3D12_BLEND_OP.D3D12_BLEND_OP_ADD,
            LogicOp = D3D12_LOGIC_OP.D3D12_LOGIC_OP_NOOP,
            RenderTargetWriteMask = (byte)D3D12_COLOR_WRITE_ENABLE.D3D12_COLOR_WRITE_ENABLE_ALL
        };

        private static D3D12_RENDER_TARGET_BLEND_DESC def => defaultRenderTargetBlendDesc;

        public static D3D12_BLEND_DESC Create(CD3DX12_DEFAULT _)
        {

            var obj = new D3D12_BLEND_DESC
            {
                AlphaToCoverageEnable = TerraFX.Interop.Windows.FALSE,
                IndependentBlendEnable = TerraFX.Interop.Windows.FALSE,
                RenderTarget =
                {
                    e0 = def, e1 = def, e2 = def, e3 = def, e4 = def,
                    e5 = def, e6 = def, e7 = def
                }
            };

            return obj;
        }
    }
}
