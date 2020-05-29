#if DEBUG || DEBUG_LOG
#define DEBUG_OUTPUT_CONSOLE
#define LOG_LEVEL_DEBUG
#endif

#if RELEASE
#define LOG_LEVEL_NONE
#endif

using System;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
#if ZLOGGER
using ZLogger;
using ZLogger.Providers;
#endif

namespace Voltium.Common
{

    internal static class Logger
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

        static Logger()
         {
//             var factory = LoggerFactory.Create(builder => {
//                 builder.ClearProviders();
//                 builder.SetMinimumLevel(MinimumLogLevel);
//
// #if DEBUG_OUTPUT_CONSOLE && ZLOGGER
//                 builder.AddZLoggerConsole();
// #endif
// #if DEBUG_OUTPUT_FILE
//                 builder.AddZLoggerFile(LogFile);
// #endif
//             });
//
//             Default = factory.CreateLogger("DefaultLogContext");
        }

        // TODO

        [Conditional("LOG")]
        public static void Log(LogLevel level, ReadOnlySpan<char> format, params object[] objects)
        {

        }

        [Conditional("LOG")]
        public static void LogTrace(ReadOnlySpan<char> format, params object[] objects)
        {

        }

        [Conditional("LOG")]
        public static void LogDebug(ReadOnlySpan<char> format, params object[] objects)
        {

        }

        [Conditional("LOG")]
        public static void LogInformation(ReadOnlySpan<char> format, params object[] objects)
        {

        }

        [Conditional("LOG")]
        public static void LogWarning(ReadOnlySpan<char> format, params object[] objects)
        {

        }

        [Conditional("LOG")]
        public static void LogCritical(ReadOnlySpan<char> format, params object[] objects)
        {

        }

        [Conditional("LOG")]
        public static void LogError(ReadOnlySpan<char> format, params object[] objects)
        {

        }
    }
}
