using System;

namespace Voltium.Common.Debugging
{
    /// <summary>
    /// Defines the set of environment variables used by Voltium
    /// </summary>
    public static class Configuration

    {
        /// <summary>
        /// Whether the build was compiled with DEBUG
        /// </summary>
        public static bool IsDebug =>
#if DEBUG
            true;
#elif AOT
            false;
#else
            AppContext.GetData("IsDebug") is 1;
#endif



        /// <summary>
        /// Whether the build should be built for profiling
        /// </summary>
        public static bool EnableProfiling =>
#if DEBUG
            false;
#elif AOT && ENABLE_PROFILING
            true;
#elif AOT
            false;
#else
            AppContext.GetData("EnableProfiling") is 1;
#endif

    }
}
