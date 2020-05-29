using System;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Voltium.Common
{
    internal static class Endianness
    {
        public static uint ReadUInt32LittleEndian(ref uint value)
        {
            if (BitConverter.IsLittleEndian)
            {
                return value;
            }

            return BinaryPrimitives.ReverseEndianness(value);
        }

        public static ushort ReadUInt16LittleEndian(ref ushort value)
        {
            if (BitConverter.IsLittleEndian)
            {
                return value;
            }

            return BinaryPrimitives.ReverseEndianness(value);
        }

        public static uint ReadRgb(ref byte value)
        {
            ref ushort start = ref Unsafe.As<byte, ushort>(ref value);
            ref byte final = ref Unsafe.Add(ref value, 3);

            return start | (uint)(final << 24);
        }
    }
}
