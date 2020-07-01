using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.GpuResources;
using Voltium.Core.Managers;
using Voltium.Core.Memory.GpuResources;
using Buffer = Voltium.Core.Memory.GpuResources.Buffer;

namespace Voltium.Core
{
    internal unsafe struct GpuContext : IDisposable
    {
        public GraphicsDevice Device;
        public ComPtr<ID3D12GraphicsCommandList> _list;
        public ComPtr<ID3D12CommandAllocator> _allocator;

        public ID3D12GraphicsCommandList* List => _list.Get();
        public ID3D12CommandAllocator* Allocator => _allocator.Get();

        private ResourceBarrier8 _barrierBuffer;
        private uint _currentBarrierCount;
        private const uint MaxNumBarriers = 8;

        public GpuContext(GraphicsDevice device, ComPtr<ID3D12GraphicsCommandList> list, ComPtr<ID3D12CommandAllocator> allocator)
        {
            Device = device;
            _list = list.Move();
            _allocator = allocator.Move();
            // We can't read past this many buffers as we skip init'ing them
            _currentBarrierCount = 0;

            // Don't bother zero'ing expensive buffer
            Unsafe.SkipInit(out _barrierBuffer);
        }

        public void AddBarrier(in D3D12_RESOURCE_BARRIER barrier)
        {
            if (_currentBarrierCount == MaxNumBarriers)
            {
                FlushBarriers();
            }

            _barrierBuffer[_currentBarrierCount++] = barrier;
        }

        public void FlushBarriers()
        {
            if (_currentBarrierCount == 0)
            {
                return;
            }

            fixed (D3D12_RESOURCE_BARRIER* pBarriers = _barrierBuffer)
            {
                List->ResourceBarrier(_currentBarrierCount, pBarriers);
            }

            _currentBarrierCount = 0;
        }

        private struct ResourceBarrier8
        {
            public D3D12_RESOURCE_BARRIER E0;
            public D3D12_RESOURCE_BARRIER E1;
            public D3D12_RESOURCE_BARRIER E2;
            public D3D12_RESOURCE_BARRIER E3;
            public D3D12_RESOURCE_BARRIER E4;
            public D3D12_RESOURCE_BARRIER E5;
            public D3D12_RESOURCE_BARRIER E6;
            public D3D12_RESOURCE_BARRIER E7;

            public ref D3D12_RESOURCE_BARRIER this[uint index]
                => ref Unsafe.Add(ref GetPinnableReference(), (int)index);

            public ref D3D12_RESOURCE_BARRIER GetPinnableReference() => ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref E0, 0));
        }

        public void Dispose()
        {
            FlushBarriers();
            Device.End(ref this);
        }
    }
}
