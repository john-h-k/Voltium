using Voltium.Common;

namespace Voltium.Core.Configuration.Graphics
{
    /// <summary>
    /// Describes the multi-sample anti-aliasing configuration
    /// </summary>
    [GenerateEquality]
    public readonly partial struct MultisamplingDesc
    {
        /// <summary>
        /// No multi-sampling. This is the default
        /// </summary>
        public static MultisamplingDesc None => new MultisamplingDesc(1, 0);

        /// <summary>
        /// 2x multi-sampling at the default quality level
        /// </summary>
        public static MultisamplingDesc X2 => new MultisamplingDesc(2, 0);

        /// <summary>
        /// 4x multi-sampling at the default quality level
        /// </summary>
        public static MultisamplingDesc X4 => new MultisamplingDesc(4, 0);

        /// <summary>
        /// 8x multi-sampling at the default quality level
        /// </summary>
        public static MultisamplingDesc X8 => new MultisamplingDesc(8, 0);

        /// <summary>
        /// Determines whether this <see cref="MultisamplingDesc"/> means the resource is multisampled or singlesampled
        /// </summary>
        public bool IsMultiSampled => SampleCount > 1;

        /// <summary>
        /// Creates a new <see cref="MultisamplingDesc"/> with a new sample count
        /// </summary>
        /// <param name="sampleCount">The sample count to ues</param>
        public MultisamplingDesc WithSampleCount(uint sampleCount) => new MultisamplingDesc(sampleCount, QualityLevel);

        /// <summary>
        /// Creates a new <see cref="MultisamplingDesc"/> with a new quality level
        /// </summary>
        /// <param name="qualityLevel">The quality level to ues</param>
        public MultisamplingDesc WithQualityLevel(uint qualityLevel) => new MultisamplingDesc(SampleCount, qualityLevel);

        /// <summary>
        /// The number of samples taken
        /// </summary>
        public readonly uint SampleCount;

        /// <summary>
        /// The quality level, where 0 is the lowest level and the highest is determined by querying GPU data
        /// </summary>
        public readonly uint QualityLevel;

        /// <summary>
        /// Creates a new <see cref="MultisamplingDesc"/>
        /// </summary>
        public MultisamplingDesc(uint sampleCount, uint qualityLevel)
        {
            SampleCount = sampleCount;
            QualityLevel = qualityLevel;
        }
    }
}
