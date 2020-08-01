namespace Voltium.RenderEngine
{
    /// <summary>
    /// Indicates the type of output
    /// </summary>
    public enum OutputClass
    {
        /// <summary>
        /// There is no output
        /// </summary>
        None,

        /// <summary>
        /// This is the primary output for the render graph. You can only have one primary output per render graph
        /// </summary>
        Primary,

        /// <summary>
        /// This is a secondary output
        /// </summary>
        Secondary
    }
}
