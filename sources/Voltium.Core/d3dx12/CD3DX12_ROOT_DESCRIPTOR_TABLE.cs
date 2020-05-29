using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using TerraFX.Interop;
#pragma warning disable 1591

namespace Voltium.Core.d3dx12
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static unsafe class CD3DX12_ROOT_DESCRIPTOR_TABLE
    {
        public static D3D12_ROOT_DESCRIPTOR_TABLE Create(in D3D12_ROOT_DESCRIPTOR_TABLE o)
        {
            return o;
        }

        public static D3D12_ROOT_DESCRIPTOR_TABLE Create(
            uint numDescriptorRanges,
            [In] D3D12_DESCRIPTOR_RANGE* _pDescriptorRanges)
        {
            return new D3D12_ROOT_DESCRIPTOR_TABLE
            {
                NumDescriptorRanges = numDescriptorRanges,
                pDescriptorRanges = _pDescriptorRanges
            };
        }

        public static void Init(
            out D3D12_ROOT_DESCRIPTOR_TABLE rootDescriptorTable,
            uint numDescriptorRanges,
            [In] D3D12_DESCRIPTOR_RANGE* _pDescriptorRanges)
        {
            rootDescriptorTable.NumDescriptorRanges = numDescriptorRanges;
            rootDescriptorTable.pDescriptorRanges = _pDescriptorRanges;
        }

    }
}
