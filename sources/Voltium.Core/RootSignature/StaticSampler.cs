using Voltium.Common;

namespace Voltium.Core
{
    /// <summary>
    /// A static sampler. Up to 2032 <see cref="StaticSampler"/>s can be bound to the pipeline,
    /// without the need for a descriptor heap, althought they must use a specific set of border colors
    /// </summary>
    [GenerateEquality]
    public readonly partial struct StaticSampler
    {
        /// <summary>
        /// A transparent black color. This is a legal border color for a static sampler
        /// </summary>
        public static Rgba128 TransparentBlack { get; } = new Rgba128(0, 0, 0, 0);

        /// <summary>
        /// An opaque black color. This is a legal border color for a static sampler
        /// </summary>
        public static Rgba128 OpaqueBlack { get; } = new Rgba128(0, 0, 0, 1);

        /// <summary>
        /// An opaque white color. This is a legal border color for a static sampler
        /// </summary>
        public static Rgba128 OpaqueWhite { get; } = new Rgba128(1, 1, 1, 1);

        /// <summary>
        /// The sampler description for the static sampler. Note that <see cref="Sampler.BorderColor"/> must be either
        /// <see cref="TransparentBlack"/>, <see cref="OpaqueBlack"/>, or <see cref="OpaqueWhite"/>
        /// </summary>
        public readonly Sampler Sampler;

        /// <summary>
        /// The shader register this sampler is bound to
        /// </summary>
        public readonly uint ShaderRegister;

        /// <summary>
        /// The register space this sampler is bound to
        /// </summary>
        public readonly uint RegisterSpace;

        /// <summary>
        /// Indicates which shaders this sampler is visible to
        /// </summary>
        public readonly ShaderVisibility Visibility;

        /// <summary>
        /// Creates a new instance of <see cref="StaticSampler"/>
        /// </summary>
        public StaticSampler(Sampler sampler, uint shaderRegister, uint registerSpace, ShaderVisibility visibility)
        {
            Sampler = sampler;
            ShaderRegister = shaderRegister;
            RegisterSpace = registerSpace;
            Visibility = visibility;
        }

        /// <summary>
        /// Creates a new instance of <see cref="StaticSampler"/>
        /// </summary>
        public StaticSampler(TextureAddressMode texUWV, SamplerFilterType samplerFilterType, uint shaderRegister, uint registerSpace, ShaderVisibility visibility, Rgba128 borderColor = default)
        {
            Sampler = new Sampler(texUWV, samplerFilterType, borderColor: borderColor);
            ShaderRegister = shaderRegister;
            RegisterSpace = registerSpace;
            Visibility = visibility;
        }
    }
}
