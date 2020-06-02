#if DEBUG || DEBUG_LOG
#define DEBUG_OUTPUT_CONSOLE
#define LOG_LEVEL_DEBUG
#define LOG
#endif

#if RELEASE
#define LOG_LEVEL_NONE
#endif

using System;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using ZLogger;
using ZLogger.Providers;

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
            //var factory = LoggerFactory.Create(builder =>
            //{
            //    _ = builder.ClearProviders()
            //               .SetMinimumLevel(MinimumLogLevel)
            //               .AddZLoggerConsole();
            //});

            //factory.CreateLogger("DefaultLogContext");
        }

        // TODO

        //[Conditional("LOG")]
        public static void Log(LogLevel level, string format, params object[]objects)
        {
            Console.WriteLine(format); //Default.ZLog(level, format);
        }

        //[Conditional("LOG")]
        public static void LogTrace(string format, params object[]objects)
        {
            Console.WriteLine(format); //Default.ZLogTrace(format);
        }

        //[Conditional("LOG")]
        public static void LogDebug(string format, params object[]objects)
        {
            Console.WriteLine(format); //Default.ZLogDebug(format);
        }

        //[Conditional("LOG")]
        public static void LogInformation(string format, params object[]objects)
        {
            Console.WriteLine(format); //Default.ZLogInformation(format);
        }

        //[Conditional("LOG")]
        public static void LogWarning(string format, params object[]objects)
        {
            Console.WriteLine(format); //Default.ZLogWarning(format);
        }

        //[Conditional("LOG")]
        public static void LogCritical(string format, params object[]objects)
        {
            Console.WriteLine(format); //Default.ZLogCritical(format);
        }

        //[Conditional("LOG")]
        public static void LogError(string format, params object[]objects)
        {
            Console.WriteLine(format); //Default.ZLogError(format);
        }
    }
}
