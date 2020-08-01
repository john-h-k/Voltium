using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop;
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
            _transientAllocator = new DynamicAllocator(Device);
        }

        //private struct BufferBuffer
        //{

        //    public Buffer Buffer0;
        //    public Buffer Buffer1;
        //    public Buffer Buffer2;
        //    public Buffer Buffer3;
        //    public Buffer Buffer4;
        //    public Buffer Buffer5;
        //    public Buffer Buffer6;
        //    public Buffer Buffer7;

        //    public ref Buffer GetPinnableReference() => ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Buffer0, 0));
        //    public ref Buffer this[int index] => ref Unsafe.Add(ref GetPinnableReference(), index);
        //}

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
        /// <param name="destination"></param>
        public void UploadBuffer<T>(T[] buffer, out Buffer destination) where T : unmanaged
            => UploadBuffer((ReadOnlySpan<T>)buffer, out destination);


        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="destination"></param>
        public void UploadBuffer<T>(Span<T> buffer, out Buffer destination) where T : unmanaged
            => UploadBuffer((ReadOnlySpan<T>)buffer, out destination);


        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="destination"></param>
        public void UploadBuffer<T>(ReadOnlySpan<T> buffer, out Buffer destination) where T : unmanaged
        {
            destination = Device.Allocator.AllocateBuffer(buffer.Length * sizeof(T), MemoryAccess.GpuOnly, ResourceState.CopyDestination);

            UploadBufferToPreexisting(buffer, destination);
        }

        /// <summary>
        /// Uploads a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="subresources"></param>
        /// <param name="tex"></param>
        /// <param name="destination"></param>
        public void UploadTexture(ReadOnlySpan<byte> texture, ReadOnlySpan<SubresourceData> subresources, in TextureDesc tex, out Texture destination)
        {
            destination = Device.Allocator.AllocateTexture(tex, ResourceState.CopyDestination);
            UploadTextureToPreexisting(texture, subresources, destination);
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
