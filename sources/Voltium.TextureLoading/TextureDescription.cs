using System;
using System.Text;
using System.Threading;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.TextureLoading.DDS;

namespace Voltium.TextureLoading
{
    /// <summary>
    /// Represents a DDS texture that has been loaded into memory and parsed
    /// </summary>
    public readonly struct TextureDescription
    {
        internal TextureDescription(
            Memory<byte> bitData,
            D3D12_RESOURCE_DIMENSION resourceDimension,
            Size3 size,
            uint mipCount,
            uint arraySize,
            DXGI_FORMAT format,
            LoaderFlags loaderFlags,
            bool isCubeMap,
            Memory<ManagedSubresourceData> subresourceData,
            AlphaMode alphaMode,
            TexType underlyingTextureType)
        {
            BitData = bitData;
            ResourceDimension = resourceDimension;
            _size = size;
            MipCount = mipCount;
            ArraySize = arraySize;
            Format = format;
            LoaderFlags = loaderFlags;
            IsCubeMap = isCubeMap;
            SubresourceData = subresourceData;
            AlphaMode = alphaMode;
            UnderlyingTextureType = underlyingTextureType;
        }

        private readonly Size3 _size;

        /// <summary>
        /// The original type of the texture. Note, as texture formats may be normalised by the loader, this may not reflect the type
        /// or format of <see cref="BitData"/>
        /// </summary>
        public TexType UnderlyingTextureType { get; }

        /// <summary>
        /// The buffer that contains the data referenced by <see cref="SubresourceData"/>
        /// </summary>
        public Memory<byte> BitData { get; }

        /// <summary>
        /// The dimension of the DDS data
        /// </summary>
        public D3D12_RESOURCE_DIMENSION ResourceDimension { get; }

        /// <summary>
        /// The height of the texture
        /// </summary>
        public uint Height => _size.Height;

        /// <summary>
        /// The width of the texture
        /// </summary>
        public uint Width  => _size.Width;

        /// <summary>
        /// The depth of the texture
        /// </summary>
        public uint Depth => _size.Depth;

        /// <summary>
        /// The number of MIPs present
        /// </summary>
        public uint MipCount { get; }

        /// <summary>
        /// The size of the texture, if depth is 1
        /// </summary>
        public uint ArraySize { get; }

        /// <summary>
        /// The format of the texture
        /// </summary>
        public DXGI_FORMAT Format { get; }

        /// <summary>
        /// Flags used by the loader
        /// </summary>
        public LoaderFlags LoaderFlags { get; }

        /// <summary>
        /// Whether the texture is a cube map
        /// </summary>
        public bool IsCubeMap { get; }

        /// <summary>
        /// The subresource data, relative to <see cref="BitData"/>, for upload
        /// </summary>
        public Memory<ManagedSubresourceData> SubresourceData { get; }

        /// <summary>
        /// The alpha mode of the texture
        /// </summary>
        public AlphaMode AlphaMode { get; }


        /// <inheritdoc cref="object"/>
        public override string ToString()
        {
            using RentedStringBuilder val = StringHelper.RentStringBuilder();

            val.AppendLine($"UnderlyingTextureType: {UnderlyingTextureType}");
            val.AppendLine($"BitData: {BitData}");
            val.AppendLine($"ResourceDimension: {ResourceDimension}");
            val.AppendLine($"Height: {Height}");
            val.AppendLine($"Width: {Width}");
            val.AppendLine($"Depth: {Depth}");
            val.AppendLine($"MipCount: {MipCount}");
            val.AppendLine($"ArraySize: {ArraySize}");
            val.AppendLine($"Format: {Format}");
            val.AppendLine($"LoaderFlags: {LoaderFlags}");
            val.AppendLine($"IsCubeMap: {IsCubeMap}");
            val.AppendLine($"AlphaMode: {AlphaMode}");

            val.AppendLine("\nSubresource Data: ");
            foreach (ManagedSubresourceData managedSubresourceData in SubresourceData.Span)
            {
                val.Append('\t').AppendLine(managedSubresourceData.ToString().Replace("\n", "\n\t"));
            }

            val.Append('\n');


            return val.ToString();
        }
    }
}
