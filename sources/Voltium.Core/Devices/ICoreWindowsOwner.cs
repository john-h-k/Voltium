namespace Voltium.Core.Managers
{
    /// <summary>
    /// Indicates a type owns a WinRt ICoreWindow and can be used as an output
    /// </summary>
    public unsafe interface ICoreWindowsOwner
    {
        /// <summary>
        /// Get the IUnknown* for the window
        /// </summary>
        /// <returns>The IUnknown* for the window, as a void*</returns>
        public void* GetIUnknownForWindow();
    }
}
