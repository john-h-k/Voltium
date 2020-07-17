//using System;
//using System.Collections.Generic;
//using TerraFX.Interop;
//using Voltium.Common;
//using Voltium.Common.Debugging;
//using Voltium.Core.Devices;

//using SpinLock = Voltium.Common.Threading.SpinLockWrapped;

//namespace Voltium.Core.Memory
//{
//    /// <summary>
//    /// Represents a subset of a <see cref="Buffer"/>
//    /// </summary>
//    public unsafe struct BufferRegion
//    {
//        internal Buffer Buffer;
//        internal ulong Offset;
//        internal int Length;

//        /// <summary>
//        /// The <see cref="Span{T}"/> encompassing the data of this <see cref="BufferRegion"/>
//        /// </summary>
//        public Span<byte> Data => new Span<byte>((byte*)Buffer.CpuAddress + Offset, Length);

//        internal ulong GpuAddress => Buffer.GpuAddress;
//    }

//    /// <summary>
//    /// An allocator used for short-lived per-frame GPU upload buffer allocations.
//    /// This does not support textures, resource aliasing, or multi-frame resources
//    /// </summary>
//    public unsafe class DynamicAllocator
//    {
//        private const nuint ConstantBufferAlignment = 256;

//        private ComputeDevice _device;
//        private Buffer _page;
//        private nuint _offset;
//        private PageManager _manager;
//        private List<Buffer> _usedPages = new();
//        private List<Buffer> _usedLargePages = new();

//        /// <summary>
//        /// Create a new <see cref="DynamicAllocator"/>
//        /// </summary>
//        /// <param name="device">The underlying <see cref="GraphicsDevice"/> to allocate from</param>
//        public DynamicAllocator(GraphicsDevice device)
//        {
//            _device = device;
//            if (!_pageManagers.TryGetValue(device, out var manager))
//            {
//                lock (_pageManagers)
//                {
//                    manager = new PageManager(device);
//                    _pageManagers[device] = manager;
//                }
//            }

//            _manager = manager;
//            _page = _manager.GetPage();
//        }

//        /// <summary>
//        /// Allocates a buffer
//        /// </summary>
//        /// <param name="size">The size, in bytes, of the buffer</param>
//        /// <param name="alignment">The alignment required by the buffer</param>
//        /// <returns>A new <see cref="Buffer"/></returns>
//        public BufferRegion Allocate(int size, nuint alignment = ConstantBufferAlignment)
//            => Allocate((uint)size, alignment);


//        /// <summary>
//        /// Allocates a buffer
//        /// </summary>
//        /// <param name="size">The size, in bytes, of the buffer</param>
//        /// <param name="alignment">The alignment required by the buffer</param>
//        /// <returns>A new <see cref="Buffer"/></returns>
//        public BufferRegion Allocate(uint size, nuint alignment = ConstantBufferAlignment)
//        {
//            if (size > int.MaxValue)
//            {
//                // because span is int length
//                ThrowHelper.ThrowArgumentException("Cannot have larger than 2GB buffers with dynamic allocator");
//            }

//            Buffer page;
//            nuint offset;
//            if (size > _page.Length)
//            {
//                page = _manager.CreateLargePage(size, alignment);
//                offset = 0;
//            }
//            else
//            {
//                offset = MathHelpers.AlignUp(_offset, alignment);
//                if (offset + size > _page.Length)
//                {
//                    _usedPages.Add(_page);
//                    _page = _manager.GetPage();
//                }

//                page = _page;
//                _offset += size + (offset - _offset);
//            }

//            return new BufferRegion { Buffer = page, Offset = offset, Length = (int)size };
//        }

//        /// <summary>
//        /// Causes the allocator to mark all used pages as deallocated when <paramref see="marker"/> is reached
//        /// </summary>
//        /// <param name="reached"></param>
//        /// <param name="marker">The <see cref="FenceMarker"/> indicating when the resources can safely be deallocated</param>
//        public void MoveToNextFrame(FenceMarker reached, FenceMarker marker)
//        {
//            _manager.AddUsedPages(_usedPages, marker);
//            _manager.AddUsedLargePages(_usedLargePages, marker);
//            _usedPages.Clear();
//            _usedLargePages.Clear();

//            _manager.MoveToNextFrame(reached);
//        }

//        private struct InUsePage
//        {
//            public Buffer Page;
//            public FenceMarker Free;
//        }

//        private static readonly Dictionary<GraphicsDevice, PageManager> _pageManagers = new();
//        private class PageManager
//        {
//            private GraphicsDevice _device;

//            public PageManager(GraphicsDevice device)
//            {
//                _device = device;
//            }

//            private LockedQueue<InUsePage, SpinLock> _usedPages = new(new SpinLock(EnvVars.IsDebug));
//            private LockedQueue<InUsePage, SpinLock> _usedLargePages = new(new SpinLock(EnvVars.IsDebug));
//            private LockedQueue<Buffer, SpinLock> _availablePages = new(new SpinLock(EnvVars.IsDebug));

//            public const nuint PageSize = 4 * 1024 * 1024; // 4mb
//            public const nuint PageAlignment = 4 * 1024; // 4kb

//            public Buffer GetPage()
//            {
//                if (_availablePages.TryDequeue(out var page))
//                {
//                    return page;
//                }

//                return CreatePage(PageSize, PageAlignment);
//            }

//            public Buffer CreateLargePage(nuint size, nuint alignment) => CreatePage(size, alignment);

//            private const uint BufferAlignment = 65536;


//            private Buffer CreatePage(nuint size, nuint alignment)
//            {
//                var desc = new InternalAllocDesc
//                {
//                    Desc = new D3D12_RESOURCE_DESC
//                    {
//                        Width = size,
//                        Height = 1,
//                        DepthOrArraySize = 1,
//                        Dimension = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER,
//                        Flags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_DENY_SHADER_RESOURCE,
//                        MipLevels = 1,

//                        // required values for a buffer
//                        SampleDesc = new DXGI_SAMPLE_DESC(1, 0),
//                        Layout = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
//                        Alignment = alignment
//                    },
//                    HeapType = D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_UPLOAD,
//                    InitialState = D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_GENERIC_READ
//                };

//                using ComPtr<ID3D12Resource> page = _device.CreateCommittedResource(&desc);

//                var buff = new Buffer(size, new GpuResource(page.Move(), desc, null));
//                // we persistently map upload resources
//                buff.Map();
//                return buff;
//            }

//            public void MoveToNextFrame(FenceMarker marker)
//            {
//                while (_usedPages.TryPeek(out var page) && marker >= page.Free)
//                {
//                    _availablePages.Enqueue(_usedPages.Dequeue().Page);
//                }
//                while (_usedLargePages.TryPeek(out var page) && marker >= page.Free)
//                {
//                    _usedPages.Dequeue().Page.Resource.Dispose();
//                }
//            }

//            public void AddUsedPage(in Buffer page, FenceMarker fence)
//            {
//                _usedPages.Enqueue(new InUsePage { Page = page, Free = fence });
//            }

//            public void AddUsedLargePages(ReadOnlySpan<Buffer> pages, FenceMarker fence)
//            {
//                foreach (var page in pages)
//                {
//                    AddUsedLargePage(page, fence);
//                }
//            }

//            public void AddUsedLargePage(in Buffer page, FenceMarker fence)
//            {
//                _usedLargePages.Enqueue(new InUsePage { Page = page, Free = fence });
//            }

//            public void AddUsedPages<T>(T pages, FenceMarker fence) where T : IEnumerable<Page>
//            {
//                foreach (var page in pages)
//                {
//                    AddUsedPage(page, fence);
//                }
//            }
//        }
//    }
//}
