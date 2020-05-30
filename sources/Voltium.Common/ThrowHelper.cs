using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Common.Tracing;


namespace Voltium.Common
{
    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        public static void Todo() => throw new NotImplementedException();

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowArgumentException(string paramName, Exception inner) =>
            throw new ArgumentException(paramName, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowResourceLeakException(string message, object resource, object? resourceData = null) =>
            throw new ResourceLeakException(message, resource, resourceData);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowArrayPoolLeakException<T>(string message, T[] resource,
            ArrayPool<T>? resourceData = null) =>
            throw new ArrayPoolLeakException(message, resource, resourceData);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowArrayPoolLeakException<T>(string message, T[] resource,
            ArrayPoolLeakData<T>? resourceData = null) =>
            throw new ArrayPoolLeakException(message, resource, resourceData);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowArgumentException(string paramName, string message) =>
            throw new ArgumentException(paramName, message);

        internal static unsafe void ErrorWithBlob(int hr, ID3DBlob* pError)
        {
            if (pError != null && pError->GetBufferPointer() != null)
            {
                var message = new string((sbyte*)pError->GetBufferSize(), 0, checked((int)pError->GetBufferSize()));
                Guard.ThrowIfFailed(hr, extraInfo: message);
            }
            else
            {
                Guard.ThrowIfFailed(hr);
            }
        }

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowArgumentException(string paramName) => throw new ArgumentException(paramName);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowArgumentNullException(string paramName, Exception inner) =>
            throw new ArgumentNullException(paramName, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowArgumentNullException(string paramName, string message) =>
            throw new ArgumentNullException(paramName, message);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowArgumentNullException(string paramName) => throw new ArgumentNullException(paramName);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowArgumentOutOfRangeException(string paramName, Exception inner) =>
            throw new ArgumentOutOfRangeException(paramName, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowArgumentOutOfRangeException(string paramName, string message) =>
            throw new ArgumentOutOfRangeException(paramName, message);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowArgumentOutOfRangeException(string paramName, object value, string? message = null) =>
            throw new ArgumentOutOfRangeException(paramName, value, message);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowArgumentOutOfRangeException(string paramName) =>
            throw new ArgumentOutOfRangeException(paramName);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowArrayTypeMismatchException(string? message = null, Exception? inner = null) =>
            throw new ArrayTypeMismatchException(message, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowInvalidOperationException(string? message = null, Exception? inner = null) =>
            throw new InvalidOperationException(message, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowDivideByZeroException(string? message = null, Exception? inner = null) =>
            throw new DivideByZeroException(message, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowNotFiniteNumberException(string? message = null, Exception? inner = null) =>
            throw new NotFiniteNumberException(message, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowOverflowException(string? message = null, Exception? inner = null) =>
            throw new OverflowException(message, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowInvalidCastException(string? message = null, Exception? inner = null) =>
            throw new InvalidCastException(message, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowObjectDisposedException(string objectName, Exception inner) =>
            throw new ObjectDisposedException(objectName, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowObjectDisposedException(string objectName, string message) =>
            throw new ObjectDisposedException(objectName, message);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowObjectDisposedException(string objectName) =>
            throw new ObjectDisposedException(objectName);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowRankException(string? message = null, Exception? inner = null) =>
            throw new RankException(message, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowTimeoutException(string? message = null, Exception? inner = null) =>
            throw new TimeoutException(message, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowPlatformNotSupportedException(string? message = null, Exception? inner = null) =>
            throw new PlatformNotSupportedException(message, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowKeyNotFoundException(string? message = null, Exception? inner = null) =>
            throw new KeyNotFoundException(message, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowNotSupportedException(string? message = null, Exception? inner = null) =>
            throw new NotSupportedException(message, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowOutOfMemoryException(string? message = null, Exception? inner = null) =>
            throw new OutOfMemoryException(message, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowInsufficientMemoryException(string? message = null, Exception? inner = null) =>
            throw new InsufficientMemoryException(message, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowWin32Exception(string? message = null, Exception? inner = null) =>
            throw new Win32Exception(message, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowLockRecursionException(string? message = null, Exception? inner = null) =>
            throw new LockRecursionException(message, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowSynchronizationLockException(string? message = null, Exception? inner = null) =>
            throw new SynchronizationLockException(message, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowExternalException(string? message = null, Exception? inner = null) =>
            throw new ExternalException(message, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowExternalException(int hr, string? message = null) =>
            // you will end up here a lot. sorry x
            // enjoy this photo as compensation
            // https://thumbs.dreamstime.com/z/cat-kayaking-sea-kayaker-cap-drifting-red-plastic-boat-151961425.jpg
            throw new ExternalException(message, hr);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowBadImageFormatException(string? message = null, Exception? inner = null) =>
            throw new BadImageFormatException(message, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowNullReferenceException(string? message = null, Exception? inner = null) =>
            throw new NullReferenceException(message, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowIndexOutOfRangeException(string? message = null, Exception? inner = null) =>
            throw new IndexOutOfRangeException(message, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowInvalidDataException(string? message = null, Exception? inner = null) =>
            throw new InvalidDataException(message, inner);

        public static void Throw<T>(T exception) where T : Exception => throw exception;

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowWebException(string? message = null, Exception? inner = null) =>
            throw new WebException(message, inner);

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowJsonException(string? message = null, Exception? inner = null) =>
            throw new JsonException(message, inner);

#pragma warning disable CS8763
        [DebuggerHidden]
        [DoesNotReturn]
        public static void NeverReached()
        {
#if DEBUG
            throw new Exception("this should, never be reached");
#endif
        }
#pragma warning restore CS8763

        [DebuggerHidden]
        [DoesNotReturn]
        public static void ConditionalCompilationPath() => throw null!;


        [DebuggerHidden]
        [DoesNotReturn]
        public static void ThrowNotImplementedException(string? message = null, Exception? inner = null) =>
            throw new NotImplementedException(message, inner);
    }
}
