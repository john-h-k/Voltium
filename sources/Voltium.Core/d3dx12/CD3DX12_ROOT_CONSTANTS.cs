using System.Diagnostics.CodeAnalysis;
using TerraFX.Interop;
#pragma warning disable 1591

namespace Voltium.Core.d3dx12
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class CD3DX12_ROOT_CONSTANTS
    {
        public static D3D12_ROOT_CONSTANTS Create(in D3D12_ROOT_CONSTANTS o)
        {
            return new D3D12_ROOT_CONSTANTS
            {
                ShaderRegister = o.ShaderRegister,
                RegisterSpace = o.RegisterSpace,
                Num32BitValues = o.Num32BitValues
            };
        }

        public static D3D12_ROOT_CONSTANTS Create(
            uint num32BitValues,
            uint shaderRegister,
            uint registerSpace = 0)
        {
            return new D3D12_ROOT_CONSTANTS
            {
                Num32BitValues = num32BitValues,
                ShaderRegister = shaderRegister,
                RegisterSpace = registerSpace

            };
        }

        public static void Init(
            out D3D12_ROOT_CONSTANTS rootConstants,
            uint num32BitValues,
            uint shaderRegister,
            uint registerSpace = 0)
        {
            rootConstants.Num32BitValues = num32BitValues;
            rootConstants.ShaderRegister = shaderRegister;
            rootConstants.RegisterSpace = registerSpace;
        }
    }
}
