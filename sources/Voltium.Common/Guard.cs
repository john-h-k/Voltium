using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
                    FormatExtendedErrorInformation($"Object '{name}' null", name, member, line, filePath));
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
                    FormatExtendedErrorInformation($"Object '{name}' was less than 0", name, member, line, filePath));
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
                    FormatExtendedErrorInformation($"Object '{name}' was out of inclusive range '{lo}' - '{hi}' as it had value '{value}'",
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
                    FormatExtendedErrorInformation($"Object '{name}' was not initialized", name, member, line, filePath));
            }
        }

        [MethodImpl(MethodTypes.SlowPath)]
        public static string FormatExtendedErrorInformation(
            string? message,
            string? expression = null,
            string? member = null,
            int line = default,
            string? filePath = null
        )
        {
            using var builder = StringHelpers.RentStringBuilder();

            builder.AppendLine(message);
            builder.AppendLine($"At file: {filePath}");
            builder.AppendLine($"At line: {line}");
            builder.AppendLine($"In member: {member}");
            builder.AppendLine($"With argument expression: {expression}");

            return builder.ToString();
        }

        [DebuggerNonUserCode]
        [MethodImpl(MethodTypes.Validates)]
        public static bool TryGetInterface(
            int hr,
            [CallerArgumentExpression("hr")] string? expression = null
#if DEBUG || EXTENDED_ERROR_INFORMATION
            ,
            [CallerFilePath] string? filepath = default,
            [CallerMemberName] string? memberName = default,
            [CallerLineNumber] int lineNumber = default
#endif
        )
        {
            // invert branch so JIT assumes the HR is success
            if (SUCCEEDED(hr) || hr == E_NOINTERFACE)
            {
                return hr != E_NOINTERFACE;
            }

            ThrowForHr(hr
#if DEBUG || EXTENDED_ERROR_INFORMATION
                    ,
                expression, filepath, memberName, lineNumber
#endif
                    );

            return false; // never reached
        }

        [DebuggerNonUserCode]
        [MethodImpl(MethodTypes.Validates)]
        public static void ThrowIfFailed(
            int hr,
            [CallerArgumentExpression("hr")] string? expression = null
#if DEBUG || EXTENDED_ERROR_INFORMATION
            ,
            [CallerFilePath] string? filepath = default,
            [CallerMemberName] string? memberName = default,
            [CallerLineNumber] int lineNumber = default
#endif
        )
        {
            // invert branch so JIT assumes the HR is S_OK
            if (SUCCEEDED(hr))
            {
                return;
            }

            ThrowForHr(hr
#if DEBUG || EXTENDED_ERROR_INFORMATION
                    ,
                expression, filepath, memberName, lineNumber
#endif
                    );
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        [MethodImpl(MethodTypes.ThrowHelperMethod)]
        internal static unsafe void ThrowForHr(
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
            //if (hr == DXGI_ERROR_DEVICE_REMOVED)
                //return;

            var nativeMessage = $"Native code threw an exception with HR '0x{hr:X8}', message '{ResolveErrorCode(hr)}'.";


#if DEBUG || EXTENDED_ERROR_INFORMATION
            var additionalInfo = FormatExtendedErrorInformation(
                nativeMessage + (extraInfo is null ? "" : $"Additional info '{extraInfo}"),
                expression,
                memberName,
                lineNumber,
                filepath
            );
#else
            var additionalInfo = "";
#endif

            var inner = ThrowHelper.CreateExternalException(
                hr,
                additionalInfo
            );

            
            Exception ex = hr switch
            {
                E_INVALIDARG => new ArgumentException(inner.Message, inner),
                E_NOINTERFACE => new InvalidCastException(inner.Message, inner),
                E_POINTER => new ArgumentNullException(inner.Message, inner),
                E_FAIL => new InvalidOperationException(inner.Message, inner),
                E_OUTOFMEMORY => new OutOfMemoryException(inner.Message, inner),
                E_NOTIMPL => new NotImplementedException(inner.Message, inner),
                _ => inner,
            };

            throw ex;
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
                FACILITY_DXC => ResolveErrorCodeOrWin32(hr, DxcMessages),
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
            [E_NOINTERFACE] = "E_NOINTERFACE: The requested interface could not be provided by the function",
        };

        private const int DXC_SEVERITY_ERROR = 1;
        private const int FACILITY_DXC = 0xAA;
        private static int DXC_MAKE_HRESULT(int sev, int fac, int code) => MAKE_HRESULT(sev, fac, code);

        // 0x80AA0001 - The operation failed because overlapping semantics were found.
        private static readonly int DXC_E_OVERLAPPING_SEMANTICS = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0001));

        // 0x80AA0002 - The operation failed because multiple depth semantics were found.
        private static readonly int DXC_E_MULTIPLE_DEPTH_SEMANTICS = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0002));

        // 0x80AA0003 - Input file is too large.
        private static readonly int DXC_E_INPUT_FILE_TOO_LARGE = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0003));

        // 0x80AA0004 - Error parsing DXBC container.
        private static readonly int DXC_E_INCORRECT_DXBC = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0004));

        // 0x80AA0005 - Error parsing DXBC bytecode.
        private static readonly int DXC_E_ERROR_PARSING_DXBC_BYTECODE = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0005));

        // 0x80AA0006 - Data is too large.
        private static readonly int DXC_E_DATA_TOO_LARGE = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0006));

        // 0x80AA0007 - Incompatible converter options.
        private static readonly int DXC_E_INCOMPATIBLE_CONVERTER_OPTIONS = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0007));

        // 0x80AA0008 - Irreducible control flow graph.
        private static readonly int DXC_E_IRREDUCIBLE_CFG = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0008));

        // 0x80AA0009 - IR verification error.
        private static readonly int DXC_E_IR_VERIFICATION_FAILED = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0009));

        // 0x80AA000A - Scope-nested control flow recovery failed.
        private static readonly int DXC_E_SCOPE_NESTED_FAILED = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x000A));

        // 0x80AA000B - Operation is not supported.
        private static readonly int DXC_E_NOT_SUPPORTED = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x000B));

        // 0x80AA000C - Unable to encode string.
        private static readonly int DXC_E_STRING_ENCODING_FAILED = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x000C));

        // 0x80AA000D - DXIL container is invalid.
        private static readonly int DXC_E_CONTAINER_INVALID = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x000D));

        // 0x80AA000E - DXIL container is missing the DXIL part.
        private static readonly int DXC_E_CONTAINER_MISSING_DXIL = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x000E));

        // 0x80AA000F - Unable to parse DxilModule metadata.
        private static readonly int DXC_E_INCORRECT_DXIL_METADATA = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x000F));

        // 0x80AA0010 - Error parsing DDI signature.
        private static readonly int DXC_E_INCORRECT_DDI_SIGNATURE = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0010));

        // 0x80AA0011 - Duplicate part exists in dxil container.
        private static readonly int DXC_E_DUPLICATE_PART = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0011));

        // 0x80AA0012 - Error finding part in dxil container.
        private static readonly int DXC_E_MISSING_PART = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0012));

        // 0x80AA0013 - Malformed DXIL Container.
        private static readonly int DXC_E_MALFORMED_CONTAINER = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0013));

        // 0x80AA0014 - Incorrect Root Signature for shader.
        private static readonly int DXC_E_INCORRECT_ROOT_SIGNATURE = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0014));

        // 0X80AA0015 - DXIL container is missing DebugInfo part.
        private static readonly int DXC_E_CONTAINER_MISSING_DEBUG = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0015));

        // 0X80AA0016 - Unexpected failure in macro expansion.
        private static readonly int DXC_E_MACRO_EXPANSION_FAILURE = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0016));

        // 0X80AA0017 - DXIL optimization pass failed.
        private static readonly int DXC_E_OPTIMIZATION_FAILED = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0017));

        // 0X80AA0018 - General internal error.
        private static readonly int DXC_E_GENERAL_INTERNAL_ERROR = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0018));

        // 0X80AA0019 - Abort compilation error.
        private static readonly int DXC_E_ABORT_COMPILATION_ERROR = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x0019));

        // 0X80AA001A - Error in extension mechanism.
        private static readonly int DXC_E_EXTENSION_ERROR = DXC_MAKE_HRESULT(DXC_SEVERITY_ERROR, FACILITY_DXC, (0x001A));

        private static readonly Dictionary<int, string> DxcMessages = new()
        {
            // 0x80AA0001 - The operation failed because overlapping semantics were found
            [DXC_E_OVERLAPPING_SEMANTICS] = "The operation failed because overlapping semantics were found",

            // 0x80AA0002 - The operation failed because multiple depth semantics were found
            [DXC_E_MULTIPLE_DEPTH_SEMANTICS] = "The operation failed because multiple depth semantics were found",

            // 0x80AA0003 - Input file is too large
            [DXC_E_INPUT_FILE_TOO_LARGE] = "Input file is too large",

            // 0x80AA0004 - Error parsing DXBC container
            [DXC_E_INCORRECT_DXBC] = "Error parsing DXBC container",

            // 0x80AA0005 - Error parsing DXBC bytecode
            [DXC_E_ERROR_PARSING_DXBC_BYTECODE] = "Error parsing DXBC bytecode",

            // 0x80AA0006 - Data is too large
            [DXC_E_DATA_TOO_LARGE] = "Data is too large",

            // 0x80AA0007 - Incompatible converter options
            [DXC_E_INCOMPATIBLE_CONVERTER_OPTIONS] = "Incompatible converter options",

            // 0x80AA0008 - Irreducible control flow graph
            [DXC_E_IRREDUCIBLE_CFG] = "Irreducible control flow graph",

            // 0x80AA0009 - IR verification error
            [DXC_E_IR_VERIFICATION_FAILED] = "IR verification error",

            // 0x80AA000A - Scope-nested control flow recovery failed
            [DXC_E_SCOPE_NESTED_FAILED] = "Scope-nested control flow recovery failed",

            // 0x80AA000B - Operation is not supported
            [DXC_E_NOT_SUPPORTED] = "Operation is not supported",

            // 0x80AA000C - Unable to encode string
            [DXC_E_STRING_ENCODING_FAILED] = "Unable to encode string",

            // 0x80AA000D - DXIL container is invalid
            [DXC_E_CONTAINER_INVALID] = "DXIL container is invalid",

            // 0x80AA000E - DXIL container is missing the DXIL part
            [DXC_E_CONTAINER_MISSING_DXIL] = "DXIL container is missing the DXIL part",

            // 0x80AA000F - Unable to parse DxilModule metadata
            [DXC_E_INCORRECT_DXIL_METADATA] = "Unable to parse DxilModule metadata",

            // 0x80AA0010 - Error parsing DDI signature
            [DXC_E_INCORRECT_DDI_SIGNATURE] = "Error parsing DDI signature",

            // 0x80AA0011 - Duplicate part exists in dxil container
            [DXC_E_DUPLICATE_PART] = "Duplicate part exists in dxil container",

            // 0x80AA0012 - Error finding part in dxil container
            [DXC_E_MISSING_PART] = "Error finding part in dxil container",

            // 0x80AA0013 - Malformed DXIL Container
            [DXC_E_MALFORMED_CONTAINER] = "Malformed DXIL Container",

            // 0x80AA0014 - Incorrect Root Signature for shader
            [DXC_E_INCORRECT_ROOT_SIGNATURE] = "Incorrect Root Signature for shader",

            // 0X80AA0015 - DXIL container is missing DebugInfo part
            [DXC_E_CONTAINER_MISSING_DEBUG] = "DXIL container is missing DebugInfo part",

            // 0X80AA0016 - Unexpected failure in macro expansion
            [DXC_E_MACRO_EXPANSION_FAILURE] = "Unexpected failure in macro expansion",

            // 0X80AA0017 - DXIL optimization pass failed
            [DXC_E_OPTIMIZATION_FAILED] = "DXIL optimization pass failed",

            // 0X80AA0018 - General internal error
            [DXC_E_GENERAL_INTERNAL_ERROR] = "General internal error",

            // 0X80AA0019 - Abort compilation error
            [DXC_E_ABORT_COMPILATION_ERROR] = "Abort compilation error",

            // 0X80AA001A - Error in extension mechanism
            [DXC_E_EXTENSION_ERROR] = "Error in extension mechanism",
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
                        FormatExtendedErrorInformation(message, expression, memberName, lineNumber, filepath)
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
#if !DISPOSABLES_ALLOW_FINALIZE
            LogHelper.LogError(
                "OBJECT NOT DISPOSED ERROR\nFile: {0}\nMember: {1}\nLine: {2}\n",
                filepath,
                memberName,
                lineNumber
            );

            Debug.Fail("OBJECT NOT DISPOSED ERROR - see logs");
#endif
        }
    }

    //public class GraphicsException : Exception
    //{
    //}
}
