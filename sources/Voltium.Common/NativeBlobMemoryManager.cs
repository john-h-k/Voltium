using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;

namespace Voltium.Common
{
    internal unsafe sealed class DxcBlobMemoryManager : MemoryManager<byte>
    {
        private ComPtr<IDxcBlob> _blob;

        public DxcBlobMemoryManager(ComPtr<IDxcBlob> blob)
        {
            _blob = blob.Move();
        }

        public override Span<byte> GetSpan() => new Span<byte>(_blob.Ptr->GetBufferPointer(), (int)_blob.Ptr->GetBufferSize());

        public override MemoryHandle Pin(int elementIndex = 0) => new MemoryHandle((byte*)_blob.Ptr->GetBufferPointer() + elementIndex);

        public override void Unpin()
        {
            // mem is already pinned
        }

        protected override void Dispose(bool disposing)
        {
            _blob.Dispose();
        }
    }

    internal unsafe sealed class D3DBlobMemoryManager : MemoryManager<byte>
    {
        private ComPtr<ID3DBlob> _blob;

        public D3DBlobMemoryManager(ComPtr<ID3DBlob> blob)
        {
            _blob = blob.Move();
        }

        public override Span<byte> GetSpan() => new Span<byte>(_blob.Ptr->GetBufferPointer(), (int)_blob.Ptr->GetBufferSize());

        public override MemoryHandle Pin(int elementIndex = 0) => new MemoryHandle((byte*)_blob.Ptr->GetBufferPointer() + elementIndex);

        public override void Unpin()
        {
            // mem is already pinned
        }

        protected override void Dispose(bool disposing)
        {
            _blob.Dispose();
        }
    }
}
