using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using Voltium.Common;

namespace Voltium.Common.Pix
{
    internal unsafe static class NativeMethods
    {
        /*
         *
         * These are the real exports I get from WinPixEventRuntime.dll with DUMPBIN /EXPORT
         *
         * 4    0 00003AA0 PIXBeginCapture1
         * 1    1 00003B70 PIXBeginEventOnCommandList <stable>
         * 5    2 00003AA0 PIXEndCapture
         * 2    3 00003AC0 PIXEndEventOnCommandList <stable>
         * 6    4 00003A00 PIXEventsReplaceBlock
         * 7    5 00003AB0 PIXGetCaptureState
         * 8    6 00002200 PIXGetThreadInfo
         * 9    7 00003A80 PIXNotifyWakeFromFenceSignal
         * 10   8 00003A60 PIXReportCounter
         * 3    9 00003CE0 PIXSetMarkerOnCommandList <stable>
         *
         *
         * These are the only documented stable exports tho
         * - PIXBeginEventOnCommandList
         * - PIXEndEventOnCommandList
         * - PIXSetMarkerOnCommandList
         */


        private const string LibraryName = @"WinPixEventRuntime.dll";

        // TODO save to prevent transcoding costs

        private static int Utf8Length(ReadOnlySpan<char> s) => Encoding.UTF8.GetMaxByteCount(s.Length);

        /// <summary>
        ///
        /// </summary>
        /// <param name="commandList"></param>
        /// <param name="color"></param>
        /// <param name="message"></param>
        public static void BeginEventOnCommandList(
            ID3D12GraphicsCommandList* commandList,
            Argb32 color,
            ReadOnlySpan<char> message
        )
        {
            var length = Utf8Length(message);
            var buff = StackSentinel.SafeToStackalloc<byte>(length) ? stackalloc byte[length] : new byte[length];
            Encoding.UTF8.GetBytes(message, buff);

            var hr = _PIXBeginEventOnCommandList(commandList, Argb32.GetAs32BitArgb(color),
                (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buff)));

            Guard.ThrowIfFailed(hr);
        }

        public static void EndEventOnCommandList(ID3D12GraphicsCommandList* commandList)
        {
            var hr = _PIXEndEventOnCommandList(commandList);

            Guard.ThrowIfFailed(hr);
        }


        public static void SetMarkerOnCommandList(
            ID3D12GraphicsCommandList* commandList,
            Argb32 color,
            ReadOnlySpan<char> message
        )
        {
            var length = Utf8Length(message);
            var buff = StackSentinel.SafeToStackalloc<byte>(length) ? stackalloc byte[length] : GC.AllocateArray<byte>(length, pinned: true);
            Encoding.UTF8.GetBytes(message, buff);

            var hr = _PIXSetMarkerOnCommandList(
                commandList,
                Argb32.GetAs32BitArgb(color),
                (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buff))
            );

            Guard.ThrowIfFailed(hr);
        }

        [DllImport(LibraryName, EntryPoint = "PIXBeginEventOnCommandList")]
        private static extern HRESULT _PIXBeginEventOnCommandList(
            ID3D12GraphicsCommandList* commandList,
            ulong color,
            byte* formatString
        );

        [DllImport(LibraryName, EntryPoint = "PIXSetMarkerOnCommandList")]
        private static extern HRESULT _PIXSetMarkerOnCommandList(
            ID3D12GraphicsCommandList* commandList,
            ulong color,
            byte* formatString
        );

        [DllImport(LibraryName, EntryPoint = "PIXEndEventOnCommandList")]
        private static extern HRESULT _PIXEndEventOnCommandList(ID3D12GraphicsCommandList* commandList);

        [DllImport(LibraryName, EntryPoint = "PIXEventsReplaceBlock")]
        private static extern ulong _PIXEventsReplaceBlock(
            PIXEventsThreadInfo* threadInfo,
            bool getEarliestTime
        );

        public static ulong PIXEventsReplaceBlock(
            PIXEventsThreadInfo* threadInfo,
            bool getEarliestTime
        )
        {
            return _PIXEventsReplaceBlock(threadInfo, getEarliestTime);
        }


        [DllImport(LibraryName, EntryPoint = "PIXGetThreadInfo")]
        private static extern PIXEventsThreadInfo* _PIXGetThreadInfo();

        public static PIXEventsThreadInfo* PIXGetThreadInfo()
        {
            Debug.Assert(sizeof(nuint) == 8);
            var info = _PIXGetThreadInfo();
            Debug.Assert(info != null, "uh oh");
            return info;
        }

        // Notifies PIX that an event handle was set as a result of a D3D12 fence being signaled.
        // The event specified must have the same handle value as the handle
        // used in ID3D12Fence.SetEventOnCompletion.
        [DllImport(LibraryName, EntryPoint = "PIXNotifyWakeFromFenceSignal")]
        private static extern void _PIXNotifyWakeFromFenceSignal(HANDLE @event);

        internal static void PIXNotifyWakeFromFenceSignal(HANDLE @event) => _PIXNotifyWakeFromFenceSignal(@event);

        internal static ulong PIXGetTimestampCounter()
        {
            LARGE_INTEGER time;
            _ = Windows.QueryPerformanceCounter(&time);
            return (ulong)time.QuadPart;
        }

        [DllImport(LibraryName, EntryPoint = "PIXReportCounter")]
        private static extern void _PIXReportCounter(char* name, float value);

        internal static void PIXReportCounter(char* name, float value)
        {
            _PIXReportCounter(name, value);
        }
    }
}
