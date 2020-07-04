using System;
using System.Runtime.Intrinsics.X86;

namespace Voltium.Common.HashHelper
{
    internal unsafe static class ArbitraryHash
    {
        public static int HashBytes(ref byte first, nint length)
            => HashBytes(ref first, (nuint)length);
        public static int HashBytes(ref byte first, nuint length)
        {
            fixed (byte* p = &first)
            {
                return (int)Crc32CElseHashCodeCombine(p, length);
            }
        }

        public static int HashBytes(byte* first, nuint length)
        {
            return (int)Crc32CElseHashCodeCombine(first, length);
        }

        // TODO: PERF make this do 3 blocks 8 bytes at once for better perf
        // TODO: PERF use HashCode.Add not Combine
        private static uint Crc32CElseHashCodeCombine(byte* first, nuint length)
        {
            uint value = ~0u;

            var leadingBytes = MathHelpers.AlignUp(first, 8);

            // handle leading bytes until 8 byte aligned
            while (first < leadingBytes)
            {
                value = Hash(value, *first++);
                length--;
            }

            ulong* pLong = (ulong*)first;
            byte* last = first + length;
            byte* alignedLast = first + MathHelpers.AlignDown(length, 8);

            // handle main in 8 byte chunks until we can't anymore
            ulong tmpValue = value;
            while (pLong < alignedLast)
            {
                tmpValue = Hash8(tmpValue, *pLong++);
            }
            value = (uint)tmpValue;

            // handle trailing
            first = (byte*)pLong;
            while (first < last)
            {
                value = Hash(value, *first++);
            }

            return value;

            static uint Hash(uint val, byte other)
            {
                if (Sse.IsSupported)
                {
                    return Sse42.Crc32(val, other);
                }
                else
                {
                    return (uint)HashCode.Combine(val, other);
                }
            }

            static ulong Hash8(ulong val, ulong other)
            {
                if (Sse.X64.IsSupported)
                {
                    return Sse42.X64.Crc32(val, other);
                }
                else if (Sse.IsSupported)
                {
                    val = Sse42.Crc32((uint)val, (uint)other);
                    return Sse42.Crc32((uint)val, (uint)(other >> 32));
                }
                else
                {
                    return (ulong)HashCode.Combine(val, other);
                }
            }
        }
    }
}
