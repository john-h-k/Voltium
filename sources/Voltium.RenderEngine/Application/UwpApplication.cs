using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Core
{
    /// <summary>
    /// Used for creation and execution of UWP backed applications
    /// </summary>
    public static class UwpApplication
    {
        /// <summary>
        /// Run a <see cref="Application"/> on Win32
        /// </summary>
        /// <param name="application">The <see cref="Application"/> to run</param>
        /// <param name="width">The width, in pixels, of the screen</param>
        /// <param name="height">The height, in pixels, of the screen</param>
        /// <returns>The exit code of the app</returns>
        public static int Run(Application application, uint width, uint height)
        {
            throw new NotImplementedException();
        }
    }
}
