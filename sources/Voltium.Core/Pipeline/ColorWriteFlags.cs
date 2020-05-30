using static TerraFX.Interop.D3D12_COLOR_WRITE_ENABLE;


namespace Voltium.Core.Pipeline
{
    /// <summary>
    /// The mask used to determine which channels of an RGBA value
    /// should be written
    /// </summary>
    public enum ColorWriteFlags
    {
        /// <summary>
        /// No channels will be written
        /// </summary>
        None = 0,

       /// <summary>
       /// The red channel will be written
       /// </summary>
        Red = D3D12_COLOR_WRITE_ENABLE_RED,

        /// <summary>
        /// The green channel will be written
        /// </summary>
        Green = D3D12_COLOR_WRITE_ENABLE_GREEN,

        /// <summary>
        /// The blue channel will be written
        /// </summary>
        Blue = D3D12_COLOR_WRITE_ENABLE_BLUE,

        /// <summary>
        /// The alpha channel will be written
        /// </summary>
        Alpha = D3D12_COLOR_WRITE_ENABLE_ALPHA,

        /// <summary>
        /// All channels will be written
        /// </summary>
        All = D3D12_COLOR_WRITE_ENABLE_ALL,
    }
}
