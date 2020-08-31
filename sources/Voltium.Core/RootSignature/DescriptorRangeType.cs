using static TerraFX.Interop.D3D12_DESCRIPTOR_RANGE_TYPE;

namespace Voltium.Core
{
    /// <summary>
    /// Indicates the type of the view represented by the descriptors in a
    /// <see cref="DescriptorRangeParameter"/>
    /// </summary>
    public enum DescriptorRangeType
    {
        /// <summary>
        /// A shader resource view, or SRV
        /// </summary>
        ShaderResourceView = D3D12_DESCRIPTOR_RANGE_TYPE_SRV,

        /// <summary>
        /// An unordered access view, or UAV
        /// </summary>
        UnorderedAccessView = D3D12_DESCRIPTOR_RANGE_TYPE_UAV,

        /// <summary>
        /// A constant buffer view, or CBV
        /// </summary>
        ConstantBufferView = D3D12_DESCRIPTOR_RANGE_TYPE_CBV,

        /// <summary>
        /// A sampler
        /// </summary>
        Sampler = D3D12_DESCRIPTOR_RANGE_TYPE_SAMPLER
    }
}
