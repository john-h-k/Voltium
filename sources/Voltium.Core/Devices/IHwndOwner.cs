using System;

namespace Voltium.Core.Managers
{
    /// <summary>
    /// Indicates a type owns a HWND and can be used as an output
    /// </summary>
    public interface IHwndOwner
    {
        /// <summary>
        /// Get the <see cref="IntPtr"/> handle for the window
        /// </summary>
        /// <returns>The <see cref="IntPtr"/> for the window</returns>
        public IntPtr GetHwnd();
    }
}
