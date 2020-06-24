using System;
using System.Buffers.Binary;
using System.Linq.Expressions;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core;
using Voltium.Core.GpuResources;
using Voltium.TextureLoading.DDS;
using static TerraFX.Interop.DXGI_FORMAT;

// life savers:
// - http://www.opennet.ru/docs/formats/targa.pdf
// - http://paulbourke.net/dataformats/tga/

namespace Voltium.TextureLoading.TGA
{
    internal static unsafe class TGAImplementationFunctions
    {
        public static LoadedTexture CreateTgaTexture(Memory<byte> tgaData,
            LoaderFlags loaderFlags = LoaderFlags.None)
        {
            if (tgaData.Length < sizeof(TGAHeader))
            {
                ThrowHelper.ThrowInvalidDataException("Too small");
            }

            Span<byte> span = tgaData.Span;

            TGAHeader header = MemoryMarshal.Read<TGAHeader>(span);

            if (IsCompressed(header.DataTypeCode))
            {
                ThrowHelper.ThrowNotSupportedException("Compressed TGA textures are TODO"); // TODO
            }

            int size = header.Height * header.Width * (header.BitsPerPixel / 8);

            var data = new byte[size];
            var buff = data;

            DXGI_FORMAT format = loaderFlags.HasFlag(LoaderFlags.ForceSrgb)
                ? DXGI_FORMAT_R8G8B8A8_UNORM_SRGB
                : DXGI_FORMAT_R8G8B8A8_UNORM;

            switch (header.BitsPerPixel)
            {
                case 24:
                    RbgToRgba(span.Slice(sizeof(TGAHeader)), buff);
                    break;
                case 32:
                    ArbgToRgba(span.Slice(sizeof(TGAHeader)), buff);
                    break;
                case 16:
                    Rgba16ToRgba(span.Slice(sizeof(TGAHeader)), buff);
                    break;
                default:
                    ThrowHelper.ThrowNotSupportedException("Unsupported format");
                    break;
            }

            var subresources = new SubresourceData[1];
            subresources[0] = new SubresourceData();

            var desc = new TextureDesc
            {
                Width = (uint)header.Width,
                Height = (uint)header.Height,
                DepthOrArraySize = 0, // is this right?
                Format = (DataFormat)format,
                Dimension = TextureDimension.Tex2D
            };

            return new LoadedTexture(
                data,
                desc,
                1,
                loaderFlags,
                false,
                subresources,
                header.BitsPerPixel == 24 ? AlphaMode.Opaque : AlphaMode.Unknown,
                TexType.Tga
            );
        }

        // TODO make span not ref based

        private static void Rgba16ToRgba(ReadOnlySpan<byte> rgba16, Span<byte> rgba)
        {
            ref ushort start = ref Unsafe.As<byte, ushort>(ref Unsafe.AsRef(in rgba[0]));
            for (int i = 0; i < rgba16.Length / 2; i += 1)
            {
                Unsafe.As<byte, uint>(ref rgba[i * 4]) = Rgba16ToRgba(Endianness.ReadUInt16LittleEndian(ref Unsafe.Add(ref start, i)));

            }

            static uint Rgba16ToRgba(ushort rgb16)
            {
                const uint mask = (1 << 5) - 1;

                const int blueShift = 0;
                const int greenShift = 5;
                const int redShift = 10;
                const int alphaShift = 15;

                uint a = (rgb16 >> alphaShift) == 1 ? 0xFFU : 0;
                uint r = (uint)((rgb16 >> redShift) & mask) * 8U;
                uint g = (uint)((rgb16 >> greenShift) & mask) * 8U;
                uint b = (uint)((rgb16 >> blueShift) & mask) * 8U;

                return (r << 24) | (g << 16) | (b << 8) | a;
            }
        }

        private static bool IsCompressed(TgaTypeCode code) => code > TgaTypeCode.BlackAndWhite;

        // #overengineering
        private static void ArbgToRgba(ReadOnlySpan<byte> argb, Span<byte> rgba)
        {
            ref uint start = ref Unsafe.As<byte, uint>(ref Unsafe.AsRef(in argb[0]));
            for (int i = 0; i < rgba.Length / 4; i += 1)
            {
                Unsafe.As<byte, uint>(ref rgba[i * 4]) = ArbgToRgba(Unsafe.Add(ref start, i));
            }

            // just move the alpha from start to back
            static uint ArbgToRgba(uint arbg) => BitOperations.RotateLeft(arbg, 8);
        }

        private static void RbgToRgba(ReadOnlySpan<byte> rgb, Span<byte> rgba)
        {
            ref byte start = ref Unsafe.AsRef(in rgb[0]);
            for (int i = 0, j = 0; i < rgba.Length; i += 1, j += 3)
            {
                Unsafe.As<byte, uint>(ref rgba[i * 4]) = RbgToRgba(Endianness.ReadRgb(ref Unsafe.Add(ref start, j)));
            }

            // read gb, or with r, or with 0xFF as default alpha
            static uint RbgToRgba(uint rgb) => rgb | (0xFFU << 24);
        }
    }
}
