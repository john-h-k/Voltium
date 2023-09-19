using System;
using System.Diagnostics;
using TerraFX.Interop.Windows;

/*
 * K, quick debrief on how PIX works cuz y'all gonna need to know to understand this codebase.
 *
 * I am certainly going to be wrong at least somwhere here. Please correct if you realise I am
 *
 * First and foremost, perf is absolutely critical here. Don't get clever and start optimising the generics
 * with params object[]
 *
 * PIX has 2 types of events/markers
 * - CPU
 * - CPU and GPU
 *
 * The CPU args on Windows (not XBOX; see below) contain an additional value 'context', which is either a command list or command queue pointer.
 *
 * For performance reasons, PIX doesn't serialize format strings immediately, and writes them and all provided
 * arguments to memory for serialization later
 *
 * PIX serialized data supports any data type that is either 'wchar_t*', 'char*' (note: we don't support this [reasoning: i am lazy]), or a value that is less than or
 * equal to the size of a ulong (8 bytes). Anything more will be truncated/generally not work. Doesn't matter tho
 * as if its a custom type it won't have a accepted format specifier
 *
 * PIX stores your data in 8 byte chunks. So 2 vararg ints are promoted to longs and take up 16 bytes, not 8
 * Floats require special casing as there is not single precision format specifier, so they are promoted to double
 * Signed types are also specially recognised to properly sign extend, whereas other types are simply written directly
 * to the 8 byte area. This means every data type (except individual string chars) is guaranteed to be 8 byte aligned too.
 *
 * The basic layout of a PIX data string is:
 *    - EventEncoding | Color | [Optional: Only present on CPU] Context | Format String | [optional varargs...]
 *
 *    - The EventEncoding, Color, and Format String are mandatory. I'm not sure how it behaves with a null format string. Should probably find out
 *    - Every thing except the format string and any varargs which are string (char*, wchar_t*) are 8 bytes
 *    - Strings have an 8 byte header that defines the alignment, what size chunks it was copied in (so it can trim excess data), whether it is ansi,
 *      and a isShortcut bool that is unused currently. They are null terminated with a 0
 *    - String copying is done in chunks if PIX_ENABLE_BLOCK_ARGUMENT_COPY is defined, but this can (safely) over-read data on windows. Need to determine if this is ok in managed code
 *      (e.g it can trigger AddressSanitizer from native code)
 *    - Color is a 4 byte ARGB type, see PIXColor
 *    - EventEncoding is a set of flags, see Constants.EncodeEventInfo
 *
 * Because context is thrown slap bang in the middle of the args, we need to basically separate the first 16 bytes from the rest (format string and args).
 * Then we can  insert the context if necessary
 * We allocate enough for the context, and when copying CPU event args all is good.
 * Then for GPU, we skip sizeof(ulong) and rewrite the first 16 bytes. The format strings and args are still in the same place.
 *
 * This rewrite isn't just as simple as shoving everything forward 8 bytes and removing 8 bytes from the size. We need to re-encode the event too
 *
 * We rewrite in PIXEncoding.RewriteEncoding, and PIXEncoding.IsVarargs
 * - We strip out the timestamp as it is only relevant to the CPU
 * - We check if the event has varargs, and if so, make the new one have varargs too
 * - We add context flag if applicable (on XBOX)
 *
 * This is more efficient than rewriting the format strings and args to be earlier, as these are likely to be >16 bytes.
 *
 * XBOX implicitly records the context when the actual marker/event command is called on the command queue/list, so it isn't stored on the CPU
 * This means the CPU/GPU data buffers are completely homogeneous on XBOX (yay!)
 *
 * We use this ugly generic mess for perf. To prevent it infecting the codebase, we serialize the args as early as possible to a 512 byte (64 ulong) local buffer. >512 bytes of combined data
 * (EventEncoding, Color, Context, Format String, and varargs combined) isn't supported and is simply truncated to 512 bytes.
 */

namespace Voltium.Common.Pix
{
#pragma warning disable
    public unsafe static partial class PIXMethods
    {

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void ReportCounter(ReadOnlySpan<char> name, float value)
        {
            fixed (char* p = name)
            {
                NativeMethods.PIXReportCounter(p, value);
            }
        }

        [Conditional("DEBUG")]
        [Conditional("USE_PIX")]
        public static void NotifyWakeFromFenceSignal(IntPtr @event)
        {
            NativeMethods.PIXNotifyWakeFromFenceSignal((HANDLE)@event);
        }
    }
}
