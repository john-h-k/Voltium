using System;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Devices
{
    public unsafe class Fence : IInternalD3D12Object, IDisposable
    {
        private UniqueComPtr<ID3D12Fence> _fence;

        internal Fence(UniqueComPtr<ID3D12Fence> fence)
        {
            _fence = fence.Move();
        }

        public ulong CompletedValue => _fence.Ptr->GetCompletedValue();
        public void Signal(ulong value) => _fence.Ptr->Signal(value);
        unsafe ID3D12Object* IInternalD3D12Object.GetPointer() => (ID3D12Object*)_fence.Ptr;

        public void Dispose() => _fence.Dispose();
    }
}
