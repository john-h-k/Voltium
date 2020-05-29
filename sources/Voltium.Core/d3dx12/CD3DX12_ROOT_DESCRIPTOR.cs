using System.Diagnostics.CodeAnalysis;
using TerraFX.Interop;
#pragma warning disable 1591

namespace Voltium.Core.d3dx12
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class CD3DX12_ROOT_DESCRIPTOR
    {
        public static D3D12_ROOT_DESCRIPTOR Create(in D3D12_ROOT_DESCRIPTOR o)
        {
            return new D3D12_ROOT_DESCRIPTOR
            {
                ShaderRegister = o.ShaderRegister,
                RegisterSpace = o.RegisterSpace
            };
        }

        public static D3D12_ROOT_DESCRIPTOR Create(
            uint shaderRegister,
            uint registerSpace = 0)
        {
            return new D3D12_ROOT_DESCRIPTOR
            {
                ShaderRegister = shaderRegister,
                RegisterSpace = registerSpace
            };
        }

        public static void Init(out D3D12_ROOT_DESCRIPTOR table, uint shaderRegister, uint registerSpace = 0)
        {
            table.ShaderRegister = shaderRegister;
            table.RegisterSpace = registerSpace;
        }
    }
}
