using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.Pool;
using Voltium.TextureLoading;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core.Contexts
{
    /// <summary>
    /// Represents a context on which GPU commands can be recorded
    /// </summary>
    public unsafe sealed class ReadbackContext : CopyContext
    {
        private List<(BufferRegion Source, Memory<byte> Dest, TextureLayout? Layout)> _listBuffers;
        private DynamicAllocator _transientAllocator;
        private MemoryPool<byte> _allocator;
        private const int MaxNumNonListBuffers = 8;



        internal ReadbackContext(in ContextParams @params, MemoryPool<byte>? allocator = null) : base(@params)
        {
            _listBuffers = new();
            _transientAllocator = new DynamicAllocator(Device, MemoryAccess.CpuReadback);
            _allocator = allocator ?? MemoryPool<byte>.Shared;
        }

        /// <summary>
        /// Readbacks a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public void ReadbackBufferToPreexisting<T>(in Buffer source, Memory<T> destination) where T : unmanaged
        {
            var readback = _transientAllocator.AllocateBuffer(source.Length);
            CopyBufferRegion(source, readback);
            AddBuffer(new BufferRegion { Buffer = source, Length = source.Length }, Unsafe.As<Memory<T>, Memory<byte>>(ref destination));
        }

        /// <summary>
        /// Readbacks a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="destination"></param>
        /// <param name="firstSubresource"></param>
        /// <param name="numSubresources"></param>
        public void ReadbackTextureToPreexisting<T>(in Texture tex, Memory<T> destination, uint firstSubresource, uint numSubresources) where T : unmanaged
        {
            var layout = Device.GetCopyableFootprints(tex, firstSubresource, numSubresources);
            var readback = _transientAllocator.AllocateBuffer(checked((uint)layout.TotalSize));

            var span = layout.Layouts;
            for (uint i = 0; i < span.Length; i++)
            {
                var src = new D3D12_TEXTURE_COPY_LOCATION(tex.GetResourcePointer(), firstSubresource + i);
                var dest = new D3D12_TEXTURE_COPY_LOCATION(readback.GetResourcePointer(), span[(int)i]);
                List->CopyTextureRegion(&dest, 0, 0, 0, &src, null);
            }

            AddBuffer(readback, Unsafe.As<Memory<T>, Memory<byte>>(ref destination), layout);
        }

        ///// <summary>
        ///// Readbacks a buffer from the CPU to the GPU
        ///// </summary>
        ///// <param name="tex"></param>
        ///// <param name="destination"></param>
        ///// <param name="firstSubresource"></param>
        ///// <param name="numSubresources"></param>
        //public void ReadbackTextureToPreexisting<T>(in Texture tex, Span<Memory<T>> destination, uint firstSubresource, uint numSubresources) where T : unmanaged
        //{
        //    var layout = Device.GetCopyableFootprints(tex, firstSubresource, numSubresources);
        //    var readback = _transientAllocator.AllocateBuffer(checked((uint)layout.TotalSize));

        //    var span = layout.Layouts.Span;
        //    for (uint i = 0; i < span.Length; i++)
        //    {
        //        var src = new D3D12_TEXTURE_COPY_LOCATION(tex.GetResourcePointer(), firstSubresource + i);
        //        var dest = new D3D12_TEXTURE_COPY_LOCATION(readback.GetResourcePointer(), span[(int)i]);
        //        List->CopyTextureRegion(&dest, 0, 0, 0, &src, null);
        //    }

        //    AddBuffer(readback, Unsafe.As<Memory<T>, Memory<byte>>(ref destination), layout);
        //}


        private void AddBuffer(in BufferRegion buffer, Memory<byte> memory, in TextureLayout? layout = null)
        {
            _listBuffers.Add((buffer, memory, layout));
        }

        /// <summary>
        /// Submits the <see cref="ReadbackContext"/> to be executed
        /// </summary>
        public override void Dispose()
        {
            // we execute list so we need to stop GpuContext.Dispose doing so
            Params.Flags = ContextFlags.None;
            base.Dispose();
            var task = Device.Execute(this);

            if (_listBuffers?.Count > 0)
            {
                task.RegisterCallback(new ReadbackCallbackData { Allocator = _transientAllocator, BufferList = _listBuffers }, &CopyAndReleaseBuffers);
            }
        }

        private class ReadbackCallbackData
        {
            public DynamicAllocator Allocator;
            public List<(BufferRegion Source, Memory<byte> Dest, TextureLayout? layouts)> BufferList = null!;
        }

        private static void CopyAndReleaseBuffers(ReadbackCallbackData data)
        {
            var allocator = data.Allocator;
            var bufferList = data.BufferList;
            for (var i = 0; i < bufferList.Count; i++)
            {
                var (buffer, dest, layout) = bufferList[i];

                if (layout is null) // buffer
                {
                    buffer.Data.CopyTo(dest.Span);
                }
                else // texture resource
                {
                    CopyTexture(buffer, dest, layout.GetValueOrDefault());
                }
            }

            allocator.Dispose(GpuTask.Completed);

            static void CopyTexture(in BufferRegion source, Memory<byte> dest, in TextureLayout layout)
            {
                var data = source.Data;
                var destData = dest.Span;


                for (int i = 0; i < layout.Layouts.Length; i++)
                {
                    var footprint = layout.Layouts[i].Footprint;
                    var numRows = (int)layout.NumRows[i];
                    var rowSize = (int)layout.RowSizes[i];
                    var destSlicePitch = (int)rowSize * numRows;


                    var subResData = data.Slice((int)layout.Layouts[i].Offset);
                    var destSubResData = destData.Slice((int)layout.Layouts[i].Offset);

                    var slicePitch = footprint.RowPitch * numRows;

                    for (int j = 0; j < footprint.Depth; j++)
                    {
                        var pSrcSlice = subResData.Slice((int)(slicePitch * j));
                        var pDestSlice = destSubResData.Slice(destSlicePitch * j);

                        for (int k = 0; k < numRows; k++)
                        {
                            pSrcSlice.Slice((int)(footprint.RowPitch * k), rowSize).CopyTo(pDestSlice.Slice(rowSize * k));
                        }
                    }
                }

            }
        }
    }
}
