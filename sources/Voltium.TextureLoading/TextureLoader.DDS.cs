using System;
using System.IO;
using Voltium.Common;
using Voltium.TextureLoading.DDS;

namespace Voltium.TextureLoading
{
    public unsafe static partial class TextureLoader
    {
        /// <summary>
        /// Create a DDS texture from a file
        /// </summary>
        /// <param name="fileName">The file to create from</param>
        /// <param name="mipMapMaxSize">The largest size a mipmap can be (all larger will be discarded)</param>
        /// <param name="loaderFlags">The flags used by the loader</param>
        /// <returns>A descriptor struct of the DDS texture</returns>
        public static LoadedTexture CreateDdsTexture(
            string fileName,
            uint mipMapMaxSize = default,
            LoaderFlags loaderFlags = LoaderFlags.None
        )
        {
            if (fileName is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(fileName));
            }

            using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            return CreateDdsTexture(stream, mipMapMaxSize, loaderFlags);
        }

        /// <summary>
        /// Create a DDS texture from a strean
        /// </summary>
        /// <param name="stream">The stream to create from</param>
        /// <param name="mipMapMaxSize">The largest size a mipmap can be (all larger will be discarded)</param>
        /// <param name="loaderFlags">The flags used by the loader</param>
        /// <returns>A descriptor struct of the DDS texture</returns>
        public static LoadedTexture CreateDdsTexture(
            Stream stream,
            uint mipMapMaxSize = default,
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
            return CreateDdsTexture(
                data,
                mipMapMaxSize,
                loaderFlags
            );
        }

        /// <summary>
        /// Create a DDS texture from memory
        /// </summary>
        /// <param name="ddsData">The memory where the DDS data is stored </param>
        /// <param name="mipMapMaxSize">The largest size a mipmap can be (all larger will be discarded)</param>
        /// <param name="loaderFlags">The flags used by the loader</param>
        /// <returns>A descriptor struct of the DDS texture</returns>
        public static LoadedTexture CreateDdsTexture(
            ReadOnlyMemory<byte> ddsData,
            uint mipMapMaxSize = default,
            LoaderFlags loaderFlags = LoaderFlags.None
        )
        {
            if (ddsData.Length < sizeof(DDSHeader) + sizeof(uint))
            {
                ThrowHelper.ThrowArgumentException("Data too small to be a valid DDS file");
            }

            var metadata = DDSFileMetadata.FromMemory(ddsData);

            return DDSImplementationFunctions.CreateTextureFromDds12(
                metadata,
                mipMapMaxSize,
                loaderFlags
            );
        }
    }
}
