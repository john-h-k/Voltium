using static TerraFX.Interop.DirectX.D3D12_ROOT_PARAMETER_TYPE;

namespace Voltium.Core
{
    /// <summary>
    /// The type of a <see cref="RootParameter"/>
    /// </summary>
    public enum RootParameterType
    {
        /// <summary>
        /// A table that represents a set of descriptors
        /// </summary>
        DescriptorTable = D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE,

        /// <summary>
        /// A number of 32 bit constant values
        /// </summary>
        DwordConstants = D3D12_ROOT_PARAMETER_TYPE_32BIT_CONSTANTS,

        /// <summary>
        /// A directly bound constant buffer view
        /// </summary>
        ConstantBufferView = D3D12_ROOT_PARAMETER_TYPE_CBV,

        /// <summary>
        /// A directly bound shader resource view
        /// </summary>
        ShaderResourceView = D3D12_ROOT_PARAMETER_TYPE_SRV,

        /// <summary>
        /// A directly bound unordered access view
        /// </summary>
        UnorderedAccessView = D3D12_ROOT_PARAMETER_TYPE_UAV
    }
}
