using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using TerraFX.Interop;
#pragma warning disable 1591

namespace Voltium.Core.d3dx12
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static unsafe class CD3DX12_ROOT_SIGNATURE_DESC
    {
        public static D3D12_ROOT_SIGNATURE_DESC Create(CD3DX12_DEFAULT _)
        {
            return default;
        }

        public static D3D12_ROOT_SIGNATURE_DESC Create(in D3D12_ROOT_SIGNATURE_DESC o)
        {
            return o;
        }

        public static D3D12_ROOT_SIGNATURE_DESC Create(
            uint numParameters,
            [In] D3D12_ROOT_PARAMETER* _pParameters,
            uint numStaticSamplers = 0,
            [In] D3D12_STATIC_SAMPLER_DESC* _pStaticSamplers = null,
            D3D12_ROOT_SIGNATURE_FLAGS flags = D3D12_ROOT_SIGNATURE_FLAGS.D3D12_ROOT_SIGNATURE_FLAG_NONE)
        {
            return new D3D12_ROOT_SIGNATURE_DESC
            {
                NumParameters = numParameters,
                pParameters = _pParameters,
                NumStaticSamplers = numStaticSamplers,
                pStaticSamplers = _pStaticSamplers,
                Flags = flags
            };
        }

        public static void Init(
            out D3D12_ROOT_SIGNATURE_DESC desc,
            uint numParameters,
            [In] D3D12_ROOT_PARAMETER* _pParameters,
            uint numStaticSamplers = 0,
            [In] D3D12_STATIC_SAMPLER_DESC* _pStaticSamplers = null,
            D3D12_ROOT_SIGNATURE_FLAGS flags = D3D12_ROOT_SIGNATURE_FLAGS.D3D12_ROOT_SIGNATURE_FLAG_NONE)
        {
            desc.NumParameters = numParameters;
            desc.pParameters = _pParameters;
            desc.NumStaticSamplers = numStaticSamplers;
            desc.pStaticSamplers = _pStaticSamplers;
            desc.Flags = flags;
        }
    }
}
