using System;
using System.Buffers;
using System.IO.MemoryMappedFiles;
using Microsoft.Win32.SafeHandles;

namespace Voltium.Common
{
    internal static class MemoryMappedFileExtensions
    {
        public static MemoryMappedFileMemoryManager GetMemory(this MemoryMappedFile file, int offset, int size)
        {
            return new MemoryMappedFileMemoryManager(file.SafeMemoryMappedFileHandle, offset, size);
        }
    }

    internal unsafe sealed class MemoryMappedFileMemoryManager : MemoryManager<byte>
    {
        private SafeMemoryMappedFileHandle _handle;
        private int _offset;
        private int _size;

        public MemoryMappedFileMemoryManager(SafeMemoryMappedFileHandle handle, int offset, int size)
        {
            _handle = handle;
            _offset = offset;
            _size = size;
        }

        public override Span<byte> GetSpan()
        {
            return new Span<byte>((byte*)_handle.DangerousGetHandle() + _offset, _size);
        }

        public override MemoryHandle Pin(int elementIndex = 0) => new MemoryHandle((byte*)_handle.DangerousGetHandle() + _offset, pinnable: this);

        public override void Unpin()
        {
        }

        protected override void Dispose(bool disposing)
        {
            _handle.Dispose();
        }
    }
}
