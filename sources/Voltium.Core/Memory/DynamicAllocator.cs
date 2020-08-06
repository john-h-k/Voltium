using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Common.Debugging;
using Voltium.Core.Devices;

using SpinLock = Voltium.Common.Threading.SpinLockWrapped;

namespace Voltium.Core.Memory
{
    /// <summary>
    /// Represents a subset of a <see cref="Buffer"/>
    /// </summary>
    public unsafe struct BufferRegion
    {
        internal Buffer Buffer;
        internal uint Offset;
        internal uint Length;

        internal ID3D12Resource* GetResourcePointer() => Buffer.GetResourcePointer();

        // internal because it allows use-after-free
        internal Buffer.ScopedMap MapScoped() => Buffer.MapScoped();
        internal void Map() => Buffer.Map();
        internal void Unmap() => Buffer.Unmap();

        /// <summary>
        /// Writes the <typeparamref name="T"/> to the buffer
        /// </summary>
        /// <typeparam name="T">The type to write</typeparam>
        public void WriteData<T>(ref T data, uint offset) where T : unmanaged
            => Buffer.WriteData(ref data, Offset + offset);

        /// <summary>
        /// Writes the <typeparamref name="T"/> to the buffer
        /// </summary>
        /// <typeparam name="T">The type to write</typeparam>
        public void WriteConstantBufferData<T>(ref T data, uint offset) where T : unmanaged
            => Buffer.WriteConstantBufferData(ref data, Offset + offset);

        /// <summary>
        /// Writes the <typeparamref name="T"/> to the buffer
        /// </summary>
        /// <typeparam name="T">The type to write</typeparam>
        public void WriteDataByteOffset<T>(ref T data, uint offset) where T : unmanaged
            => Buffer.WriteDataByteOffset(ref data, Offset + offset);

        /// <summary>
        /// Writes the <typeparamref name="T"/> to the buffer
        /// </summary>
        /// <typeparam name="T">The type to write</typeparam>
        public void WriteData<T>(ReadOnlySpan<T> data, uint offset = 0) where T : unmanaged
            => Buffer.WriteData(data, Offset + offset);

        /// <summary>
        /// The <see cref="Span{T}"/> encompassing the data of this <see cref="BufferRegion"/>
        /// </summary>
        public Span<byte> Data => new Span<byte>((byte*)Buffer.CpuAddress + Offset, (int)Length);

        internal ulong GpuAddress => Buffer.GpuAddress + Offset;
    }

    /// <summary>
    /// An allocator used for short-lived per-frame GPU upload buffer allocations.
    /// This does not support textures, resource aliasing, or multi-frame resources
    /// </summary>
    public unsafe struct DynamicAllocator
    {
        private const nuint ConstantBufferAlignment = 256;

        private Buffer _page;
        private nuint _offset;
        private PageManager _manager;
        private List<Buffer> _usedPages;
        private List<Buffer> _usedLargePages;
        private MemoryAccess _access;

        /// <summary>
        /// Create a new <see cref="DynamicAllocator"/>
        /// </summary>
        /// <param name="device">The underlying <see cref="GraphicsDevice"/> to allocate from</param>
        /// <param name="access">The <see cref="MemoryAccess"/> for this <see cref="DynamicAllocator"/></param>
        public DynamicAllocator(ComputeDevice device, MemoryAccess access)
        {
            if (!_pageManagers.TryGetValue((device, access), out var manager))
            {
                lock (_pageManagers)
                {
                    manager = new PageManager(device, access);
                    _ = GCHandle.Alloc(manager);
                    _pageManagers[(device, access)] = manager;
                }
            }

            _manager = manager;
            _access = access;
            _page = _manager.GetPage();
            _offset = 0;
            _usedPages = new();
            _usedLargePages = new();
        }

        /// <summary>
        /// Allocates a buffer
        /// </summary>
        /// <param name="size">The size, in bytes, of the buffer</param>
        /// <param name="alignment">The alignment required by the buffer</param>
        /// <returns>A new <see cref="Buffer"/></returns>
        public BufferRegion AllocateBuffer(int size, nuint alignment = ConstantBufferAlignment)
            => AllocateBuffer((uint)size, alignment);


        /// <summary>
        /// Allocates a buffer
        /// </summary>
        /// <param name="size">The size, in bytes, of the buffer</param>
        /// <param name="alignment">The alignment required by the buffer</param>
        /// <returns>A new <see cref="Buffer"/></returns>
        public BufferRegion AllocateBuffer(uint size, nuint alignment = ConstantBufferAlignment)
        {
            if (size > int.MaxValue)
            {
                // because span is int length
                ThrowHelper.ThrowArgumentException("Cannot have larger than 2GB buffers with dynamic allocator");
            }

            Buffer page;
            nuint offset;
            if (size > _page.Length)
            {
                page = _manager.CreateLargePage(size);
                _usedLargePages.Add(page);
                offset = 0;
            }
            else
            {
                offset = MathHelpers.AlignUp(_offset, alignment);
                if (offset + size > _page.Length)
                {
                    _usedPages.Add(_page);
                    _page = _manager.GetPage();
                }

                page = _page;
                _offset += size + (offset - _offset);
            }

            return new BufferRegion { Buffer = page, Offset = (uint)offset, Length = size };
        }

        /// <summary>
        /// Disposes the allocator and indicates it can be disposed at a certain point
        /// </summary>
        /// <param name="task">The <see cref="GpuTask"/> to indicate when this intsance can dispose its memory</param>
        public void Dispose(in GpuTask task)
        {
            _usedPages.Add(_page);
            _manager.AddUsedPages(_usedPages.AsSpan(), task);

            if (task.IsCompleted)
            {
                ReleaseLargePages(_usedLargePages);
            }
            else
            {
                task.RegisterCallback(_usedLargePages, &ReleaseLargePages);
            }
        }

        private static void ReleaseLargePages(List<Buffer> largePages)
        {
            foreach (var page in largePages)
            {
                page.Dispose();
            }
        }


        private struct InUsePage
        {
            public Buffer Page;
            public GpuTask Free;
        }

        private static readonly Dictionary<(ComputeDevice, MemoryAccess Access), PageManager> _pageManagers = new();

        private sealed class PageManager
        {
            private ComputeDevice _device;
            private MemoryAccess _access;

            public PageManager(ComputeDevice device, MemoryAccess access)
            {
                _device = device;
                _access = access;
            }

            private LockedQueue<InUsePage, SpinLock> _pages = new(new SpinLock(EnvVars.IsDebug));

            public const nuint PageSize = 4 * 1024 * 1024; // 4mb

            public Buffer GetPage()
            {
                if (_pages.TryPeek(out var page) && page.Free.IsCompleted)
                {
                    return _pages.Dequeue().Page;
                }

                return CreatePage(PageSize);
            }

            public Buffer CreateLargePage(nuint size) => CreatePage(size);

            private Buffer CreatePage(nuint size)
            {
                var desc = new InternalAllocDesc
                {
                    Desc = new D3D12_RESOURCE_DESC
                    {
                        Width = size,
                        Height = 1,
                        DepthOrArraySize = 1,
                        Dimension = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER,
                        Flags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_DENY_SHADER_RESOURCE,
                        MipLevels = 1,

                        // required values for a buffer
                        SampleDesc = new DXGI_SAMPLE_DESC(1, 0),
                        Layout = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
                        Alignment = 0
                    },
                    HeapType = (D3D12_HEAP_TYPE)_access,
                    InitialState = _access == MemoryAccess.CpuUpload ? D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_GENERIC_READ : D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COPY_DEST,
                };

                using ComPtr<ID3D12Resource> page = _device.CreateCommittedResource(&desc);

                var buff = new Buffer(size, new GpuResource(page.Move(), desc, null));

                if (_access == MemoryAccess.CpuUpload)
                {
                    // we persistently map upload resources
                    buff.Map();
                }
                return buff;
            }

            public void AddUsedPage(in Buffer page, in GpuTask fence)
            {
                _pages.Enqueue(new InUsePage { Page = page, Free = fence });
            }

            public void AddUsedPages(ReadOnlySpan<Buffer> pages, in GpuTask fence)
            {
                foreach (var page in pages)
                {
                    AddUsedPage(page, fence);
                }
            }
        }
    }
}
