using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.TextureLoading.DDS;
using Voltium.TextureLoading.TGA;
using static TerraFX.Interop.DXGI_FORMAT;

namespace Voltium.TextureLoading
{
    public static unsafe partial class TextureLoader
    {
        /// <summary>
        /// Create a TGA texture from a file
        /// </summary>
        /// <param name="fileName">The file to create from</param>
        /// <param name="loaderFlags">The flags used by the loader</param>
        /// <returns>A descriptor struct of the texture</returns>
        public static TextureDescription CreateTgaTexture(
            string fileName,
            LoaderFlags loaderFlags = LoaderFlags.None
        )
        {
            if (fileName is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(fileName));
            }

            using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            return CreateTgaTexture(
                stream, loaderFlags
            );
        }

        /// <summary>
        /// Create a TGA texture from a strean
        /// </summary>
        /// <param name="stream">The stream to create from</param>
        /// <param name="loaderFlags">The flags used by the loader</param>
        /// <returns>A descriptor struct of the texture</returns>
        public static TextureDescription CreateTgaTexture(
            Stream stream,
            LoaderFlags loaderFlags = LoaderFlags.None
        )
        {
            if (stream is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(stream));
            }

            long streamSize = stream!.Length;
            if (streamSize > int.MaxValue)
            {
                ThrowHelper.ThrowArgumentException("File too large");
            }

            var data = new byte[streamSize];
            stream.Read(data);
            return CreateTgaTexture(
                data,
                loaderFlags
            );
        }

        /// <summary>
        /// Create a TGA texture from memory
        /// </summary>
        /// <param name="tgaData">The memory where the TGA data is stored </param>
        /// <param name="loaderFlags">The flags used by the loader</param>
        /// <returns>A descriptor struct of the texture</returns>
        public static TextureDescription CreateTgaTexture(
            Memory<byte> tgaData,
            LoaderFlags loaderFlags = LoaderFlags.None
        )
        {
            if (tgaData.Length < sizeof(TGAHeader))
            {
                ThrowHelper.ThrowArgumentException("Data too small to be a valid DDS file");
            }

            return TGAImplementationFunctions.CreateTgaTexture(
                tgaData,
                loaderFlags
            );
        }

        private static bool InspectForValidTgaHeader(in Span<byte> span)
        {
            if (span.Length < 18)
            {
                return false;
            }

            TGAHeader header = MemoryMarshal.Read<TGAHeader>(span);
            if (!TGAHeaderExtensions.IsValidTypeCode(header) || !TGAHeaderExtensions.IsValidBitsPerPixel(header))
            {
                return false;
            }

            //Default.ZLogInformation("we are not very certain this is a TGA file. it just isn't valid bmp/png/dds");

            return true;
        }
    }
}
