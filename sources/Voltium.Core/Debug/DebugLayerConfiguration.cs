using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Voltium.Common;
using Voltium.Common.Debugging;


namespace Voltium.Core.Devices
{
    using MessageCallback = Action<GraphicsExceptionMessageType, LogLevel, string>;

    /// <summary>
    /// Defines the settings for Device-Removed Extended Data (DRED)
    /// </summary>
    [Flags]
    public enum DredFlags
    {
        /// <summary>
        /// None. DRED is disabled
        /// </summary>
        None = 0,

        /// <summary>
        /// Auto-breadcrumb metadata to track execution progress is enabled
        /// </summary>
        AutoBreadcrumbs = 1,

        /// <summary>
        /// Allocation metadata to track page faults is enabled
        /// </summary>
        PageFaultMetadata = 2,

        /// <summary>
        /// Watson dump is enabled via Windows Error Reporting (WER)
        /// </summary>
        WatsonDumpEnablement = 4,

        /// <summary>
        /// All DRED settings are enabled
        /// </summary>
        All = AutoBreadcrumbs | PageFaultMetadata | WatsonDumpEnablement
    }

    /// <summary>
    /// Defines the settings for the debug layer
    /// </summary>
    [Flags]
    public enum DebugFlags
    {
        /// <summary>
        /// None. The debug layer is disabled
        /// </summary>
        None = 0,

        /// <summary>
        /// Enable the debug layer
        /// </summary>
        DebugLayer = 1 << 0,

        /// <summary>
        /// Enable GPU-based validation. This allow more thorough debugging but will significantly slow down your app
        /// </summary>
        GpuBasedValidation = 1 << 1,
    }

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
        public DebugFlags DebugFlags { get; set; } = ConfigVars.IsDebug ? DebugFlags.DebugLayer : DebugFlags.None;

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
        public bool ProfilingEnabled { get; set; } = ConfigVars.IsDebug;
    }
}
