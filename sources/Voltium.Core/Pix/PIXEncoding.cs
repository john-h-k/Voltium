using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Core;

namespace Voltium.Common.Pix
{
    internal unsafe static class PIXEncoding
    {
        public static readonly bool IsXbox =
#if PIX_XBOX
            true;
#elif PIX_WINDOWS
            false;
#else
            false; // TODO
#endif

        // ReSharper disable twice InconsistentNaming
        public const uint WIN_EVENT_3BLOB_VERSION = 2;
        public const uint D3D12_EVENT_METADATA = WIN_EVENT_3BLOB_VERSION;

        public const ulong EventsReservedRecordSpaceQwords = 64;

        // this is used to make sure SSE string copy always will end 16-byte write in the current block
        // this way only a check if CanWrite(destination, limit) can be performed, instead of CanWrite(destination, limit) - 1
        // since both these are ulong* and SSE writes in 16 byte chunks, 8 bytes are kept in reserve
        // so even if SSE overwrites 8 extra bytes, those will still belong to the correct block
        // on next iteration check destination will be greater than limit
        // this is used as well for fixed size UMD events and EndEvent since these require less space
        // than other variable length user events and do not need big reserved space
        public const ulong EventsReservedTailSpaceQwords = 2;

        public const ulong EventsSafeFastCopySpaceQwords =
            EventsReservedRecordSpaceQwords - EventsReservedTailSpaceQwords;

        public const int EventsGraphicsRecordSpaceQwords = 64;

        //Bits 7-19 (13 bits)
        public const ulong EventsBlockEndMarker = 0x00000000000FFF80;

        //Bits 10-19 (10 bits)
        public const ulong EventsTypeReadMask = 0x00000000000FFC00;
        public const ulong EventsTypeWriteMask = 0x00000000000003FF;
        public const int EventsTypeBitShift = 10;


        //Bits 20-63 (44 bits)
        public const ulong EventsTimestampReadMask = 0xFFFFFFFFFFF00000;
        public const ulong EventsTimestampWriteMask = 0x00000FFFFFFFFFFF;
        public const int EventsTimestampBitShift = 20;

        public static ulong EncodeEventInfo(ulong timestamp, PIXEventType pixEventType)
        {
            return ((timestamp & EventsTimestampWriteMask) << EventsTimestampBitShift) |
                   (((ulong)pixEventType & EventsTypeWriteMask) << EventsTypeBitShift);
        }

        //Bits 60-63 (4)
        public const ulong EventsStringAlignmentWriteMask = 0x000000000000000F;
        public const ulong EventsStringAlignmentReadMask = 0xF000000000000000;
        public const int EventsStringAlignmentBitShift = 60;

        //Bits 55-59 (5)
        public const ulong EventsStringCopyChunkSizeWriteMask = 0x000000000000001F;
        public const ulong EventsStringCopyChunkSizeReadMask = 0x0F80000000000000;
        public const int EventsStringCopyChunkSizeBitShift = 55;

        //Bit 54
        public const ulong EventsStringIsANSIWriteMask = 0x0000000000000001;
        public const ulong EventsStringIsANSIReadMask = 0x0040000000000000;
        public const int EventsStringIsANSIBitShift = 54;

        //Bit 53
        public const ulong EventsStringIsShortcutWriteMask = 0x0000000000000001;
        public const ulong EventsStringIsShortcutReadMask = 0x0020000000000000;
        public const int EventsStringIsShortcutBitShift = 53;

        internal static void WriteFormatString(ref ulong* destination, ulong* limit, ReadOnlySpan<char> obj)
        {
            fixed (char* p = obj)
            {
                CopyEventArgument(ref destination, limit, p);
            }
        }

        private static bool IsStringType<T>()
        {
            return typeof(T) == typeof(string)
                   || typeof(T) == typeof(ReadOnlySpan<char>) /* not yet a thing */
                   || typeof(T) == typeof(Span<char>) /* not yet a thing */
                   || typeof(T) == typeof(ReadOnlyMemory<char>)
                   || typeof(T) == typeof(Memory<char>);
        }

        internal static void Write<T>(ref ulong* destination, ulong* limit, T obj)
        {
            Debug.Assert(obj is object);

            if (CanWrite(destination, limit))
            {
                if (IsStringType<T>())
                {
                    fixed (char* p = &GetDataRef(obj))
                    {
                        CopyEventArgument(ref destination, limit, p);
                    }
                }
                else
                {
                    *destination++ = As64BitChunk(obj);
                }
            }
        }

        private static ref readonly char GetDataRef<T>(T obj)
        {
            Debug.Assert(obj is object);

            if (typeof(T) == typeof(string))
            {
                return ref ((string)(object)obj).GetPinnableReference();
            }

            if (typeof(T) == typeof(Memory<char>))
            {
                return ref ((ReadOnlyMemory<char>)(object)obj).Span.GetPinnableReference();
            }
            if (typeof(T) == typeof(ReadOnlyMemory<char>))
            {
                return ref ((ReadOnlyMemory<char>)(object)obj).Span.GetPinnableReference();
            }

            if (typeof(T) == typeof(Span<char>)) // not currently supported but in case it ever is
            {
                Environment.FailFast("uncomment line below and recompile");
                //return ref ((Span<char>) (object) obj).Span.GetPinnableReference();
            }

            if (typeof(T) == typeof(ReadOnlySpan<char>)) // not currently supported but in case it ever is
            {
                Environment.FailFast("uncomment line below and recompile");
                //return ref ((ReadOnlySpan<char>) (object) obj).GetPinnableReference();
            }

            return ref Unsafe.AsRef<char>(null);
        }

        internal static ulong As64BitChunk<T>(T obj)
        {
            Debug.Assert(obj is object);
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                ThrowHelper.ThrowArgumentException("Managed type that isn't [ReadOnly]Span<char>, [ReadOnly]Memory<char>, or string cannot be written to PIX");
            }

#if !ALLOW_PIX_OVERRUNS
            if (Unsafe.SizeOf<T>() > sizeof(ulong))
            {
                ThrowHelper.ThrowArgumentException("Greater than 8 byte type cannot be written to PIX");
            }
#endif

            // Float must become double as it isn't a supported format specifier
            if (typeof(T) == typeof(float))
            {
                return (ulong)BitConverter.DoubleToInt64Bits((float)(object)obj);
            }

            // Signed types need to be sign extended
            if (typeof(T) == typeof(sbyte))
            {
                return (ulong)(sbyte)(object)obj;
            }

            if (Unsafe.SizeOf<T>() == 1)
            {
                return Unsafe.As<T, byte>(ref obj);
            }

            if (typeof(T) == typeof(short))
            {
                return (ulong)(sbyte)(object)obj;
            }

            if (Unsafe.SizeOf<T>() == 2)
            {
                return Unsafe.As<T, ushort>(ref obj);
            }

            if (typeof(T) == typeof(int))
            {
                return (ulong)(int)(object)obj;
            }

            if (Unsafe.SizeOf<T>() == 4)
            {
                return Unsafe.As<T, uint>(ref obj);
            }

            if (typeof(T) == typeof(long))
            {
                return (ulong)(long)(object)obj;
            }

            if (Unsafe.SizeOf<T>() == 8)
            {
                return Unsafe.As<T, ulong>(ref obj);
            }

#if !FORBID_NON_PRIMITIVES
            ThrowHelper.ThrowArgumentException($"Cannot serialize non primitive type \"{typeof(T)}\"");
#endif

            ulong value = default;
            Unsafe.Write(&value, obj);
            return value;
        }

        public static ulong EncodeStringInfo(ulong alignment, ulong copyChunkSize, bool isANSI, bool isShortcut)
        {
            return ((alignment & EventsStringAlignmentWriteMask) << EventsStringAlignmentBitShift) |
                   ((copyChunkSize & EventsStringCopyChunkSizeWriteMask) << EventsStringCopyChunkSizeBitShift) |
                   (((isANSI ? 1U : 0) & EventsStringIsANSIWriteMask) << EventsStringIsANSIBitShift) |
                   (((isShortcut ? 1U : 0) & EventsStringIsShortcutWriteMask) << EventsStringIsShortcutBitShift);
        }

        public static bool IsAligned(void* pointer, uint alignment)
        {
            return (((ulong)pointer) & (alignment - 1)) == 0;
        }


        public static void CopyEventArgumentSlowest(ref ulong* destination, ulong* limit, char* argument)
        {
            *destination++ = EncodeStringInfo(0, 8, false, false);
            while (CanWrite(destination, limit))
            {
                ulong c = argument[0];
                if (c == default)
                {
                    *destination++ = 0;
                    return;
                }

                ulong x = c;
                c = argument[1];
                if (c == default)
                {
                    *destination++ = x;
                    return;
                }

                x |= c << 16;
                c = argument[2];
                if (c == default)
                {
                    *destination++ = x;
                    return;
                }

                x |= c << 32;
                c = argument[3];
                if (c == default)
                {
                    *destination++ = x;
                    return;
                }

                x |= c << 48;
                *destination++ = x;
                argument += 4;
            }
        }

        public static void CopyEventArgumentSlow(ref ulong* destination, ulong* limit, char* argument)
        {
#if PIX_ENABLE_BLOCK_ARGUMENT_COPY
            if (IsAligned(argument, 8))
            {
                *destination++ = EncodeStringInfo(0, 8, false, false);
                ulong* source = (ulong*)argument;
                while (CanWrite(destination, limit))
                {
                    ulong qword = *source++;
                    *destination++ = qword;
                    //check if any of the characters is a terminating zero
                    //TODO: check if reversed condition is faster
                    if (!((qword & 0xFFFF000000000000) != 0 &&
                          (qword & 0xFFFF00000000) != 0 &&
                          (qword & 0xFFFF0000) != 0 &&
                          (qword & 0xFFFF) != 0))
                    {
                        break;
                    }
                }
            }
            else
#endif // PIX_ENABLE_BLOCK_ARGUMENT_COPY
            {
                CopyEventArgumentSlowest(ref destination, limit, argument);
            }
        }


        public static void CopyEventArgument(ref ulong* destination, ulong* limit, char* argument)
        {
            if (CanWrite(destination, limit))
            {
                if (argument != null)
                {
#if PIX_ENABLE_BLOCK_ARGUMENT_COPY
                    if (IsAligned(argument, 16))
                    {
                        *destination++ = EncodeStringInfo(0, 16, false, false);
                        var zero = Vector128<int>.Zero;
                        if (IsAligned(destination, 16))
                        {
                            while (CanWrite(destination, limit))
                            {
                                var mem = Sse2.LoadAlignedVector128((int*)argument);
                                Sse2.StoreAligned((int*)destination, mem);
                                //check if any of the characters is a terminating zero
                                var res = Sse2.CompareEqual(mem, zero);
                                destination += 2;
                                if (Sse2.MoveMask(res.AsByte()) != 0)
                                {
                                    break;
                                }

                                argument += 8;
                            }
                        }
                        else
                        {
                            while (CanWrite(destination, limit))
                            {
                                var mem = Sse2.LoadVector128((int*)argument);
                                Sse2.Store((int*)destination, mem);
                                //check if any of the characters is a terminating zero
                                var res = Sse2.CompareEqual(mem, zero);
                                destination += 2;
                                if (Sse2.MoveMask(res.AsByte()) != 0)
                                {
                                    break;
                                }

                                argument += 8;
                            }
                        }
                    }
                    else
#endif // PIX_ENABLE_BLOCK_ARGUMENT_COPY
                    {
                        CopyEventArgumentSlow(ref destination, limit, argument);
                    }
                }
                else
                {
                    *destination++ = 0UL;
                }
            }
        }

        private static bool IsVarargs(ulong encodedEvent)
        {
            var type = ((uint)encodedEvent & EventsTypeReadMask) >> EventsTypeBitShift;
            // varargs are odd
            return type % 2 != 0;
        }

        // i am sorry for this code. i should probably move all this stuff up instead of bit twiddling based off the old CPU encoded event
        private static void RewriteEncodedEvent(void* data, PIXEventType noargNoContextType)
        {
            // the arg is the "base" event type, e.g BeginEvent_NoArgs. we add varargs and context if applicable

            // we read the event
            // if it is vararg, we make the param event type vararg (add -1, VarArgsOffset)
            // if we are on xbox, we mask it to make it with HasContext as required
            // we then shift it back into place. we don't call EncodeEvent as we don't have a timestamp so there is just no need
            ulong* pData = (ulong*)data;
#if PIX_XBOX
            const PIXEventType mask = PIXEventType.HasContext;
#else
            const int mask = 0x00;
#endif
            pData[0] = (ulong)((noargNoContextType + (IsVarargs(pData[0]) ? (int)PIXEventType.VarArgsOffset : 0)) | mask) << EventsTypeBitShift;
        }

        private static void AdjustPointerForContext(ref void* data, ref uint size)
        {
#if !PIX_XBOX
            // We need to push forward encodedEvent and color by 8 bytes each to overwrite context
            // (which isn't present on xbox)

            ulong* pData = (ulong*)data;

            var encodedEvent = pData[0];
            var color = pData[1];

            Unsafe.Write(&pData[1], encodedEvent);
            Unsafe.Write(&pData[2], color);

            data = pData + 1;
            size -= 8;
#endif
        }


        private const PIXEventType BeginEvent = PIXEventType.BeginEventNoArgs;
        private const PIXEventType SetMarker = PIXEventType.SetMarkerNoArgs;

        public static void SetGPUMarkerOnContext(
            ID3D12GraphicsCommandList* commandList,
            void* data,
            uint size
        )
        {
            AdjustPointerForContext(ref data, ref size);
            RewriteEncodedEvent(data, SetMarker);

            commandList->SetMarker(D3D12_EVENT_METADATA, data, size);
        }

        public static void SetGPUMarkerOnContext(
            ID3D12CommandQueue* commandQueue,
            void* data,
            uint size
        )
        {
            AdjustPointerForContext(ref data, ref size);
            RewriteEncodedEvent(data, SetMarker);

            commandQueue->SetMarker(D3D12_EVENT_METADATA, data, size);
        }

        public static void BeginGPUEventOnContext(
            ID3D12GraphicsCommandList* commandList,
            void* data,
            uint size
        )
        {
            AdjustPointerForContext(ref data, ref size);
            RewriteEncodedEvent(data, BeginEvent);

            commandList->BeginEvent(D3D12_EVENT_METADATA, data, size);
        }

        public static void BeginGPUEventOnContext(
            ID3D12CommandQueue* commandQueue,
            void* data,
            uint size
        )
        {
            AdjustPointerForContext(ref data, ref size);
            RewriteEncodedEvent(data, BeginEvent);

            commandQueue->BeginEvent(D3D12_EVENT_METADATA, data, size);
        }

        public static void EndGPUEventOnContext(
            ID3D12GraphicsCommandList* commandList
        )
        {
            commandList->EndEvent();
        }

        public static void EndGPUEventOnContext(
            ID3D12CommandQueue* commandQueue
        )
        {
            commandQueue->EndEvent();
        }

        public static bool CanWrite(ulong* destination, ulong* limit)
        {
#if ALLOW_PIX_OVERRUNS || RELEASE
            return destination < limit;
#else
            var cond = destination < limit;
            Debug.Assert(cond || destination == null || limit == null,
                "Tried to write more than 512 bytes of PIX data");
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse <-- shit the fuck up rider have you ever heard of release mode
            return cond;
#endif
        }

        public static void WriteContext(ref ulong* destination, ulong* limit, in ID3D12CommandList* context)
            => WriteContext(ref destination, limit, context);

        public static void WriteContext(ref ulong* destination, ulong* limit, void* context)
        {
            // Context is not used on xbox
            if (CanWrite(destination, limit) && !IsXbox)
            {
                *destination++ = (ulong)context;
            }
        }
    }
}
