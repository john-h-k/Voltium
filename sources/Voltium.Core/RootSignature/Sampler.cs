using System.Diagnostics;
using System.Runtime.CompilerServices;
using TerraFX.Interop;

namespace Voltium.Core
{
    /// <summary>
    /// A sampler, used for sampling textures
    /// </summary>
    public unsafe struct Sampler
    {
        /// <summary>
        /// Defines the <see cref="TextureAddressMode"/> to use for sampling points outside
        /// of the texture coordinates in the U address dimension
        /// </summary>
        public TextureAddressMode TexU => (TextureAddressMode)_desc.AddressU;

        /// <summary>
        /// Defines the <see cref="TextureAddressMode"/> to use for sampling points outside
        /// of the texture coordinates in the W address dimension
        /// </summary>
        public TextureAddressMode TexW => (TextureAddressMode)_desc.AddressW;

        /// <summary>
        /// Defines the <see cref="TextureAddressMode"/> to use for sampling points outside
        /// of the texture coordinates in the V address dimension
        /// </summary>
        public TextureAddressMode TexV => (TextureAddressMode)_desc.AddressV;

        /// <summary>
        /// The <see cref="SamplerFilterType"/> used for filtering mipmaps, minification, and magnification
        /// </summary>
        public SamplerFilterType Filter => (SamplerFilterType)_desc.Filter;

        /// <summary>
        /// Defines the bias used for mips - that is, if the calculated mip is level 3, and the offset is n, the used mip is 3 + n
        /// </summary>
        public float MipLODBias => _desc.MipLODBias;

        /// <summary>
        /// The maximum value used for anistropy
        /// </summary>
        public uint MaxAnisotropy => _desc.MaxAnisotropy;

        /// <summary>
        /// The comparison operator used to compare sampled data when the <see cref="SamplerFilterType.UseComparisonOperator"/> flag is set
        /// </summary>
        public SampleComparisonFunc ComparisonFunc => (SampleComparisonFunc)_desc.ComparisonFunc;

        /// <summary>
        /// The color
        /// </summary>
        public Rgba128 BorderColor => Unsafe.As<float, Rgba128>(ref _desc.BorderColor[0]);

        /// <summary>
        /// The minimum (most detailed) mipmap level to use
        /// </summary>
        public float MinLOD => _desc.MinLOD;

        /// <summary>
        /// The maximum (least detailed) mipmap level to use
        /// </summary>
        public float MaxLOD => _desc.MaxLOD;

        private D3D12_SAMPLER_DESC _desc;
        internal D3D12_SAMPLER_DESC GetDesc() => _desc;

        /// <summary>
        /// Creates a new <see cref="Sampler"/>
        /// </summary>
        public unsafe Sampler(
            TextureAddressMode texUWV,
            SamplerFilterType filter,
            float mipLODBias = 0,
            uint maxAnisotropy = 16,
            SampleComparisonFunc comparisonFunc = SampleComparisonFunc.LessThan,
            Rgba128 borderColor = default,
            float minLOD = 0,
            float maxLOD = float.MaxValue
        ) : this(texUWV, texUWV, texUWV, filter, mipLODBias, maxAnisotropy, comparisonFunc, borderColor, minLOD, maxLOD)
        {

        }

        /// <summary>
        /// Creates a new <see cref="Sampler"/>
        /// </summary>
        public unsafe Sampler(
            TextureAddressMode texU,
            TextureAddressMode texW,
            TextureAddressMode texV,
            SamplerFilterType filter,
            float mipLODBias = 0,
            uint maxAnisotropy = 16,
            SampleComparisonFunc comparisonFunc = SampleComparisonFunc.LessThan,
            Rgba128 borderColor = default,
            float minLOD = 0,
            float maxLOD = float.MaxValue
        )
        {
            Debug.Assert(maxAnisotropy <= 16);

            _desc = new D3D12_SAMPLER_DESC
            {
                AddressU = (D3D12_TEXTURE_ADDRESS_MODE)texU,
                AddressW = (D3D12_TEXTURE_ADDRESS_MODE)texW,
                AddressV = (D3D12_TEXTURE_ADDRESS_MODE)texV,
                Filter = (D3D12_FILTER)filter,
                MipLODBias = mipLODBias,
                MaxAnisotropy = maxAnisotropy,
                ComparisonFunc = (D3D12_COMPARISON_FUNC)comparisonFunc,
                MinLOD = minLOD,
                MaxLOD = maxLOD,
            };
            Unsafe.As<float, Rgba128>(ref _desc.BorderColor[0]) = borderColor;
        }
    }
}
