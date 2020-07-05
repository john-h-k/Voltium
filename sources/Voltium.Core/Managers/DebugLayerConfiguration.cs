using Microsoft.Extensions.Logging;
using Voltium.Common.Debugging;

namespace Voltium.Core.Managers
{
    /// <summary>
    /// Describes the debugging state used by the engine
    /// </summary>
    public class DebugLayerConfiguration
    {
        /// <summary>
        /// Disables <see cref="Validation"/>
        /// </summary>
        public DebugLayerConfiguration DisableValidation()
        {
            Validation.Disable();
            return this;
        }

        /// <summary>
        /// Disables <see cref="DeviceRemovedMetadata"/>
        /// </summary>
        public DebugLayerConfiguration DisableDeviceRemovedMetadata()
        {
            DeviceRemovedMetadata.Disable();
            return this;
        }

        /// <summary>
        /// The default <see cref="DebugLayerConfiguration"/>
        /// </summary>
        public static DebugLayerConfiguration Default { get; } = new DebugLayerConfiguration();

        /// <summary>
        /// The <see cref="ValidationConfig"/> that describes the validation configuration for the application
        /// </summary>
        public ValidationConfig Validation { get; set; } = ValidationConfig.Default;

        /// <summary>
        /// The <see cref="DeviceRemovedMetadataConfig"/> that describes the metadata created to help debug device removed scenarios
        /// </summary>
        public DeviceRemovedMetadataConfig DeviceRemovedMetadata { get; set; } = DeviceRemovedMetadataConfig.Default;

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

    /// <summary>
    /// Describes the state of the validation for the application
    /// </summary>
    public class ValidationConfig
    {
        /// <summary>
        /// Disables all elements
        /// </summary>
        public ValidationConfig Disable()
        {
            GraphicsLayerValidation = false;
            InfrastructureLayerValidation = false;
            GpuBasedValidation = false;
            return this;
        }

        /// <summary>
        /// The default <see cref="ValidationConfig"/>
        /// </summary>
        public static ValidationConfig Default { get; } = new ValidationConfig
        {
            GraphicsLayerValidation = EnvVars.IsDebug,
            InfrastructureLayerValidation = EnvVars.IsDebug,
            GpuBasedValidation = false
        };

        /// <summary>
        /// Whether CPU-based graphics validation is enabled
        /// </summary>
        public bool GraphicsLayerValidation { get; set; }

        /// <summary>
        /// Whether CPU-based graphics infrastructure validation is enabled
        /// </summary>
        public bool InfrastructureLayerValidation { get; set; }

        /// <summary>
        /// Whether GPU-based graphics validation is enabled
        /// </summary>
        public bool GpuBasedValidation { get; set; }
    }


    /// <summary>
    /// Describes the state of extended device removal data
    /// </summary>
    public class DeviceRemovedMetadataConfig
    {
        /// <summary>
        /// Disables all elements
        /// </summary>
        public DeviceRemovedMetadataConfig Disable()
        {
            PageFaultMetadata = false;
            AutoBreadcrumbMetadata = false;
            WindowsErrorReporting = false;
            return this;
        }

        /// <summary>
        /// The default <see cref="DeviceRemovedMetadataConfig"/>
        /// </summary>
        public static DeviceRemovedMetadataConfig Default { get; } = new DeviceRemovedMetadataConfig
        {
            PageFaultMetadata = EnvVars.IsDebug,
            AutoBreadcrumbMetadata = EnvVars.IsDebug,
            WindowsErrorReporting = false
        };

        internal bool RequiresDredSupport => PageFaultMetadata || AutoBreadcrumbMetadata || WindowsErrorReporting;

        /// <summary>
        /// Whether metadata about allocations and resource VAs is stored to help diagnose page faults
        /// </summary>
        public bool PageFaultMetadata { get; set; }
        /// <summary>
        /// Whether metadata about execution point (referred to as a breadcrumsb) is enabled
        /// </summary>
        public bool AutoBreadcrumbMetadata { get; set; }

        /// <summary>
        /// Whether Windows Error Reporting (WER, otherwise known as Watson) is enabled
        /// </summary>
        public bool WindowsErrorReporting { get; set; }
    }
}
