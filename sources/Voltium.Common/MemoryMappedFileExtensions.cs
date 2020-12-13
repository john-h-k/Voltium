using System;
using System.Buffers;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Voltium.Common
{
    internal static class MemoryMappedFileExtensions
    {
        public static MemoryMappedFileMemoryManager GetMemory(this MemoryMappedFile file, int offset, int size)
         => file.CreateViewAccessor(offset, size).GetMemory(size);

        public static MemoryMappedFileMemoryManager GetMemory(this MemoryMappedViewAccessor file, int size)
            => new MemoryMappedFileMemoryManager(file.SafeMemoryMappedViewHandle, checked((int)file.PointerOffset), size);
    }

    internal unsafe sealed class MemoryMappedFileMemoryManager : MemoryManager<byte>
    {
        private SafeMemoryMappedViewHandle _view;
        private byte* _pointer;
        private int _offset;
        private int _size;

        public MemoryMappedFileMemoryManager(SafeMemoryMappedViewHandle view, int offset, int size)
        {
            _view = view;
            _offset = offset;
            _size = size;
            _view.AcquirePointer(ref _pointer);
        }

        public override Span<byte> GetSpan()
        {
            ThrowIfDisposed();
            return new Span<byte>(_pointer + _offset, _size);
        }

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            ThrowIfDisposed();
            return new MemoryHandle((byte*)_view.DangerousGetHandle() + _offset, pinnable: this);
        }

        private void ThrowIfDisposed()
        {
            if (!_view.IsClosed && !_view.IsInvalid)
            {
                return;
            }

            ThrowHelper.ThrowObjectDisposedException(nameof(_view));
        }

        public override void Unpin()
        {
        }

        protected override void Dispose(bool disposing)
        {
            _view.Dispose();
        }
    }
}
