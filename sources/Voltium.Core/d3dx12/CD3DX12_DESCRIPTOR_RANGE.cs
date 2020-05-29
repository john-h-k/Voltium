using System.Diagnostics.CodeAnalysis;
using TerraFX.Interop;
#pragma warning disable 1591

namespace Voltium.Core.d3dx12
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class CD3DX12_DESCRIPTOR_RANGE
    {
        public static D3D12_DESCRIPTOR_RANGE Create(in D3D12_DESCRIPTOR_RANGE o)
        {
            return new D3D12_DESCRIPTOR_RANGE
            {
                RangeType = o.RangeType,
                NumDescriptors = o.NumDescriptors,
                BaseShaderRegister = o.BaseShaderRegister,
                RegisterSpace = o.RegisterSpace,
                OffsetInDescriptorsFromTableStart = o.OffsetInDescriptorsFromTableStart
            };
        }

        public static D3D12_DESCRIPTOR_RANGE Create(
            D3D12_DESCRIPTOR_RANGE_TYPE rangeType,
            uint numDescriptors,
            uint baseShaderRegister,
            uint registerSpace = 0,
            uint offsetInDescriptorsFromTableStart =
             /* D3D12_DESCRIPTOR_RANGE_OFFSET_APPEND */ uint.MaxValue)
        {
            return new D3D12_DESCRIPTOR_RANGE
            {
                RangeType = rangeType,
                NumDescriptors = numDescriptors,
                BaseShaderRegister = baseShaderRegister,
                RegisterSpace = registerSpace,
                OffsetInDescriptorsFromTableStart = offsetInDescriptorsFromTableStart
            };
        }
    }
}
