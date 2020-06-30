using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using static TerraFX.Interop.Windows;

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
            if (FAILED(hr))
            {
                D3D12DebugShim.WriteAllMessages();
                ThrowHelper.ThrowExternalException(
                    hr,
#if DEBUG || EXTENDED_ERROR_INFORMATION
                    Format($"Native code threw an exception with HR '0x{hr:X8}', message '{ResolveErrorCode(hr)}'. " +
                    extraInfo is null ? "" : $"Additional info '{extraInfo}",
                        expression, memberName, lineNumber, filepath)
#else
                    $"Native code threw an exception with HR '0x{hr:X8}, message '{ResolveErrorCode(hr)}''" +
                    extraInfo is null ? "" : $"Additional info '{extraInfo}"
#endif
                );
            }
        }

        private enum ErrorContext
        {
            Unspecified = 0,
            Win32 = FACILITY_WIN32,
            Dxgi = FACILITY_DXGI,
            D3D12 = FACILITY_DIRECT3D12,
            // There don't seem to be any errors for this facility
            //D3D12Debug = FACILITY_DIRECT3D12_DEBUG,
            DXCore = FACILITY_DXCORE
        }

        private static string ResolveErrorCode(int hr, ErrorContext context = ErrorContext.Unspecified)
        {
            var facility = context == ErrorContext.Unspecified ? HRESULT_FACILITY(hr) : (int)context;

            return facility switch
            {
                FACILITY_WIN32 => ResolveErrorCodeOrWin32(hr, Win32Messages),
                FACILITY_DXGI => ResolveErrorCodeOrWin32(hr, DxgiMessages),
                FACILITY_DXCORE => ResolveErrorCodeOrWin32(hr, DXCoreMessages),
                FACILITY_DIRECT3D12 => ResolveErrorCodeOrWin32(hr, D3D12Messages),
                _ => ResolveErrorCodeOrWin32(hr, null),
            };
        }

        private static string ResolveErrorCodeOrWin32(int hr, Dictionary<int, string>? map)
        {
            string? msg = null;
            if (map?.TryGetValue(hr, out msg) ?? false)
            {
                return msg ?? ErrMessage_UndocumentedError;
            }

            return Win32Messages.TryGetValue(hr, out msg) ? msg : ErrMessage_UndocumentedError;
        }

        private static readonly Dictionary<int, string> Win32Messages = new()
        {
            [E_ABORT] = "E_ABORT: Operation aborted",
            [E_FAIL] = "E_FAIL: E_FAIL: Unspecified failure",
            [E_INVALIDARG] = "E_INVALIDARG: An invalid parameter was passed to the function",
            [E_OUTOFMEMORY] = "E_OUTOFMEMORY: The system did not have enough memory to fulfil the request",
            [E_POINTER] = "E_POINTER: A null or invalid pointer was passed to the method",
            [E_NOINTERFACE] = "E_NOINTERFACE: The requested interface could not be provided by the function"
        };

        private static readonly Dictionary<int, string> DxgiMessages = new()
        {
            [DXGI_ERROR_INVALID_CALL] = "DXGI_ERROR_INVALID_CALL: The application provided invalid parameter data",
            [DXGI_ERROR_NOT_FOUND] = "DXGI_ERROR_NOT_FOUND: When calling IDXGIObject::GetPrivateData, the GUID passed in is not recognized as one previously passed to IDXGIObject::SetPrivateData or IDXGIObject::SetPrivateDataInterface. When calling IDXGIFactory::EnumAdapters or IDXGIAdapter::EnumOutputs, the enumerated ordinal is out of range",
            [DXGI_ERROR_MORE_DATA] = "DXGI_ERROR_MORE_DATA: The buffer supplied by the application is not big enough to hold the requested data",
            [DXGI_ERROR_UNSUPPORTED] = "DXGI_ERROR_UNSUPPORTED: The requested functionality is not supported by the device or the driver",
            [DXGI_ERROR_DEVICE_REMOVED] = "DXGI_ERROR_DEVICE_REMOVED: The video card has been physically removed from the system, or a driver upgrade for the video card has occurred. The application should destroy and recreate the device",
            [DXGI_ERROR_DEVICE_HUNG] = "DXGI_ERROR_DEVICE_HUNG: The application's device failed due to badly formed commands sent by the application",
            [DXGI_ERROR_DEVICE_RESET] = "DXGI_ERROR_DEVICE_RESET: The device failed due to a badly formed command. This is a run-time issue; The application should destroy and recreate the device",
            [DXGI_ERROR_WAS_STILL_DRAWING] = "DXGI_ERROR_WAS_STILL_DRAWING: The GPU was busy at the moment when a call was made to perform an operation, and did not execute or schedule the operation",
            [DXGI_ERROR_FRAME_STATISTICS_DISJOINT] = "DXGI_ERROR_FRAME_STATISTICS_DISJOINT: An event (for example, a power cycle) interrupted the gathering of presentation statistics",
            [DXGI_ERROR_GRAPHICS_VIDPN_SOURCE_IN_USE] = "DXGI_ERROR_GRAPHICS_VIDPN_SOURCE_IN_USE: The application attempted to acquire exclusive ownership of an output, but failed because some other application (or device within the application) already acquired ownership",
            [DXGI_ERROR_DRIVER_INTERNAL_ERROR] = "DXGI_ERROR_DRIVER_INTERNAL_ERROR: The driver encountered a problem and was put into the device removed state",
            [DXGI_ERROR_NONEXCLUSIVE] = "DXGI_ERROR_NONEXCLUSIVE: A global counter resource is in use, and the Direct3D device can't currently use the counter resource",
            [DXGI_ERROR_NOT_CURRENTLY_AVAILABLE] = "DXGI_ERROR_NOT_CURRENTLY_AVAILABLE: The resource or request is not currently available, but it might become available later",
            [DXGI_ERROR_REMOTE_CLIENT_DISCONNECTED] = "DXGI_ERROR_REMOTE_CLIENT_DISCONNECTED: Reserved; receiving this error code likely indicates it is not meant to be a DXGI error",
            [DXGI_ERROR_REMOTE_OUTOFMEMORY] = "DXGI_ERROR_REMOTE_OUTOFMEMORY: Reserved; receiving this error code likely indicates it is not meant to be a DXGI error",
            [DXGI_ERROR_ACCESS_LOST] = "DXGI_ERROR_ACCESS_LOST: The desktop duplication interface is invalid. The desktop duplication interface typically becomes invalid when a different type of image is displayed on the desktop",
            [DXGI_ERROR_WAIT_TIMEOUT] = "DXGI_ERROR_WAIT_TIMEOUT: The time-out interval elapsed before the next desktop frame was available",
            [DXGI_ERROR_SESSION_DISCONNECTED] = "DXGI_ERROR_SESSION_DISCONNECTED: The Remote Desktop Services session is currently disconnected",
            [DXGI_ERROR_RESTRICT_TO_OUTPUT_STALE] = "DXGI_ERROR_RESTRICT_TO_OUTPUT_STALE: The DXGI output (monitor) to which the swap chain content was restricted is now disconnected or changed",
            [DXGI_ERROR_CANNOT_PROTECT_CONTENT] = "DXGI_ERROR_CANNOT_PROTECT_CONTENT: DXGI can't provide content protection on the swap chain. This error is typically caused by an older driver, or when you use a swap chain that is incompatible with content protection",
            [DXGI_ERROR_ACCESS_DENIED] = "DXGI_ERROR_ACCESS_DENIED: You tried to use a resource to which you did not have the required access privileges. This error is most typically caused when you write to a shared resource with read-only access",
            [DXGI_ERROR_NAME_ALREADY_EXISTS] = "DXGI_ERROR_NAME_ALREADY_EXISTS: The supplied name of a resource in a call to IDXGIResource1::CreateSharedHandle is already associated with some other resource",
            [DXGI_ERROR_SDK_COMPONENT_MISSING] = "DXGI_ERROR_SDK_COMPONENT_MISSING: The operation depends on an SDK component that is missing or mismatched",
            [DXGI_ERROR_NOT_CURRENT] = ErrMessage_UndocumentedError,
            [DXGI_ERROR_HW_PROTECTION_OUTOFMEMORY] = ErrMessage_UndocumentedError,
            [DXGI_ERROR_DYNAMIC_CODE_POLICY_VIOLATION] = ErrMessage_UndocumentedError,
            [DXGI_ERROR_NON_COMPOSITED_UI] = ErrMessage_UndocumentedError,
            [DXGI_ERROR_MODE_CHANGE_IN_PROGRESS] = ErrMessage_UndocumentedError,
            [DXGI_ERROR_CACHE_CORRUPT] = ErrMessage_UndocumentedError,
            [DXGI_ERROR_CACHE_FULL] = ErrMessage_UndocumentedError,
            [DXGI_ERROR_CACHE_HASH_COLLISION] = ErrMessage_UndocumentedError,
            [DXGI_ERROR_ALREADY_EXISTS] = "DXGI_ERROR_ALREADY_EXISTS: The desired element already exists. This is returned by DXGIDeclareAdapterRemovalSupport if it is not the first time that the function is called",
        };

        private static readonly Dictionary<int, string> DXCoreMessages = new()
        {
            [DXCORE_ERROR_EVENT_NOT_UNREGISTERED] = ErrMessage_UndocumentedError
        };

        private static readonly Dictionary<int, string> D3D12Messages = new()
        {
            [D3D12_ERROR_ADAPTER_NOT_FOUND] = "D3D12_ERROR_ADAPTER_NOT_FOUND: Cached PSO was created on a different device and cannot be reused on the current device",
            [D3D12_ERROR_DRIVER_VERSION_MISMATCH] = "D3D12_ERROR_DRIVER_VERSION_MISMATCH: Cached PSO was created on a different driver version and cannot be reused on the current device",
        };

        private const string ErrMessage_UndocumentedError = "This error is undocumented :(";

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

#if !DISPOSABLES_ALLOW_FINALIZE
            Debug.Fail("OBJECT NOT DISPOSED ERROR - see logs");
#endif
        }
    }

    //public class GraphicsException : Exception
    //{
    //}
}
