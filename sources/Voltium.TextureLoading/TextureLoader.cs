using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.GpuResources;
using Voltium.TextureLoading.DDS;
using static TerraFX.Interop.D3D12_RESOURCE_DIMENSION;

namespace Voltium.TextureLoading
{
    /// <summary>
    /// The type used for loading of texture files
    /// </summary>
    public static unsafe partial class TextureLoader
    {
        /// <summary>
        /// Create a texture from a file
        /// </summary>
        /// <param name="fileName">The file that contains the texture</param>
        /// <param name="type">The type of the texture to create, or <see cref="TexType.RuntimeDetect"/> to automatically detect</param>
        /// <param name="loaderFlags">The flags passed to the texture loader</param>
        /// <param name="maxMipMapSize">The maximum permitted size of a mipmap, or 0 to indicate the maximum size permitted by hardware</param>
        /// <returns>A texture description</returns>
        public static LoadedTexture CreateTexture(
            string fileName,
            TexType type = TexType.RuntimeDetect,
            LoaderFlags loaderFlags = LoaderFlags.None,
            uint maxMipMapSize = 0
        )
        {
            if (fileName is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(fileName));
            }

            using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            return CreateTexture(
                stream,
                type,
                loaderFlags,
                maxMipMapSize
            );
        }

        /// <summary>
        /// Create a texture from a stream
        /// </summary>
        /// <param name="stream">The stream that contains the texture</param>
        /// <param name="type">The type of the texture to create, or <see cref="TexType.RuntimeDetect"/> to automatically detect</param>
        /// <param name="loaderFlags">The flags passed to the texture loader</param>
        /// <param name="maxMipMapSize">The maximum permitted size of a mipmap, or 0 to indicate the maximum size permitted by hardware</param>
        /// <returns>A texture description</returns>
        public static LoadedTexture CreateTexture(
            Stream stream,
            TexType type = TexType.RuntimeDetect,
            LoaderFlags loaderFlags = LoaderFlags.None,
            uint maxMipMapSize = 0
        )
        {
            if (stream is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(stream));
            }

            long streamSize = stream!.Length;
            if (streamSize > int.MaxValue)
            {
                ThrowHelper.ThrowArgumentException($"File too large (only files up to {int.MaxValue} bytes are supported)");
            }

            var data = new byte[streamSize];
            stream.Read(data);
            return CreateTexture(
                data,
                type,
                loaderFlags,
                maxMipMapSize
            );
        }

        /// <summary>
        /// Create a texture from memory
        /// </summary>
        /// <param name="data">The data that contains the texture in memory</param>
        /// <param name="type">The type of the texture to create, or <see cref="TexType.RuntimeDetect"/> to automatically detect</param>
        /// <param name="loaderFlags">The flags passed to the texture loader</param>
        /// <param name="maxMipMapSize">The maximum permitted size of a mipmap, or 0 to indicate the maximum size permitted by hardware</param>
        /// <returns>A texture description</returns>
        public static LoadedTexture CreateTexture(
            Memory<byte> data,
            TexType type = TexType.RuntimeDetect,
            LoaderFlags loaderFlags = LoaderFlags.None,
            uint maxMipMapSize = 0
        )
        {
            if (type == TexType.RuntimeDetect && !TryResolveTexture(data, out type))
            {
                ThrowHelper.ThrowArgumentException("Could not recognise texture format");
            }

            switch (type)
            {
                case TexType.RuntimeDetect:
                    ThrowHelper.NeverReached();
                    break;
                case TexType.DirectDrawSurface:
                    return CreateDdsTexture(data, maxMipMapSize, loaderFlags);
                case TexType.Bitmap:
                    ThrowHelper.Todo();
                    break;
                case TexType.TruevisionGraphicsAdapter:
                    return CreateTgaTexture(data, loaderFlags);
                case TexType.PortableNetworkGraphics:
                    ThrowHelper.Todo();
                    break;
                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(type), type);
                    return default;
            }

            return default;
        }

        /// <summary>
        /// Record an upload copy to the GPU to execute asynchronously
        /// </summary>
        /// <param name="device">The device to create resources on</param>
        /// <param name="cmdList">The command list to record to</param>
        /// <param name="texture">The texture to be uploaded</param>
        /// <param name="textureBuffer">A resource buffer that will contain the uploaded texture</param>
        /// <param name="textureBufferUploadHeap">An intermediate buffer used to copy over the texture</param>
        /// <param name="resourceFlags">Flags used in creation of the <paramref name="textureBuffer"/> resource</param>
        public static void RecordTextureUpload(
            ID3D12Device* device,
            ID3D12GraphicsCommandList* cmdList,
            in LoadedTexture texture,
            out ID3D12Resource* textureBuffer,
            out ID3D12Resource* textureBufferUploadHeap,
            D3D12_RESOURCE_FLAGS resourceFlags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_NONE
        )
        {
            if (device == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(device));
            }

            var textureDescription = texture.Desc;

            DXGI_FORMAT format = texture.LoaderFlags.HasFlag(LoaderFlags.ForceSrgb)
                ? InteropTypeUtilities.MakeSrgb((DXGI_FORMAT)textureDescription.Format)
                : (DXGI_FORMAT)textureDescription.Format;

            textureBuffer = default;
            textureBufferUploadHeap = default;

            ID3D12Fence* fence;

            Guid iid = Windows.IID_ID3D12Fence;
            Guard.ThrowIfFailed(device->CreateFence(0, D3D12_FENCE_FLAGS.D3D12_FENCE_FLAG_NONE, &iid, (void**)&fence));

            fixed (char* pName = "ID3D12Fence")
            {
                Guard.ThrowIfFailed(fence->SetName((ushort*)pName));
            }

            switch (textureDescription.Dimension)
            {
                case TextureDimension.Tex2D:
                {
                    D3D12_RESOURCE_DESC texDesc;
                    texDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
                    texDesc.Alignment = 0;
                    texDesc.Width = textureDescription.Width;
                    texDesc.Height = textureDescription.Height;
                    texDesc.DepthOrArraySize = textureDescription.DepthOrArraySize;
                    texDesc.MipLevels = (ushort)texture.MipCount;
                    texDesc.Format = format;
                    texDesc.SampleDesc.Count = 1;
                    texDesc.SampleDesc.Quality = 0;
                    texDesc.Layout = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_UNKNOWN;
                    texDesc.Flags = resourceFlags;

                    iid = Windows.IID_ID3D12Resource;
                    var defaultHeapProperties = new D3D12_HEAP_PROPERTIES(D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_DEFAULT);

                    {
                        ID3D12Resource* pTexture;
                        Guard.ThrowIfFailed(device->CreateCommittedResource(
                            &defaultHeapProperties,
                            D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
                            &texDesc,
                            D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COMMON,
                            null,
                            &iid,
                            (void**)&pTexture));

                        textureBuffer = pTexture;
                    }

                    uint num2DSubresources = (uint)(texDesc.DepthOrArraySize * texDesc.MipLevels);
                    ulong uploadBufferSize = Windows.GetRequiredIntermediateSize(textureBuffer, 0, num2DSubresources);

                    var uploadHeapProperties = new D3D12_HEAP_PROPERTIES(D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_UPLOAD);
                    var buffer = D3D12_RESOURCE_DESC.Buffer(uploadBufferSize);

                    {
                        ID3D12Resource* pTextureUploadHeap;
                        Guard.ThrowIfFailed(device->CreateCommittedResource(
                            &uploadHeapProperties,
                            D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
                            &buffer,
                            D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_GENERIC_READ,
                            pOptimizedClearValue: null,
                            &iid,
                            (void**)&pTextureUploadHeap));

                        textureBufferUploadHeap = pTextureUploadHeap;
                    }

                    var commonToCopyDest = D3D12_RESOURCE_BARRIER.InitTransition(textureBuffer,
                        D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COMMON,
                        D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COPY_DEST);
                    cmdList->ResourceBarrier(1, &commonToCopyDest);

                    fixed (SubresourceData* pManagedSubresourceData = texture.SubresourceData.Span)
                    fixed (byte* pBitData = texture.Data.Span)
                    {
                        // Convert the ManagedSubresourceData to D3D12_SUBRESOURCE_DATA
                        // Just involves changing the offset (int32, relative to start of data) to an absolute pointer
                        for (int i = 0; i < num2DSubresources; i++)
                        {
                            SubresourceData* p = &pManagedSubresourceData[i];
                            ((D3D12_SUBRESOURCE_DATA*)p)->pData = pBitData + p->DataOffset;
                        }

                        Windows.UpdateSubresources(
                            cmdList,
                            textureBuffer,
                            textureBufferUploadHeap,
                            0,
                            0,
                            num2DSubresources,
                            (D3D12_SUBRESOURCE_DATA*)pManagedSubresourceData
                        );
                    }

                    var copyDestToSrv = D3D12_RESOURCE_BARRIER.InitTransition(textureBuffer,
                        D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COPY_DEST,
                        D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE);

                    cmdList->ResourceBarrier(1, &copyDestToSrv);
                    return;
                }

                default:
                    ThrowHelper.ThrowNotSupportedException("Unsupported dimension");
                    return;
            }
        }

        private const uint DdsMagicWord = 0x20534444; // ASCII for 'DDS'
        private const ushort BmpMagicWord = 0x424D; // ASCII for 'BM'
        private const ulong PngMagicWord = 0x89504E470d0A1A0A; // ASCII for '\x89PNG\r\n\x1a\n'

        private static bool TryResolveTexture(in Memory<byte> data, out TexType texType)
        {
            // wtf.jpg
            // we technically handle really small invalid textures here but they'll just error out later in the pipeline
            // no point early checking as a failed texture load isn't normally something you expect to recover from so
            // perf is not critical
            if (data.Length < sizeof(ulong))
            {
                // Technically bitmap/dds is 2/4 byte magic but obviously it has data after that
                ThrowHelper.ThrowInvalidDataException("Data too small to be any recognised type");
            }

            var span = data.Span;

            if (BinaryPrimitives.ReadUInt16LittleEndian(span) == BmpMagicWord)
            {
                texType = TexType.Bitmap;
                return true;
            }

            if (BinaryPrimitives.ReadUInt32LittleEndian(span) == DdsMagicWord)
            {
                texType = TexType.DirectDrawSurface;
                return true;
            }

            if (BinaryPrimitives.ReadUInt64LittleEndian(span) == PngMagicWord)
            {
                texType = TexType.PortableNetworkGraphics;
                return true;
            }

            if (InspectForValidTgaHeader(span))
            {
                texType = TexType.TruevisionGraphicsAdapter;
                return true;
            }

            Debug.Fail("what the fuck are you passing to this method??????");

            texType = default;
            return false;
        }
    }
}
