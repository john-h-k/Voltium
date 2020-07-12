namespace Voltium.Core.Memory
{
    ///// <summary>
    ///// An allocator used for short-lived per-frame GPU buffer allocations.
    ///// This does not support textures, resource aliasing, or multi-frame resources
    ///// </summary>
    //public unsafe class DynamicAllocator
    //{
    //    private const nuint ConstantBufferAlignment = 256;

    //    private GraphicsDevice _device;
    //    private Page _page;
    //    private nuint _offset;
    //    private PageManager _manager;
    //    private List<Page> _usedPages = new();
    //    private List<Page> _usedLargePages = new();

    //    /// <summary>
    //    /// Create a new <see cref="DynamicAllocator"/>
    //    /// </summary>
    //    /// <param name="device">The underlying <see cref="GraphicsDevice"/> to allocate from</param>
    //    public DynamicAllocator(GraphicsDevice device)
    //    {
    //        _device = device;
    //        if (!_pageManagers.TryGetValue(device, out var manager))
    //        {
    //            lock (_pageManagers)
    //            {
    //                manager = new PageManager(device);
    //                _pageManagers[device] = manager;
    //            }
    //        }

    //        _manager = manager;
    //        _page = _manager.GetPage();
    //    }

    //    /// <summary>
    //    /// Allocates a buffer
    //    /// </summary>
    //    /// <param name="size">The size, in bytes, of the buffer</param>
    //    /// <param name="alignment">The alignment required by the buffer</param>
    //    /// <returns>A new <see cref="Buffer"/></returns>
    //    public Buffer AllocateBuffer(int size, nuint alignment = ConstantBufferAlignment)
    //        => AllocateBuffer((nuint)size, alignment);


    //    /// <summary>
    //    /// Allocates a buffer
    //    /// </summary>
    //    /// <param name="size">The size, in bytes, of the buffer</param>
    //    /// <param name="alignment">The alignment required by the buffer</param>
    //    /// <returns>A new <see cref="Buffer"/></returns>
    //    public Buffer AllocateBuffer(nuint size, nuint alignment = ConstantBufferAlignment)
    //    {
    //        Page page;
    //        nuint offset;
    //        if (size > _page.Size)
    //        {
    //            page = _manager.CreateLargePage(size, alignment);
    //            offset = 0;
    //        }
    //        else
    //        {
    //            offset = MathHelpers.AlignUp(_offset, alignment);
    //            if (offset + size > _page.Size)
    //            {
    //                _usedPages.Add(_page);
    //                _page = _manager.GetPage();
    //            }

    //            page = _page;
    //            _offset += size + (offset - _offset);
    //        }

    //        var resDesc = new GpuResourceDesc(GpuResourceFormat.Buffer(size), MemoryAccess.CpuUpload, ResourceState.GenericRead);
    //        var res = new GpuResource(page.Resource.Copy(), resDesc, default, size, offset, null);
    //        var buff = new Buffer(res);
    //        buff.MapAs<byte>();
    //        return buff;
    //    }

    //    /// <summary>
    //    /// Causes the allocator to mark all used pages as deallocated when <paramref see="marker"/> is reached
    //    /// </summary>
    //    /// <param name="reached"></param>
    //    /// <param name="marker">The <see cref="FenceMarker"/> indicating when the resources can safely be deallocated</param>
    //    public void MoveToNextFrame(FenceMarker reached, FenceMarker marker)
    //    {
    //        _manager.AddUsedPages(_usedPages, marker);
    //        _manager.AddUsedLargePages(_usedLargePages, marker);
    //        _usedPages.Clear();
    //        _usedLargePages.Clear();

    //        _manager.MoveToNextFrame(reached);
    //    }

    //    private struct Page
    //    {
    //        public ComPtr<ID3D12Resource> Resource;
    //        public void* CpuPointer;
    //        public ulong GpuPointer;
    //        public nuint Size;
    //    }

    //    private struct InUsePage
    //    {
    //        public Page Page;
    //        public FenceMarker Free;
    //    }

    //    private static readonly Dictionary<GraphicsDevice, PageManager> _pageManagers = new();
    //    private class PageManager
    //    {
    //        private GraphicsDevice _device;

    //        public PageManager(GraphicsDevice device)
    //        {
    //            _device = device;
    //        }

    //        private LockedQueue<InUsePage, SpinLock> _usedPages = new(new SpinLock(EnvVars.IsDebug));
    //        private LockedQueue<InUsePage, SpinLock> _usedLargePages = new(new SpinLock(EnvVars.IsDebug));
    //        private LockedQueue<Page, SpinLock> _availablePages = new(new SpinLock(EnvVars.IsDebug));

    //        public const nuint PageSize = 4 * 1024 * 1024; // 4mb
    //        public const nuint PageAlignment = 4 * 1024; // 4kb

    //        public Page GetPage()
    //        {
    //            if (_availablePages.TryDequeue(out var page))
    //            {
    //                return page;
    //            }

    //            return CreatePage(PageSize, PageAlignment);
    //        }

    //        public Page CreateLargePage(nuint size, nuint alignment) => CreatePage(size, alignment);

    //        private const uint BufferAlignment = 65536;
    //        private Page CreatePage(nuint size, nuint alignment)
    //        {
    //            var heap = new D3D12_HEAP_PROPERTIES(D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_UPLOAD);
    //            var desc = D3D12_RESOURCE_DESC.Buffer(PageSize, alignment: Math.Max(alignment, BufferAlignment));

    //            using ComPtr<ID3D12Resource> page = default;
    //            Guard.ThrowIfFailed(_device.Device->CreateCommittedResource(
    //                &heap,
    //                D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
    //                &desc,
    //                D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_GENERIC_READ,
    //                null,
    //                page.Guid,
    //                ComPtr.GetVoidAddressOf(&page)
    //            ));

    //            void* cpuPointer;
    //            Guard.ThrowIfFailed(page.Get()->Map(0, null, &cpuPointer));
    //            ulong gpuPointer = page.Get()->GetGPUVirtualAddress();

    //            return new Page { Resource = page.Move(), CpuPointer = cpuPointer, GpuPointer = gpuPointer, Size = size };
    //        }

    //        public void MoveToNextFrame(FenceMarker marker)
    //        {
    //            while (_usedPages.TryPeek(out var page) && marker >= page.Free)
    //            {
    //                _availablePages.Enqueue(_usedPages.Dequeue().Page);
    //            }
    //            while (_usedLargePages.TryPeek(out var page) && marker >= page.Free)
    //            {
    //                _usedPages.Dequeue().Page.Resource.Dispose();
    //            }
    //        }

    //        public void AddUsedPage(Page page, FenceMarker fence)
    //        {
    //            _usedPages.Enqueue(new InUsePage { Page = page, Free = fence });
    //        }

    //        public void AddUsedLargePages<T>(T pages, FenceMarker fence) where T : IEnumerable<Page>
    //        {
    //            foreach (var page in pages)
    //            {
    //                AddUsedLargePage(page, fence);
    //            }
    //        }

    //        public void AddUsedLargePage(Page page, FenceMarker fence)
    //        {
    //            _usedLargePages.Enqueue(new InUsePage { Page = page, Free = fence });
    //        }

    //        public void AddUsedPages<T>(T pages, FenceMarker fence) where T : IEnumerable<Page>
    //        {
    //            foreach (var page in pages)
    //            {
    //                AddUsedPage(page, fence);
    //            }
    //        }
    //    }
    //}
}
