using Voltium.Common;

namespace Voltium.Core.Configuration.Graphics
{
    /// <summary>
    /// Describes the multi-sample anti-aliasing configuration
    /// </summary>
    [GenerateEquality]
    public partial struct MsaaDesc
    {
        /// <summary>
        /// No multi-sampling. This is the default
        /// </summary>
        public static MsaaDesc None => new MsaaDesc(1, 0);

        /// <summary>
        /// 2x multi-sampling at the default quality level
        /// </summary>
        public static MsaaDesc X2 => new MsaaDesc(2, 0);

        /// <summary>
        /// 4x multi-sampling at the default quality level
        /// </summary>
        public static MsaaDesc X4 => new MsaaDesc(4, 0);

        /// <summary>
        /// 8x multi-sampling at the default quality level
        /// </summary>
        public static MsaaDesc X8 => new MsaaDesc(8, 0);

        /// <summary>
        /// Determines whether this <see cref="MsaaDesc"/> means the resource is multisampled or singlesampled
        /// </summary>
        public bool IsMultiSampled => SampleCount > 1;

        /// <summary>
        /// Creates a new <see cref="MsaaDesc"/> with a new sample count
        /// </summary>
        /// <param name="sampleCount">The sample count to ues</param>
        public MsaaDesc WithSampleCount(uint sampleCount) => new MsaaDesc(sampleCount, QualityLevel);

        /// <summary>
        /// Creates a new <see cref="MsaaDesc"/> with a new quality level
        /// </summary>
        /// <param name="qualityLevel">The quality level to ues</param>
        public MsaaDesc WithQualityLevel(uint qualityLevel) => new MsaaDesc(SampleCount, qualityLevel);

        /// <summary>
        /// The number of samples taken
        /// </summary>
        public uint SampleCount;

        /// <summary>
        /// The quality level, where 0 is the lowest level and the highest is determined by querying GPU data
        /// </summary>
        public uint QualityLevel;

        /// <summary>
        /// Creates a new <see cref="MsaaDesc"/>
        /// </summary>
        public MsaaDesc(uint sampleCount, uint qualityLevel)
        {
            SampleCount = sampleCount;
            QualityLevel = qualityLevel;
        }
    }
}
