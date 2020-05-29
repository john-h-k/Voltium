using TerraFX.Interop;

namespace Voltium.Core.Managers
{
    /// <summary>
    /// Provides data about a target screen
    /// </summary>
    public readonly struct ScreenData
    {
        /// <summary>
        /// The height of the screen, in pixels
        /// </summary>
        public readonly uint Height;

        /// <summary>
        ///  The width of the screen, in pixels
        /// </summary>
        public readonly uint Width;

        /// <summary>
        /// The size of the scene, which is <see cref="Height"/> multiplied by <see cref="Width"/>
        /// </summary>
        public uint Size => Height * Width;

        /// <summary>
        /// The handle to the window
        /// </summary>
        public readonly HWND Handle;

        /// <summary>
        /// Create a new instance of <see cref="ScreenData"/>
        /// </summary>
        public ScreenData(uint height, uint width, HWND handle)
        {
            Height = height;
            Width = width;
            Handle = handle;
        }
    }
}
