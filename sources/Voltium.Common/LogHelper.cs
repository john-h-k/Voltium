#if DEBUG || DEBUG_LOG
#define DEBUG_OUTPUT_CONSOLE
#define LOG_LEVEL_DEBUG
#define LOG
#endif

#if RELEASE
#define LOG_LEVEL_NONE
#endif

using Microsoft.Extensions.Logging;
using ZLogger;

namespace Voltium.Common
{

    internal static class LogHelper
    {
        private const string LogFile = "log.txt";

        public static LogLevel MinimumLogLevel
        {
            get
            {
#if LOG_LEVEL_TRACE
                return LogLevel.Trace;
#elif LOG_LEVEL_DEBUG
                return LogLevel.Debug;
#elif LOG_LEVEL_INFORMATION
                return LogLevel.Information;
#elif LOG_LEVEL_WARNING
                return LogLevel.Warning;
#elif LOG_LEVEL_CRITICAL
                return LogLevel.Critical;
#elif LOG_LEVEL_ERROR
                return LogLevel.Error;
#elif LOG_LEVEL_NONE
                return LogLevel.None;
#else
#error No log level defined
#endif
            }
        }

        public static readonly ILogger Logger = LoggerFactory.Create(builder =>
        {
            _ = builder.ClearProviders()
                       .SetMinimumLevel(MinimumLogLevel)
                       .AddZLoggerConsole();
        }).CreateLogger("GlobalLogger");
    }
}
