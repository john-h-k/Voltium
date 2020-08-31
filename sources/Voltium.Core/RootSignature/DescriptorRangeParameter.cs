namespace Voltium.Core
{
    /// <summary>
    /// Represents a range of descriptors relative to a descriptor heap
    /// </summary>
    public readonly struct DescriptorRangeParameter
    {
        /// <summary>
        /// Indicates the type of the view represented by the descriptors
        /// </summary>
        public readonly DescriptorRangeType Type;

        /// <summary>
        /// The number of descriptors in this range
        /// </summary>
        public readonly uint DescriptorCount;

        /// <summary>
        /// The HLSL register where the range starts
        /// </summary>
        public readonly uint BaseShaderRegister;

        /// <summary>
        /// The HLSL space which the descriptors are in
        /// </summary>
        public readonly uint RegisterSpace;

        /// <summary>
        /// The number of descriptors after the table start that this range begins
        /// </summary>
        public readonly uint DescriptorOffset;

        /// <summary>
        /// Indicates the <see cref="DescriptorOffset"/> should be after the last descriptor of the
        /// </summary>
        public const uint AppendAfterLastDescriptor = 0xFFFFFFFF;

        /// <summary>
        /// Creates a new <see cref="DescriptorRangeParameter"/>
        /// </summary>
        public DescriptorRangeParameter(
            DescriptorRangeType type,
            uint baseShaderRegister,
            uint descriptorCount,
            uint registerSpace,
            uint offsetInDescriptorsFromTableStart = AppendAfterLastDescriptor
        )
        {
            Type = type;
            BaseShaderRegister = baseShaderRegister;
            DescriptorCount = descriptorCount;
            RegisterSpace = registerSpace;
            DescriptorOffset = offsetInDescriptorsFromTableStart;
        }
    }
}
