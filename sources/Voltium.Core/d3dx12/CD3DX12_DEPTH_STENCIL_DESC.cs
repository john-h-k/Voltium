using System.Diagnostics.CodeAnalysis;
using TerraFX.Interop;
#pragma warning disable 1591

namespace Voltium.Core.d3dx12
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class CD3DX12_DEPTH_STENCIL_DESC
    {
        public static D3D12_DEPTH_STENCIL_DESC Create(in D3D12_DEPTH_STENCIL_DESC o)
        {
            return o;
        }

        private static readonly D3D12_DEPTH_STENCILOP_DESC defaultStencilOp
            = new D3D12_DEPTH_STENCILOP_DESC
            {
                StencilFailOp = D3D12_STENCIL_OP.D3D12_STENCIL_OP_KEEP,
                StencilDepthFailOp = D3D12_STENCIL_OP.D3D12_STENCIL_OP_KEEP,
                StencilPassOp = D3D12_STENCIL_OP.D3D12_STENCIL_OP_KEEP,
                StencilFunc = D3D12_COMPARISON_FUNC.D3D12_COMPARISON_FUNC_ALWAYS
            };

        public static D3D12_DEPTH_STENCIL_DESC Create(CD3DX12_DEFAULT _)
        {
            return new D3D12_DEPTH_STENCIL_DESC
            {
                DepthEnable = TerraFX.Interop.Windows.TRUE,
                DepthWriteMask = D3D12_DEPTH_WRITE_MASK.D3D12_DEPTH_WRITE_MASK_ALL,
                DepthFunc = D3D12_COMPARISON_FUNC.D3D12_COMPARISON_FUNC_LESS,
                StencilEnable = TerraFX.Interop.Windows.FALSE,
                StencilReadMask = (byte)D3D12.D3D12_DEFAULT_STENCIL_READ_MASK,
                StencilWriteMask = (byte)D3D12.D3D12_DEFAULT_STENCIL_WRITE_MASK,
                FrontFace = defaultStencilOp,
                BackFace = defaultStencilOp
            };
        }

        public static D3D12_DEPTH_STENCIL_DESC Create(
            int depthEnable,
            D3D12_DEPTH_WRITE_MASK depthWriteMask,
            D3D12_COMPARISON_FUNC depthFunc,
            int stencilEnable,
            byte stencilReadMask,
            byte stencilWriteMask,
            D3D12_STENCIL_OP frontStencilFailOp,
            D3D12_STENCIL_OP frontStencilDepthFailOp,
            D3D12_STENCIL_OP frontStencilPassOp,
            D3D12_COMPARISON_FUNC frontStencilFunc,
            D3D12_STENCIL_OP backStencilFailOp,
            D3D12_STENCIL_OP backStencilDepthFailOp,
            D3D12_STENCIL_OP backStencilPassOp,
            D3D12_COMPARISON_FUNC backStencilFunc)
        {
            return new D3D12_DEPTH_STENCIL_DESC
            {
                DepthEnable = depthEnable,
                DepthWriteMask = depthWriteMask,
                DepthFunc = depthFunc,
                StencilEnable = stencilEnable,
                StencilReadMask = stencilReadMask,
                StencilWriteMask = stencilWriteMask,
                FrontFace =
                {
                    StencilFailOp = frontStencilFailOp,
                    StencilDepthFailOp = frontStencilDepthFailOp,
                    StencilPassOp = frontStencilPassOp,
                    StencilFunc = frontStencilFunc
                },
                BackFace =
                {
                    StencilFailOp = backStencilFailOp,
                    StencilDepthFailOp = backStencilDepthFailOp,
                    StencilPassOp = backStencilPassOp,
                    StencilFunc = backStencilFunc
                }
            };
        }
    }
}
