using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Voltium.Core.Managers
{
    /// <summary>
    /// Describes the debugging state used by the engine
    /// </summary>
    public class DebugLayer
    {
        /// <summary>
        /// Whether CPU-based graphics validation is enabled
        /// </summary>
        public bool CpuValidationEnabled { get; set; }

        /// <summary>
        /// Whether GPU-based graphics validation is enabled
        /// </summary>
        public bool GpuValidationEnabled { get; set; }

        /// <summary>
        /// The <see cref="LogLevel"/> used by the logging infrastructure
        /// </summary>
        public LogLevel LogLevel { get; set; }

        /// <summary>
        /// Whether profiling is enabled
        /// </summary>
        public bool ProfilingEnabled { get; set; }
    }
}
