using System;
using System.Collections.Generic;
using System.Diagnostics;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.D3D12;
using Voltium.Core.Managers;
using static TerraFX.Interop.D3D12_RESOURCE_STATES;

namespace Voltium.Core.GpuResources
{
    ///// <summary>
    ///// An allocator used for allocating GPU memory.
    ///// This type is not thread safe
    ///// </summary>
    //internal struct GpuAllocator
    //{
    //    private const int DefaultBufferPageSize = 64 * 1024; // 64kb
    //    private const int UploadBufferPageSize = 2 * 1024 * 1024; // 2mb
    //    private const int ReadbackBufferPageSize = -1; // TODO

    //    private readonly GpuPageAllocator _pageAllocator;
    //    private GpuResource _currentPage;
    //    private nuint _currentPageOffset;
    //    private nuint _currentPageSize;

    //    // These pages are marked to be recycled or deleted but don't yet have an associated fence
    //    private Queue<GpuResource> _framePagesToRecycle;
    //    private Queue<GpuResource> _framePagesToDelete;

    //    public GpuAllocation Allocate(nuint size)
    //    {
    //        if (size < _currentPageSize - _currentPageOffset)
    //        {
    //            var alloc = new GpuAllocation(_currentPage.Copy(), size, _currentPageOffset);

    //            _currentPageOffset += size;

    //            return alloc.Move();
    //        }
    //        else
    //        {
    //            var page = _pageAllocator.GetPage(size);
    //        }
    //    }

    //    internal unsafe sealed class GpuPageAllocator
    //    {
    //        private readonly int DefaultPageSize;
    //        private ExecutionContext _context;
    //        private FenceMarker _currentFence;
    //        private GpuMemoryType _type;

    //        // These pages can be recycled/deleted when their fence is reached
    //        private Queue<KeyValuePair<FenceMarker, GpuResource>> _pagesForRecycling = new(16);
    //        private Queue<KeyValuePair<FenceMarker, GpuResource>> _pagesForDeletion = new(16);
    //        // These pages are available for use
    //        private Queue<GpuResource> _availablePages = new();

    //        public GpuResource GetNewPage(int minimumSize = -1)
    //        {
    //            RecyclePages();

    //            if (_availablePages.TryDequeue(out var page))
    //            {
    //                return page.Move();
    //            }
    //            else
    //            {
    //                return CreatePage((ulong)Math.Max(minimumSize, DefaultPageSize));
    //            }
    //        }

    //        private GpuResource CreatePage(ulong size)
    //        {
    //            D3D12_HEAP_PROPERTIES properties = new((D3D12_HEAP_TYPE)_type);
    //            D3D12_RESOURCE_DESC desc = D3D12_RESOURCE_DESC.Buffer(width: size);

    //            using ComPtr<ID3D12Resource> page = default;

    //            var state = GetInitialPageState();

    //            Guard.ThrowIfFailed(DeviceManager.Manager.Device->CreateCommittedResource(
    //                &properties,
    //                D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
    //                &desc,
    //                state,
    //                null,
    //                page.Guid,
    //                ComPtr.GetVoidAddressOf(&page)
    //            ));

    //            return new GpuResource(page, state);
    //        }

    //        private D3D12_RESOURCE_STATES GetInitialPageState() => _type switch
    //        {
    //            // TODO are these the best options
    //            // they reflect the GPU state not CPU states. So general use case for CpuReadOptimized
    //            // is that GPU is using it as copy dest etc
    //            GpuMemoryType.CpuReadOptimized => D3D12_RESOURCE_STATE_COPY_DEST,
    //            GpuMemoryType.CpuWriteOptimized => D3D12_RESOURCE_STATE_GENERIC_READ,
    //            GpuMemoryType.GpuOnly => D3D12_RESOURCE_STATE_COMMON,
    //            _ => default
    //        };

    //        public void MarkFence(FenceMarker marker)
    //        {
    //            _currentFence = marker;

    //            RecyclePages();
    //            DeletePages();
    //        }

    //        private void DeletePages()
    //        {
    //            for (var i = 0; i < _pagesForDeletion.Count; i++)
    //            {
    //                (var fence, var page) = _pagesForDeletion.Peek();
    //                if (_currentFence >= fence)
    //                {
    //                    using var _ = _pagesForDeletion.Dequeue().Value;
    //                    page.Dispose();
    //                }
    //            }
    //        }

    //        private void RecyclePages()
    //        {
    //            for (var i = 0; i < _pagesForRecycling.Count; i++)
    //            {
    //                (var fence, var page) = _pagesForRecycling.Peek();
    //                if (_currentFence >= fence)
    //                {
    //                    _ = _availablePages.Dequeue();
    //                    _availablePages.Enqueue(page.Move());
    //                }
    //            }
    //        }

    //    }
    // }
}
