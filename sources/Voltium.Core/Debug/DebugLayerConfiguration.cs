using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Voltium.Common;
using Voltium.Common.Debugging;


namespace Voltium.Core.Devices
{
    using MessageCallback = Action<GraphicsExceptionMessageType, LogLevel, string>;

    /// <summary>
    /// Describes the debugging state used by the engine
    /// </summary>
    [Fluent]
    public sealed partial class DebugLayerConfiguration
    {
        private List<MessageCallback> _callbacks = new();


        private static readonly DebugLayerConfiguration _debug = new DebugLayerConfiguration()
            .WithDebugFlags(DebugFlags.DebugLayer)
            .WithDredFlags(DredFlags.All)
            .WithValidationLogLevel(LogLevel.Information)
            .WithBreakpointLogLevel(LogLevel.Error)
            .WithProfilingEnabled(false);

        private static readonly DebugLayerConfiguration _none = new DebugLayerConfiguration()
            .WithDebugFlags(DebugFlags.None)
            .WithDredFlags(DredFlags.None)
            .WithValidationLogLevel(LogLevel.None)
            .WithBreakpointLogLevel(LogLevel.None)
            .WithProfilingEnabled(false);

        private static readonly DebugLayerConfiguration _profile = new DebugLayerConfiguration()
            .WithDebugFlags(DebugFlags.None)
            .WithDredFlags(DredFlags.None)
            .WithValidationLogLevel(LogLevel.None)
            .WithBreakpointLogLevel(LogLevel.None)
            .WithProfilingEnabled(true);

        /// <summary>
        /// The default layer for debugging
        /// </summary>
        public static DebugLayerConfiguration Debug => (DebugLayerConfiguration)_debug.MemberwiseClone();


        /// <summary>
        /// No debug layer, logging, or profiling
        /// </summary>
        public static DebugLayerConfiguration None => (DebugLayerConfiguration)_none.MemberwiseClone();

        /// <summary>
        /// The default layer for profiling
        /// </summary>
        public static DebugLayerConfiguration Profile => (DebugLayerConfiguration)_profile.MemberwiseClone();

        /// <summary>
        /// The <see cref="DebugFlags"/> that describes the debug for the application
        /// </summary>
        public DebugFlags DebugFlags { get; set; } = ConfigurationA
.IsDebug ? DebugFlags.DebugLayer : DebugFlags.None;

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
        public bool ProfilingEnabled { get; set; } = ConfigurationA
.IsDebug;
    }
}
