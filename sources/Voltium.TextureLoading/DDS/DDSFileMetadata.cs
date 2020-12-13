using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Voltium.Common;

namespace Voltium.TextureLoading.DDS
{
    internal readonly unsafe ref struct DDSFileMetadata
    {
        public static DDSFileMetadata FromMemory(in ReadOnlyMemory<byte> ddsData)
        {
            Debug.Assert(!ddsData.IsEmpty);
            ReadOnlySpan<byte> span = ddsData.Span;
            ref readonly byte dataStart = ref MemoryMarshal.GetReference(span);

            uint magicNum = BinaryPrimitives.ReadUInt32LittleEndian(span);

            if (magicNum != 0x20534444 /* "DDS " */)
            {
                ThrowHelper.ThrowArgumentException($"File not a valid DDS file - expected magic number {0x20534444} but got {magicNum}");
            }

            ref DDSHeader header = ref Unsafe.As<byte, DDSHeader>(ref Unsafe.Add(ref Unsafe.AsRef(in dataStart), sizeof(uint)));

            bool hasDxt10Header = false;
            if (header.DdsPixelFormat.Flags.HasFlag(PixelFormatFlags.DDS_FOURCC)
                && InteropTypeUtilities.MakeFourCC('D', 'X', '1', '0') == header.DdsPixelFormat.FourCC)
            {
                if (ddsData.Length < sizeof(DDSHeader) + sizeof(uint) + sizeof(DDSHeaderDxt10))
                {
                    ThrowHelper.ThrowArgumentException("File too small to be a valid DDS file");
                }

                hasDxt10Header = true;
            }

            int offset = sizeof(uint) + sizeof(DDSHeader) + (hasDxt10Header ? sizeof(DDSHeaderDxt10) : 0);

            return new DDSFileMetadata(ref header, ddsData[offset..]);
        }

        private DDSFileMetadata(ref DDSHeader ddsHeader, ReadOnlyMemory<byte> bitData)
        {
            _ddsHeader = MemoryMarshal.CreateReadOnlySpan(ref ddsHeader, 1);
            BitData = bitData;
        }

        private readonly ReadOnlySpan<DDSHeader> _ddsHeader;
        public ref readonly DDSHeader DdsHeader => ref _ddsHeader[0];

        public readonly ReadOnlyMemory<byte> BitData;
    }
}
