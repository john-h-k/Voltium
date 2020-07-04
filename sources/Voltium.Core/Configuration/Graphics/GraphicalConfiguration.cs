namespace Voltium.Core.Managers
{
    /// <summary>
    /// Describes configurable elements of the graphical engine
    /// </summary>
    public sealed class GraphicalConfiguration
    {
        /// <summary>
        /// The minimum feature level required for execution of the app
        /// </summary>
        public FeatureLevel RequiredFeatureLevel { get; set; } = FeatureLevel.Level11_0;

        /// <summary>
        /// The <see cref="DebugLayerConfiguration"/> used
        /// </summary>
        public DebugLayerConfiguration DebugLayerConfiguration { get; set; } = DebugLayerConfiguration.Default;
    }
}
