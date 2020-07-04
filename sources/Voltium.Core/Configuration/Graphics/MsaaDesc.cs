namespace Voltium.Core.Configuration.Graphics
{
    /// <summary>
    /// Describes the multi-sample anti-aliasing configuration
    /// </summary>
    public readonly struct MsaaDesc
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
        /// The number of samples taken
        /// </summary>
        public readonly uint SampleCount;

        /// <summary>
        /// The quality level, where 0 is the lowest level and the highest is determined by querying GPU data
        /// </summary>
        public readonly uint QualityLevel;

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
