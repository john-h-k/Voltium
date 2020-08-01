namespace Voltium.Core.Devices
{
    /// <summary>
    /// Describes configurable elements of the graphical engine
    /// </summary>
    public sealed class DeviceConfiguration
    {
        /// <summary>
        /// The default <see cref="DeviceConfiguration"/>, which requires <see cref="FeatureLevel.GraphicsLevel11_0"/> and has <see cref="DebugLayerConfiguration.Default"/>
        /// </summary>
        public static DeviceConfiguration Default { get; } = new DeviceConfiguration();

        /// <summary>
        /// The default <see cref="DeviceConfiguration"/> without a debug layer, which requires <see cref="FeatureLevel.GraphicsLevel11_0"/>
        /// </summary>
        public static DeviceConfiguration NoDebugLayer { get; } = new DeviceConfiguration { DebugLayerConfiguration = null };

        /// <summary>
        /// The minimum feature level required for execution of the app
        /// </summary>
        public FeatureLevel RequiredFeatureLevel { get; set; } = FeatureLevel.GraphicsLevel11_0;

        /// <summary>
        /// The <see cref="DebugLayerConfiguration"/> used
        /// </summary>
        public DebugLayerConfiguration? DebugLayerConfiguration { get; set; } = DebugLayerConfiguration.Default;
    }
}
