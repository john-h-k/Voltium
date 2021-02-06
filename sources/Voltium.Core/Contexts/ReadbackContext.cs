using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.HighPerformance.Extensions;
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
        // TODO - UMA architectures wont let the copycontext methods be used (as no command list is present)

        private List<(Buffer Source, Memory<byte> Dest, uint FirstSubresource, uint NumSubresources, D3D12_RESOURCE_DESC? Layout)> _listBuffers;
        private DynamicAllocator _transientAllocator;
        private MemoryPool<byte> _allocator;
        private const int MaxNumNonListBuffers = 8;

        /// <summary>
        /// Whether this readback context implicitly behaves synchronously
        /// </summary>
        public bool IsContextSync => Params.Device.Allocator.GpuOnlyResourcesAreWritable;

        internal ReadbackContext(in ContextParams @params, MemoryPool<byte>? allocator = null) : base(@params)
        {
            _listBuffers = new();
            _transientAllocator = new DynamicAllocator(Device, MemoryAccess.CpuReadback);
            _allocator = allocator ?? MemoryPool<byte>.Shared;
        }

        private IMemoryOwner<byte> Allocate(uint length)
        {
            return _allocator.Rent((int)length);
        }

        /// <summary>
        /// Readbacks a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="source"></param>
        public IMemoryOwner<byte> ReadbackBuffer(in Buffer source)
        {
            var dest = Allocate(source.Length);
            ReadbackBufferToPreexisting(source, dest.Memory);
            return dest;
        }


        /// <summary>
        /// Readbacks a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="source"></param>
        /// <param name="firstSubresource"></param>
        public IMemoryOwner<byte> ReadbackTexture(in Texture source, uint firstSubresource = 0)
        {
            var pDevice = Params.Device.As<ID3D12Device>();

            var desc = source.GetResourcePointer()->GetDesc();
            var numSubresources = desc.GetSubresources(pDevice);

            var layouts = stackalloc D3D12_PLACED_SUBRESOURCE_FOOTPRINT[(int)numSubresources];
            var numRows = stackalloc uint[(int)numSubresources];
            var rowSizes = stackalloc ulong[(int)numSubresources];
            ulong totalSize;

            pDevice->GetCopyableFootprints(&desc, firstSubresource, numSubresources, 0, layouts, numRows, rowSizes, &totalSize);

            ulong totalDestSize = 0;
            for (var i = 0; i < numSubresources; i++)
            {
                totalDestSize += numRows[i] + rowSizes[i];
            }

            var dest = Allocate(checked((uint)totalDestSize));
            ReadbackTextureToPreexisting(source, dest.Memory);
            return dest;
        }

        /// <summary>
        /// Readbacks a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public void ReadbackBufferToPreexisting<T>(in Buffer source, Memory<T> destination) where T : unmanaged
        {
            var readback = _transientAllocator.AllocateBuffer(source.Length);
            CopyResource(source, readback);
            AddBuffer(source, destination.AsBytes());
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
            var readback = _transientAllocator.AllocateBuffer(checked((uint)Windows.GetRequiredIntermediateSize(tex.GetResourcePointer(), firstSubresource, numSubresources)));

            var layouts = stackalloc D3D12_PLACED_SUBRESOURCE_FOOTPRINT[(int)numSubresources];
            var numRows = stackalloc uint[(int)numSubresources];
            var rowSizes = stackalloc ulong[(int)numSubresources];
            ulong totalSize;

            var desc = tex.GetResourcePointer()->GetDesc();
            Params.Device.DevicePointer->GetCopyableFootprints(&desc, firstSubresource, numSubresources, 0, layouts, numRows, rowSizes, &totalSize);


            for (uint i = 0; i < numSubresources; i++)
            {
                var src = new D3D12_TEXTURE_COPY_LOCATION(tex.GetResourcePointer(), firstSubresource + i);
                var dest = new D3D12_TEXTURE_COPY_LOCATION(readback.GetResourcePointer(), layouts[i]);
                List->CopyTextureRegion(&dest, 0, 0, 0, &src, null);
            }

            AddBuffer(readback, Unsafe.As<Memory<T>, Memory<byte>>(ref destination), firstSubresource, numSubresources, tex.GetResourcePointer()->GetDesc());
        }

        /// <summary>
        /// Readbacks a buffer from the CPU to the GPU
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="destination"></param>
        /// <param name="firstSubresource"></param>
        public void ReadbackTextureToPreexisting<T>(in Texture tex, Memory<T> destination, uint firstSubresource = 0) where T : unmanaged
        {
            var desc = tex.GetResourcePointer()->GetDesc();
            var numSubresources = desc.GetSubresources(Params.Device.As<ID3D12Device>());
            ReadbackTextureToPreexisting(tex, destination, firstSubresource, numSubresources - firstSubresource);
        }


        private void AddBuffer(in Buffer buffer, Memory<byte> memory, uint firstSubresource = 0, uint numSubresources = 0, in D3D12_RESOURCE_DESC? layout = null)
        {
            _listBuffers.Add((buffer, memory, firstSubresource, numSubresources, layout));
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
                task.RegisterCallback(new ReadbackCallbackData { Device = Params.Device, Allocator = _transientAllocator, BufferList = _listBuffers }, &CopyAndReleaseBuffers);
            }
        }

        private class ReadbackCallbackData
        {
            public DynamicAllocator Allocator;
            public ComputeDevice Device = null!;
            public List<(Buffer Source, Memory<byte> Dest, uint FirstSubresource, uint NumSubresources, D3D12_RESOURCE_DESC? layouts)> BufferList = null!;
        }

        private static void CopyAndReleaseBuffers(ReadbackCallbackData data)
        {
            var allocator = data.Allocator;
            var bufferList = data.BufferList;
            for (var i = 0; i < bufferList.Count; i++)
            {
                var (buffer, dest, firstSubresource, numSubresources, layout) = bufferList[i];

                if (layout is null) // buffer
                {
                    buffer.Span.CopyTo(dest.Span);
                }
                else // texture resource
                {
                    CopyTexture(data.Device, buffer, dest, firstSubresource, numSubresources, layout.GetValueOrDefault());
                }
            }

            allocator.Dispose(GpuTask.Completed);

            static void CopyTexture(ComputeDevice device, in Buffer sourceRes, Memory<byte> destMem, uint firstSubresource, uint numSubresources, in D3D12_RESOURCE_DESC desc)
            {
                var source = sourceRes.Span;
                var dest = destMem.Span;

                var pDevice = device.As<ID3D12Device>();

                var layouts = stackalloc D3D12_PLACED_SUBRESOURCE_FOOTPRINT[(int)numSubresources];
                var numRows = stackalloc uint[(int)numSubresources];
                var rowSizes = stackalloc ulong[(int)numSubresources];
                ulong totalSize;

                fixed (D3D12_RESOURCE_DESC* pDesc = &desc)
                {
                    pDevice->GetCopyableFootprints(pDesc, firstSubresource, numSubresources, 0, layouts, numRows, rowSizes, &totalSize);
                }

                for (var i = firstSubresource; i < numSubresources; i++)
                {
                    var footprint = layouts[i];
                    var rowCount = (int)numRows[i];
                    var destRowPitch = (int)rowSizes[i];
                    var sourceRowPitch = (int)footprint.Footprint.RowPitch;

                    var offset = (int)footprint.Offset;

                    var sourceSlicePitch = (int)footprint.Footprint.RowPitch * rowCount;
                    var destSlicePitch = destRowPitch * rowCount;

                    for (var j = 0; j < footprint.Footprint.Depth; j++)
                    {
                        for (var k = 0; k < rowCount; k++)
                        {
                            var sourceOffset = (sourceSlicePitch * j) + (sourceRowPitch * k);
                            var destOffset = (destSlicePitch * j) + (destRowPitch * k);

                            source[(offset + sourceOffset)..destRowPitch].CopyTo(dest[(offset + destOffset)..destRowPitch]);
                        }
                    }
                }
            }
        }
    }
}
