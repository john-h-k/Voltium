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
using Voltium.TextureLoading;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core.Contexts
{
    /// <summary>
    /// Represents a context on which GPU commands can be recorded
    /// </summary>
    public unsafe struct UploadContext : IDisposable
    {
        private GpuContext _context;
        private List<Buffer> _listBuffers;
        private DynamicAllocator _transientAllocator;
        private int _numBuffers;
        private const int MaxNumNonListBuffers = 8;

        internal UploadContext(in GpuContext context)
        {
            _context = context;
            _listBuffers = new();
            _numBuffers = 0;
            _transientAllocator = new DynamicAllocator(_context.Device);
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



#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public void CopyBufferRegion(in BufferRegion source, in BufferRegion dest)
            => CopyBufferRegion(source.Buffer, source.Offset, dest.Buffer, dest.Offset, source.Length);

        public void CopyBufferRegion(in Buffer source, in BufferRegion dest)
            => CopyBufferRegion(source, 0, dest.Buffer, dest.Offset, source.Length);

        public void CopyBufferRegion(in BufferRegion source, in Buffer dest)
            => CopyBufferRegion(source.Buffer, source.Offset, dest, 0, source.Length);

        public void CopyBufferRegion(in Buffer source, uint sourceOffset, in Buffer dest, uint destOffset, uint numBytes)
            => _context.List->CopyBufferRegion(dest.GetResourcePointer(), destOffset, source.GetResourcePointer(), sourceOffset, numBytes);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Copy an entire resource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="dest">The resource to copy to</param>
        public void CopyResource(in Buffer source, in Buffer dest)
            => _context.List->CopyResource(dest.GetResourcePointer(), source.GetResourcePointer());

        /// <summary>
        /// Copy an entire resource
        /// </summary>
        /// <param name="source">The resource to copy from</param>
        /// <param name="dest">The resource to copy to</param>
        public void CopyResource(in Texture source, in Texture dest)
            => _context.List->CopyResource(dest.GetResourcePointer(), source.GetResourcePointer());

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
            destination = _context.Device.Allocator.AllocateBuffer(buffer.Length * sizeof(T), MemoryAccess.GpuOnly, ResourceState.CopyDestination);

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
            destination = _context.Device.Allocator.AllocateTexture(tex, ResourceState.CopyDestination);
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

                _context.FlushBarriers();
                _ = Windows.UpdateSubresources(
                    _context.List,
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
        public void Dispose()
        {
            // we execute list so we need to stop GpuContext.Dispose doing so
            _context._executeOnClose = false;
            _context.Dispose();
            var task = _context.Device.Execute(ref _context);
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
