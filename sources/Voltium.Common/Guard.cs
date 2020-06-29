using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using TerraFX.Interop;

namespace Voltium.Common
{
    internal static class Guard
    {
        [MethodImpl(MethodTypes.Validates)]
        public static void NotNull<T>(
            [AllowNull] T val,
            [CallerArgumentExpression("val")] string name = null!,
            [CallerMemberName] string member = null!,
            [CallerLineNumber] int line = default,
            [CallerFilePath] string filePath = null!
        )
        {
            if (val is null)
            {
                ThrowHelper.ThrowArgumentNullException(name,
                    Format($"Object '{name}' null", name, member, line, filePath));
            }
        }

        [MethodImpl(MethodTypes.Validates)]
        public static void Positive(int value,
            [CallerArgumentExpression("val")] string name = null!,
            [CallerMemberName] string member = null!,
            [CallerLineNumber] int line = default,
            [CallerFilePath] string filePath = null!
        )

        {
            if (value < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(name,
                    Format($"Object '{name}' was less than 0", name, member, line, filePath));
            }
        }

        [MethodImpl(MethodTypes.Validates)]
        public static void InRangeExclusive(int value, int lo, int hi,
                [CallerArgumentExpression("val")] string name = null!,
                [CallerMemberName] string member = null!,
                [CallerLineNumber] int line = default,
                [CallerFilePath] string filePath = null!
            )
            // ReSharper disable twice ExplicitCallerInfoArgument
            => InRangeInclusive(value, lo - 1, hi + 1, name, member, line, filePath);

        [MethodImpl(MethodTypes.Validates)]
        public static void InRangeInclusive(int value, int lo, int hi,
            [CallerArgumentExpression("val")] string name = null!,
            [CallerMemberName] string member = null!,
            [CallerLineNumber] int line = default,
            [CallerFilePath] string filePath = null!
        )
        {
            if (value < lo || value > hi)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(name,
                    Format($"Object '{name}' was out of inclusive range '{lo}' - '{hi}' as it had value '{value}'",
                        name, member, line, filePath));
            }
        }

        [MethodImpl(MethodTypes.Validates)]
        public static void Initialized(bool initialized,
            [CallerArgumentExpression("val")] string name = null!,
            [CallerMemberName] string member = null!,
            [CallerLineNumber] int line = default,
            [CallerFilePath] string filePath = null!
        )
        {
            if (!initialized)
            {
                ThrowHelper.ThrowInvalidOperationException(
                    Format($"Object '{name}' was not initialized", name, member, line, filePath));
            }
        }

        [MethodImpl(MethodTypes.SlowPath)]
        public static string Format(
            string? message,
            string? expression = null,
            string? member = null,
            int line = default,
            string? filePath = null
        )
        {
            using var builder = StringHelper.RentStringBuilder();

            builder.AppendLine(message);
            builder.AppendLine($"At file: {filePath}");
            builder.AppendLine($"At line: {line}");
            builder.AppendLine($"In member: {member}");
            builder.AppendLine($"With argument expression: {expression}");

            return builder.ToString();
        }

        [DebuggerNonUserCode]
        [MethodImpl(MethodTypes.Validates)]
        public static void ThrowIfFailed(
            int hr,
#if DEBUG || EXTENDED_ERROR_INFORMATION
            [CallerArgumentExpression("hr")] string? expression = null,
            [CallerFilePath] string? filepath = default,
            [CallerMemberName] string? memberName = default,
            [CallerLineNumber] int lineNumber = default,
#endif
            string? extraInfo = null
        )
        {
            if (Windows.FAILED(hr))
            {
                D3D12DebugShim.WriteAllMessages();
                ThrowHelper.ThrowExternalException(
                    hr,
#if DEBUG || EXTENDED_ERROR_INFORMATION
                    Format($"Native code threw an exception with HR '0x{hr:X8}', message '{ResolveErrorCode(hr)}'. Additional " +
                    $"info '{extraInfo}",
                        expression, memberName, lineNumber, filepath)
#else
                    $"Native code threw an exception with HR '0x{hr:X8}'" +
                    $"Additional info provided '{extraInfo}'"
#endif
                );
            }
        }

        private static string ResolveErrorCode(int hr)
        {
#if REFLECTION
            // TODO this is horrific
            var windows = typeof(Windows);
            foreach (var field in windows.GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (field.GetValue(null) is int val && val == hr)
                {
                    return field.Name;
                }
            }
#endif

            return "<unmapped>";
        }



        [MethodImpl(MethodTypes.Validates)]
        public static void True(
            bool condition,
            string message = ""
#if DEBUG || EXTENDED_ERROR_INFORMATION
            ,
            [CallerArgumentExpression("condition")]
            string? expression = null,
            [CallerFilePath] string? filepath = default,
            [CallerMemberName] string? memberName = default,
            [CallerLineNumber] int lineNumber = default
#endif
        )
        {
            {
                if (!condition)
                {
                    ThrowHelper.ThrowInvalidOperationException(
#if DEBUG || EXTENDED_ERROR_INFORMATION
                        Format(message, expression, memberName, lineNumber, filepath)
#else
                        message
#endif
                    );
                }
            }
        }

        [Conditional("DEBUG")]
        [Conditional("TRACE_DISPOSABLES")]
        public static void MarkDisposableFinalizerEntered(
            [CallerFilePath] string? filepath = default,
            [CallerMemberName] string? memberName = default,
            [CallerLineNumber] int lineNumber = default
        )
        {
            Logger.LogError(
                "OBJECT NOT DISPOSED ERROR\nFile: {0}\nMember: {1}\nLine: {2}\n",
                filepath!, memberName!, lineNumber
            );

#if DISPOSABLES_ALLOW_FINALIZE
            Debug.Fail("OBJECT NOT DISPOSED ERROR - see logs");
#endif
        }
    }

    //public class GraphicsException : Exception
    //{
    //}
}
