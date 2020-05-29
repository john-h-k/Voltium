using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using TerraFX.Interop;
#pragma warning disable 1591

namespace Voltium.Core.d3dx12
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static unsafe class CD3DX12_SHADER_BYTECODE
    {
        public static D3D12_SHADER_BYTECODE Create(in D3D12_SHADER_BYTECODE o)
        {
            var obj = new D3D12_SHADER_BYTECODE
            {
                pShaderBytecode = o.pShaderBytecode,
                BytecodeLength = o.BytecodeLength
            };

            return obj;
        }
        public static D3D12_SHADER_BYTECODE Create(
            [In] ID3DBlob* pShaderBlob)
        {
            var obj = new D3D12_SHADER_BYTECODE
            {
                pShaderBytecode = pShaderBlob->GetBufferPointer(),
                BytecodeLength = pShaderBlob->GetBufferSize()
            };

            return obj;
        }
        public static D3D12_SHADER_BYTECODE Create(
            void* _pShaderBytecode,
            UIntPtr bytecodeLength)
        {

            var obj = new D3D12_SHADER_BYTECODE
            {
                pShaderBytecode = _pShaderBytecode,
                BytecodeLength = bytecodeLength
            };
            return obj;
        }
    }
}
