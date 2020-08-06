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
using System.IO;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Voltium.Common
{
    internal static partial class LogHelper
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

        private const string LogSymbol = "DEBUG";

        private static readonly ILogger Logger = LoggerFactory.Create(builder =>
        {
            _ = builder.ClearProviders()
                       .SetMinimumLevel(MinimumLogLevel)
                       .AddZLoggerConsole(options => options.EnableStructuredLogging = false);
        }).CreateLogger("GlobalLogger");

        private static TextWriter Out => Console.Out;

        [Conditional(LogSymbol)]
        public static void Log(LogLevel level, string message) => Out.WriteLine(message);

        [Conditional(LogSymbol)]
        public static void LogTrace(string message) => Out.WriteLine(message);

        [Conditional(LogSymbol)]
        public static void LogDebug(string message) => Out.WriteLine(message);

        [Conditional(LogSymbol)]
        public static void LogInformation(string message) => Out.WriteLine(message);

        [Conditional(LogSymbol)]
        public static void LogWarning(string message) => Out.WriteLine(message);

        [Conditional(LogSymbol)]
        public static void LogCritical(string message) => Out.WriteLine(message);

        //[Conditional(LogSymbol)]
        //public static void LogError(string message) => Out.WriteLine(message);


        [Conditional(LogSymbol)]
        [VariadicGeneric("Out.Write(%t)")]
        public static void LogError(string message)
        {
            Out.Write(message);
            VariadicGenericAttribute.InsertExpressionsHere();
            Out.WriteLine();
        }
    }
}
