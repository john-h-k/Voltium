using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Core;

namespace Voltium.RenderEngine
{
    /// <summary>
    /// Contains static methods used to a execute a <see cref="Application"/>
    /// </summary>
    public static class ApplicationRunner
    {
        /// <summary>
        /// Run a <see cref="Application"/> on Win32
        /// </summary>
        /// <param name="application">The <see cref="Application"/> to run</param>
        /// <param name="width">The width, in pixels, of the screen</param>
        /// <param name="height">The height, in pixels, of the screen</param>
        /// <returns>The exit code of the app</returns>
        public static int RunWin32(Application application, uint width = 700, uint height = 700)
            => Win32ApplicationRunner.Run(application, width, height);



        //public static int RunUwp(Application application)
        //    => UwpApplication.Run(application);
    }
}
