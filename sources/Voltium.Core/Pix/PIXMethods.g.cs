using System;
using System.Diagnostics;
using TerraFX.Interop;
using Voltium.Core;
using static Voltium.Common.Pix.PIXEncoding;

namespace Voltium.Common.Pix
{
    // the public API. supports 0->16 additional format values, generic'd to avoid boxing

#pragma warning disable
    public unsafe static partial class PIXMethods
    {
        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent(
            in Argb32 color,
            ReadOnlySpan<char> formatString
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(0, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(1, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(2, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(3, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(4, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(5, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(6, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(7, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(8, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(9, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(10, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(11, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(12, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(13, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(14, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(15, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14,
            T15 t15
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(16, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);
            Write(ref destination, limit, t15);

            *destination = 0UL;

            PIXEvents.BeginEvent(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(0, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(1, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(2, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(3, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(4, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(5, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(6, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(7, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(8, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(9, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(10, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(11, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(12, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(13, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(14, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(15, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14,
            T15 t15
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(16, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);
            Write(ref destination, limit, t15);

            *destination = 0UL;

            PIXEvents.BeginEvent(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent(
            in CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(0, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0>(
            in CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(1, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(buffer - destination), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1>(
            in CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(2, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2>(
            in CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(3, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3>(
            in CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(4, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4>(
            in CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(5, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5>(
            in CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(6, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6>(
            in CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(7, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7>(
            in CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(8, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
            in CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(9, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            in CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(10, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            in CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(11, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            in CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(12, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            in CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(13, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            in CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(14, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            in CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(15, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
            in CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14,
            T15 t15
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(16, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);
            Write(ref destination, limit, t15);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent(
            ComputeContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(0, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0>(
            in ComputeContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(1, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(buffer - destination), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1>(
            in ComputeContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(2, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2>(
            in ComputeContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(3, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3>(
            in ComputeContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(4, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4>(
            in ComputeContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(5, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5>(
            in ComputeContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(6, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6>(
            in ComputeContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(7, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7>(
            in ComputeContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(8, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
            in ComputeContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(9, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            in ComputeContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(10, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            in ComputeContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(11, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            in ComputeContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(12, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            in ComputeContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(13, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            in ComputeContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(14, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            in ComputeContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(15, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
            in ComputeContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14,
            T15 t15
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(16, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);
            Write(ref destination, limit, t15);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent(
            in GraphicsContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(0, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0>(
            in GraphicsContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(1, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(buffer - destination), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1>(
            in GraphicsContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(2, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2>(
            in GraphicsContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(3, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3>(
            in GraphicsContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(4, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4>(
            in GraphicsContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(5, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5>(
            in GraphicsContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(6, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6>(
            in GraphicsContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(7, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7>(
            in GraphicsContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(8, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
            in GraphicsContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(9, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            in GraphicsContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(10, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            in GraphicsContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(11, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            in GraphicsContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(12, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            in GraphicsContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(13, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            in GraphicsContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(14, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            in GraphicsContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(15, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void BeginEvent<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
            in GraphicsContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14,
            T15 t15
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.BeginEvent(16, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);
            Write(ref destination, limit, t15);

            *destination = 0UL;

            PIXEvents.BeginEvent(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker(
            in Argb32 color,
            ReadOnlySpan<char> formatString
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(0, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(1, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(2, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(3, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(4, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(5, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(6, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(7, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(8, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(9, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(10, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(11, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(12, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(13, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(14, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(15, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14,
            T15 t15
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(16, false)));
            Write(ref destination, limit, colorVal);

            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);
            Write(ref destination, limit, t15);

            *destination = 0UL;

            PIXEvents.SetMarker(buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(0, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(1, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(2, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(3, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(4, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(5, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(6, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(7, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(8, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(9, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(10, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(11, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(12, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(13, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(14, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(15, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
            ID3D12CommandQueue* context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14,
            T15 t15
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(16, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context);
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);
            Write(ref destination, limit, t15);

            *destination = 0UL;

            PIXEvents.SetMarker(context, buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker(
            CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(0, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            *destination = 0UL;

            PIXEvents.SetMarker(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0>(
            CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(1, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);

            *destination = 0UL;

            PIXEvents.SetMarker(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1>(
            CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(2, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);

            *destination = 0UL;

            PIXEvents.SetMarker(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2>(
            CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(3, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);

            *destination = 0UL;

            PIXEvents.SetMarker(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3>(
            CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(4, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);

            *destination = 0UL;

            PIXEvents.SetMarker(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4>(
            CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(5, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);

            *destination = 0UL;

            PIXEvents.SetMarker(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5>(
            CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(6, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);

            *destination = 0UL;

            PIXEvents.SetMarker(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6>(
            CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(7, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);

            *destination = 0UL;

            PIXEvents.SetMarker(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7>(
            CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(8, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);

            *destination = 0UL;

            PIXEvents.SetMarker(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
            CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(9, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);

            *destination = 0UL;

            PIXEvents.SetMarker(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(10, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);

            *destination = 0UL;

            PIXEvents.SetMarker(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(11, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);

            *destination = 0UL;

            PIXEvents.SetMarker(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(12, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);

            *destination = 0UL;

            PIXEvents.SetMarker(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(13, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);

            *destination = 0UL;

            PIXEvents.SetMarker(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(14, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);

            *destination = 0UL;

            PIXEvents.SetMarker(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(15, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);

            *destination = 0UL;

            PIXEvents.SetMarker(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void SetMarker<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
            CopyContext context,
            in Argb32 color,
            ReadOnlySpan<char> formatString,
            T0 t0,
            T1 t1,
            T2 t2,
            T3 t3,
            T4 t4,
            T5 t5,
            T6 t6,
            T7 t7,
            T8 t8,
            T9 t9,
            T10 t10,
            T11 t11,
            T12 t12,
            T13 t13,
            T14 t14,
            T15 t15
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;
            var colorVal = Argb32.GetAs32BitArgb(color);

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.SetMarker(16, true)));
            Write(ref destination, limit, colorVal);
            WriteContext(ref destination, limit, context.GetListPointer());
            WriteFormatString(ref destination, limit, formatString);
            Write(ref destination, limit, t0);
            Write(ref destination, limit, t1);
            Write(ref destination, limit, t2);
            Write(ref destination, limit, t3);
            Write(ref destination, limit, t4);
            Write(ref destination, limit, t5);
            Write(ref destination, limit, t6);
            Write(ref destination, limit, t7);
            Write(ref destination, limit, t8);
            Write(ref destination, limit, t9);
            Write(ref destination, limit, t10);
            Write(ref destination, limit, t11);
            Write(ref destination, limit, t12);
            Write(ref destination, limit, t13);
            Write(ref destination, limit, t14);
            Write(ref destination, limit, t15);

            *destination = 0UL;

            PIXEvents.SetMarker(context.GetListPointer(), buffer, (uint)(destination - buffer), thread, time);
        }


        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void EndEvent()
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.EndEvent(0, true)));
            *destination = 0UL;

            PIXEvents.EndEvent(buffer, 1, thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void EndEvent(
            ID3D12CommandQueue* context
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.EndEvent(0, true)));
            WriteContext(ref destination, limit, context);
            *destination = 0UL;

            PIXEvents.EndEvent(context, buffer, 2, thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void EndEvent(
            in CopyContext context
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.EndEvent(0, true)));
            WriteContext(ref destination, limit, context.GetListPointer());
            *destination = 0UL;

            PIXEvents.EndEvent(context.GetListPointer(), buffer, 2, thread, time);
        }


        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void EndEvent(
            in ComputeContext context
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.EndEvent(0, true)));
            WriteContext(ref destination, limit, context.GetListPointer());
            *destination = 0UL;

            PIXEvents.EndEvent(context.GetListPointer(), buffer, 2, thread, time);
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void EndEvent(
            in GraphicsContext context
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.EndEvent(0, true)));
            WriteContext(ref destination, limit, context.GetListPointer());
            *destination = 0UL;

            PIXEvents.EndEvent(context.GetListPointer(), buffer, 2, thread, time);
        }

        // used by scoped event
        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        internal static void EndEvent(
            ID3D12GraphicsCommandList* context
        )
        {
            var thread = PIXEvents.RetrieveTimeData(out ulong time);

            ulong* buffer = stackalloc ulong[EventsGraphicsRecordSpaceQwords];
            ulong* destination = buffer;
            ulong* limit = (buffer + EventsGraphicsRecordSpaceQwords) - EventsReservedTailSpaceQwords;

            Write(ref destination, limit, EncodeEventInfo(time, EventTypeInferer.EndEvent(0, true)));
            WriteContext(ref destination, limit, context);
            *destination = 0UL;

            PIXEvents.EndEvent(context, buffer, 2, thread, time);
        }
    }
}
