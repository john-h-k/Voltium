using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Toolkit.HighPerformance.Extensions;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Common.Debugging;
using Voltium.Core.Devices;

using SpinLock = Voltium.Common.Threading.SpinLockWrapped;

namespace Voltium.Core.Memory
{

    /// <summary>
    /// An allocator used for short-lived per-frame GPU upload buffer allocations.
    /// This does not support textures, resource aliasing, or multi-frame resources
    /// </summary>
    public unsafe struct DynamicAllocator
    {
        private struct Page
        {
            public GpuResource Resource;
            public nuint UsedOffset;
            public nuint Length;

            public InternalAllocDesc Desc;
        }

        private const nuint ConstantBufferAlignment = 256;

        private ComputeDevice _device;
        private Page _page;
        private PageManager _manager;
        private List<Page> _usedPages;
        private List<Page> _usedLargePages;
        private readonly MemoryAccess _access;

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

            _device = device;
            _manager = manager;
            _access = access;
            _page = _manager.GetPage();
            _usedPages = new();
            _usedLargePages = new();
        }

        /// <summary>
        /// Allocates a buffer
        /// </summary>
        /// <param name="size">The size, in bytes, of the buffer</param>
        /// <param name="alignment">The alignment required by the buffer</param>
        /// <returns>A new <see cref="Buffer"/></returns>
        public Buffer AllocateBuffer(int size, nuint alignment = ConstantBufferAlignment)
            => AllocateBuffer((uint)size, alignment);


        /// <summary>
        /// Allocates a buffer
        /// </summary>
        /// <param name="size">The size, in bytes, of the buffer</param>
        /// <param name="alignment">The alignment required by the buffer</param>
        /// <returns>A new <see cref="Buffer"/></returns>
        public Buffer AllocateBuffer(uint size, nuint alignment = ConstantBufferAlignment)
        {
            if (size > int.MaxValue)
            {
                // because span is int length
                ThrowHelper.ThrowArgumentException("Cannot have larger than 2GB buffers with dynamic allocator");
            }

            Page page;
            nuint offset;
            if (size > _page.Length)
            {
                page = _manager.CreateLargePage(size);
                _usedLargePages.Add(page);
                offset = 0;
            }
            else
            {
                offset = MathHelpers.AlignUp(_page.UsedOffset, alignment);
                if (offset + size > _page.Length)
                {
                    _usedPages.Add(_page);
                    _page = _manager.GetPage();
                }

                page = _page;
                _page.UsedOffset += size + (offset - _page.UsedOffset);
            }

            
            return new Buffer(_device, page.Resource, offset, page.Desc);
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

        private static void ReleaseLargePages(List<Page> largePages)
        {
            foreach (var page in largePages)
            {
                page.Resource.Dispose();
            }
        }


        private struct InUsePage
        {
            public Page Page;
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

            public Page GetPage()
            {
                if (_pages.TryPeek(out var page) && page.Free.IsCompleted)
                {
                    return _pages.Dequeue().Page;
                }

                return CreatePage(PageSize);
            }

            public Page CreateLargePage(nuint size) => CreatePage(size);

            private Page CreatePage(nuint size)
            {
                var desc = new InternalAllocDesc
                {
                    Desc = new D3D12_RESOURCE_DESC
                    {
                        Width = size,
                        Height = 1,
                        DepthOrArraySize = 1,
                        Dimension = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER,
                        MipLevels = 1,

                        // required values for a buffer
                        SampleDesc = new DXGI_SAMPLE_DESC(1, 0),
                        Layout = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
                        Alignment = 0
                    },
                    HeapType = (D3D12_HEAP_TYPE)_access,
                    HeapProperties = new D3D12_HEAP_PROPERTIES((D3D12_HEAP_TYPE)_access),
                    InitialState = _access == MemoryAccess.CpuUpload ? D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_GENERIC_READ : D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COPY_DEST,
                };

                using UniqueComPtr<ID3D12Resource> page = _device.CreateCommittedResource(&desc);

                var buff = new Page { Resource = new GpuResource(_device, page.Move(), &desc, null), UsedOffset = 0, Length = size, Desc = desc };

                return buff;
            }

            public void AddUsedPage(in Page page, in GpuTask fence)
            {
                _pages.Enqueue(new InUsePage { Page = page, Free = fence });
            }

            public void AddUsedPages(ReadOnlySpan<Page> pages, in GpuTask fence)
            {
                foreach (var page in pages)
                {
                    AddUsedPage(page, fence);
                }
            }
        }
    }
}
