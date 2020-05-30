using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Core.Configuration.Graphics
{
    /// <summary>
    /// Describes the multi-sample anti-aliasing configuration
    /// </summary>
    public readonly struct MsaaDesc
    {
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
