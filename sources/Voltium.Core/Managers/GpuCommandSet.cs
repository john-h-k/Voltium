using System;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.Core.Managers
{
    internal struct GpuCommandSet : IDisposable
    {
        public ComPtr<ID3D12GraphicsCommandList> List;
        public ComPtr<ID3D12CommandAllocator> Allocator;

        public GpuCommandSet Move()
        {
            var copy = this;
            copy.List = List.Move();
            copy.Allocator = Allocator.Move();
            return copy;
        }

        public GpuCommandSet Copy()
        {
            var copy = this;
            copy.List = List.Copy();
            copy.Allocator = Allocator.Copy();
            return copy;
        }

        public void Dispose()
        {
            List.Dispose();
            Allocator.Dispose();
        }
    }
}
