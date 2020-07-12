using System;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Devices
{
    internal struct GpuCommandSet : IDisposable
    {
        public ComPtr<ID3D12GraphicsCommandList> List;
        public ComPtr<ID3D12CommandAllocator> Allocator;

        public void Dispose()
        {
            List.Dispose();
            Allocator.Dispose();
        }
    }
}
