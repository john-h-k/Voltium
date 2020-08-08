using Microsoft.Extensions.Logging;
using Voltium.Common;
using Voltium.Common.Debugging;

namespace Voltium.Core.Devices
{
    /// <summary>
    /// Describes the debugging state used by the engine
    /// </summary>
    public class DebugLayerConfiguration
    {
        /// <summary>
        /// The default <see cref="DebugLayerConfiguration"/>
        /// </summary>
        public static DebugLayerConfiguration Default { get; } = new DebugLayerConfiguration();


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public DebugLayerConfiguration WithDebugFlags(DebugFlags flags)
        {
            DebugFlags = flags;
            return this;
        }

        public DebugLayerConfiguration WithDredFlags(DredFlags flags)
        {
            DredFlags = flags;
            return this;
        }

        public DebugLayerConfiguration WithValidationLogLevel(LogLevel level)
        {
            ValidationLogLevel = level;
            return this;
        }

        public DebugLayerConfiguration WithBreakpointLogLevel(LogLevel level)
        {
            BreakpointLogLevel = level;
            return this;
        }

        /// <summary>
        /// The <see cref="DebugFlags"/> that describes the debug for the application
        /// </summary>
        public DebugFlags DebugFlags { get; set; } = DebugFlags.None;

        /// <summary>
        /// The <see cref="DredFlags"/> that describes the metadata created to help debug device removed scenarios
        /// </summary>
        public DredFlags DredFlags { get; set; } = DredFlags.None;

        /// <summary>
        /// The <see cref="LogLevel"/> used to determine which validation messages should be logged
        /// </summary>
        public LogLevel ValidationLogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Enables debug breakpointing when messages with a given <see cref="LogLevel"/> are sent
        /// </summary>
        public LogLevel BreakpointLogLevel { get; set; } = LogLevel.None;

        /// <summary>
        /// Whether profiling is enabled
        /// </summary>
        public bool ProfilingEnabled { get; set; } = EnvVars.IsDebug;
    }
}
