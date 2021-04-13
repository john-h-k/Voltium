
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Voltium.Common;
using Voltium.Core.Contexts;
using Voltium.Core.Memory;
using Voltium.Extensions;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core.Devices
{
    public unsafe sealed class UploadBatch
    {
        private ContextEncoder<Devirt_ArrayBufferWriter<byte>> _writer;

        private Func<int, MemoryAccess, Buffer> _allocateBuffer;
        private Func<TextureDesc, Texture> _allocateTexture;

        public unsafe Buffer UploadBuffer<T>(T[] data) where T : unmanaged => UploadBuffer<T>(data);
        public unsafe Buffer UploadBuffer<T>(ReadOnlySpan<T> data) where T : unmanaged
        {
            var buffer = _allocateBuffer(data.ByteLength(), MemoryAccess.GpuOnly);
            UploadBuffer(data, buffer);
            return buffer;
        }

        public void UploadBuffer<T>(ReadOnlySpan<T> data, in Buffer buffer, uint offset = 0) where T : unmanaged
        {
            var intermediate = _allocateBuffer(data.ByteLength(), MemoryAccess.CpuUpload);
            data.CopyTo(intermediate.AsSpan<T>());

            var command = new CommandBufferCopy
            {
                Source = intermediate.Handle,
                Dest = buffer.Handle,
                DestOffset = offset,
                SourceOffset = 0,
                Length = (uint)data.ByteLength()
            };

            _writer.Emit(&command);
        }
        public void UploadTexture<T>(ReadOnlySpan<T[]> subresources, in Texture texture, uint firstSubresource = 0) where T : unmanaged
        {
            foreach (var mem in subresources)
            {
                UploadTextureSubresource<T>(firstSubresource++, mem, texture);
            }
        }

        public void UploadTexture<T>(ReadOnlySpan<ReadOnlyMemory<T>> subresources, in Texture texture, uint firstSubresource = 0) where T : unmanaged
        {
            foreach (var mem in subresources)
            {
                UploadTextureSubresource(firstSubresource++, mem.Span, texture);
            }
        }

        public void UploadTextureSubresource<T>(uint subresource, ReadOnlySpan<T> data, in Texture texture) where T : unmanaged
        {
            const int requiredTextureAlignment = 256;

            var intermediate = _allocateBuffer(data.ByteLength(), MemoryAccess.CpuUpload);
            var intermediateData = intermediate.AsSpan();
            var dataBytes = MemoryMarshal.AsBytes(data);

            var bpp = (int)texture.Format.BitsPerPixel();

            var width = (int)texture.Width;
            var height = (int)texture.Height;
            var alignedWidth = MathHelpers.AlignUp((int)texture.Width * bpp, requiredTextureAlignment);


            var depthSliceCount = texture.IsArray ? 1u : texture.DepthOrArraySize;

            for (var i = 0; i < depthSliceCount; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    dataBytes.Slice(width * bpp * j, width * bpp).CopyTo(intermediateData.Slice(alignedWidth * j, width));
                }
                dataBytes = dataBytes.Slice(width * height);
            }

            var command = new CommandBufferToTextureCopy
            {
                DestSubresource = subresource,
                Dest = texture.Handle,
                Source = intermediate.Handle,
                HasBox = false,

                SourceFormat = texture.Format,
                SourceWidth = (uint)texture.Width,
                SourceHeight = texture.Height,
                SourceDepth = depthSliceCount,
                SourceOffset = 0,
                SourceRowPitch = (uint)alignedWidth
            };

            _writer.Emit(&command);
        }
    }
}
