using System;
using System.Collections.Generic;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Memory;
using Voltium.Core.Pool;
using Voltium.TextureLoading;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core.Contexts
{
    /// <summary>
    /// Represents a context on which GPU commands can be recorded
    /// </summary>
    public unsafe class UploadContext : CopyContext
    {
        private List<Buffer> _listBuffers;
        private DynamicAllocator _transientAllocator;
        private const int MaxNumNonListBuffers = 8;

        internal UploadContext(in ContextParams @params) : base(@params)
        {
            _listBuffers = new();
            _transientAllocator = new DynamicAllocator(Device, MemoryAccess.CpuUpload);
        }

        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="destination"></param>
        public void UploadBufferToPreexisting<T>(T[] buffer, in Buffer destination) where T : unmanaged
            => UploadBufferToPreexisting((ReadOnlySpan<T>)buffer, destination);


        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="destination"></param>
        public void UploadBufferToPreexisting<T>(Span<T> buffer, in Buffer destination) where T : unmanaged
            => UploadBufferToPreexisting((ReadOnlySpan<T>)buffer, destination);

        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="buffer"></param>
        public Buffer UploadBuffer<T>(T[] buffer) where T : unmanaged
            => UploadBuffer((ReadOnlySpan<T>)buffer);


        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="buffer"></param>
        public Buffer UploadBuffer<T>(Span<T> buffer) where T : unmanaged
            => UploadBuffer((ReadOnlySpan<T>)buffer);


        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="buffer"></param>
        public Buffer UploadBuffer<T>(ReadOnlySpan<T> buffer) where T : unmanaged
        {
            var destination = Device.Allocator.AllocateBuffer(buffer.Length * sizeof(T), MemoryAccess.GpuOnly, ResourceState.CopyDestination);
            UploadBufferToPreexisting(buffer, destination);
            return destination;
        }

        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="subresources"></param>
        /// <param name="tex"></param>
        public Texture UploadTexture(ReadOnlySpan<byte> texture, ReadOnlySpan<SubresourceData> subresources, in TextureDesc tex)
        {
            var destination = Device.Allocator.AllocateTexture(tex, ResourceState.CopyDestination);
            UploadTextureToPreexisting(texture, subresources, destination);
            return destination;
        }

        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="destination"></param>
        public void UploadBufferToPreexisting<T>(ReadOnlySpan<T> buffer, in Buffer destination) where T : unmanaged
        {
            var upload = _transientAllocator.AllocateBuffer(buffer.Length * sizeof(T));
            upload.WriteData(buffer);

            CopyBufferRegion(upload, destination);
        }

        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="subresources"></param>
        /// <param name="destination"></param>
        public void UploadTextureToPreexisting(ReadOnlySpan<byte> texture, ReadOnlySpan<SubresourceData> subresources, in Texture destination)
        {
            var upload = _transientAllocator.AllocateBuffer(
                checked((uint)Windows.GetRequiredIntermediateSize(destination.GetResourcePointer(), 0, (uint)subresources.Length))
            );

            upload.Buffer.SetName("Intermediate texture upload buffer");

            fixed (byte* pTextureData = texture)
            fixed (SubresourceData* pSubresources = subresources)
            {
                // D3D12_SUBRESOURCE_DATA and SubresourceData are blittable, just SubresourceData contains an offset past the pointer rather than the pointer
                // Fix that here
                for (var i = 0; i < subresources.Length; i++)
                {
                    ((D3D12_SUBRESOURCE_DATA*)&pSubresources[i])->pData = pTextureData + pSubresources[i].DataOffset;
                }

                FlushBarriers();
                _ = Windows.UpdateSubresources(
                   List,
                    destination.GetResourcePointer(),
                    upload.Buffer.GetResourcePointer(),
                    upload.Offset,
                    0,
                    (uint)subresources.Length,
                    (D3D12_SUBRESOURCE_DATA*)pSubresources
                );
            }
        }


        private void RetireBuffer(in Buffer buffer)
        {
            _listBuffers.Add(buffer);
        }

        /// <summary>
        /// Submits the <see cref="UploadContext"/> to be executed
        /// </summary>
        public override void Dispose()
        {
            // we execute list so we need to stop GpuContext.Dispose doing so
            Params.ExecuteOnClose = false;
            base.Dispose();
            var task = Device.Execute(this);
            _transientAllocator.Dispose(task);

            if (_listBuffers?.Count > 0)
            {
                task.RegisterCallback(_listBuffers, &ReleaseBuffers);
            }
        }

        private static void ReleaseBuffers(List<Buffer> buffers)
        {
            for (var i = 0; i < buffers.Count; i++)
            {
                buffers[i].Dispose();
            }
        }
    }
}
