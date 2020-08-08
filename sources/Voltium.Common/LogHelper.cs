#if DEBUG
#define DEBUG_OUTPUT_CONSOLE
#define LOG_LEVEL_DEBUG
#define LOG
#endif

#if RELEASE
#define LOG_LEVEL_NONE
#endif

using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.HighPerformance.Extensions;

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreSame<T, U>() => typeof(T) == typeof(U);

        private static bool CanSerializeType<T>() => Helpers.IsPrimitive<T>();

        private const string LogSymbol = "LOG";

        private const int BufferSize = 4096;

        [ThreadStatic]
        private static char[]? TextBuffer;
        private static int TextBufferHead;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [MemberNotNull(nameof(TextBuffer))]
        private static void EnsureInitialized()
        {
            if (TextBuffer is not null)
            {
                return;
            }

            InitializeTextBuffer();

            [MethodImpl(MethodImplOptions.NoInlining)]
            [MemberNotNull(nameof(TextBuffer))]
            static void InitializeTextBuffer()
            {
                TextBuffer = new char[BufferSize];
            }
        }

        private static void FlushBuffer()
        {
            Console.Out.Write(TextBuffer.AsSpan(0, TextBufferHead));
            TextBufferHead = 0;
        }

        private static void DirectWriteData(ReadOnlySpan<char> val) => Console.Out.Write(val);

        private static void Write(ReadOnlySpan<char> val)
        {
            EnsureInitialized();
            if (!val.TryCopyTo(TextBuffer.AsSpan(TextBufferHead)))
            {
                if (val.Length > BufferSize)
                {
                    DirectWriteData(val);
                }
                FlushBuffer();
            }
        }

        private static void Write(char val)
        {
            EnsureInitialized();
            if (TextBufferHead < TextBuffer.Length)
            {
                TextBuffer[TextBufferHead++] = val;
            }
            else
            {
                FlushBuffer();
            }
        }

        private static void Write<T>(T val) => Write((val?.ToString() ?? "null").AsSpan());

        private const char NewLine = '\n';

        [Conditional(LogSymbol)]
        [VariadicGeneric("Write(%t)")]
        public static void Log(LogLevel level, ReadOnlySpan<char> message)
        {
            if (level < MinimumLogLevel)
            {
                return;
            }
            Write(message);
            VariadicGenericAttribute.InsertExpressionsHere();
            Write(NewLine);
        }

        [Conditional(LogSymbol)]
        [VariadicGeneric("Log(LogLevel.Trace, message %t...)")]
        public static partial void LogTrace(ReadOnlySpan<char> message);

        [Conditional(LogSymbol)]
        [VariadicGeneric("Log(LogLevel.Debug, message %t...)")]
        public static partial void LogDebug(ReadOnlySpan<char> message);

        [Conditional(LogSymbol)]
        [VariadicGeneric("Log(LogLevel.Information, message %t...)")]
        public static partial void LogInformation(ReadOnlySpan<char> message);

        [Conditional(LogSymbol)]
        [VariadicGeneric("Log(LogLevel.Warning, message %t...)")]
        public static partial void LogWarning(ReadOnlySpan<char> message);

        [Conditional(LogSymbol)]
        [VariadicGeneric("Log(LogLevel.Critical, message %t...)")]
        public static partial void LogCritical(ReadOnlySpan<char> message);


        [Conditional(LogSymbol)]
        [VariadicGeneric("Log(LogLevel.Error, message %t...)")]
        public static partial void LogError(ReadOnlySpan<char> message);
    }
}
