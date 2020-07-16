using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;

namespace Voltium.Core
{
    /// <summary>
    /// Represents a generic Gpu context
    /// </summary>
    public unsafe struct GpuContext : IDisposable
    {
        internal ComputeDevice Device;
        internal ComPtr<ID3D12GraphicsCommandList> _list;
        internal ComPtr<ID3D12CommandAllocator> _allocator;
        internal bool _executeOnClose;

        internal ID3D12GraphicsCommandList* List => _list.Get();
        internal ID3D12CommandAllocator* Allocator => _allocator.Get();

        private ResourceBarrier8 _barrierBuffer;
        private uint _currentBarrierCount;
        internal const uint MaxNumBarriers = 8;

        internal GpuContext(ComputeDevice device, ComPtr<ID3D12GraphicsCommandList> list, ComPtr<ID3D12CommandAllocator> allocator, bool executeOnClose)
        {
            Device = device;
            _list = list.Move();
            _allocator = allocator.Move();
            _executeOnClose = executeOnClose;
            // We can't read past this many buffers as we skip init'ing them
            _currentBarrierCount = 0;

            // Don't bother zero'ing expensive buffer
            Unsafe.SkipInit(out _barrierBuffer);
        }

        internal void AddBarrier(in D3D12_RESOURCE_BARRIER barrier)
        {
            if (_currentBarrierCount == MaxNumBarriers)
            {
                FlushBarriers();
            }

            _barrierBuffer[_currentBarrierCount++] = barrier;
        }

        internal void AddBarriers(ReadOnlySpan<D3D12_RESOURCE_BARRIER> barriers)
        {
            if (barriers.Length > MaxNumBarriers)
            {
                fixed (D3D12_RESOURCE_BARRIER* pBarriers = barriers)
                {
                    List->ResourceBarrier((uint)barriers.Length, pBarriers);
                }
                return;
            }
            if (_currentBarrierCount + barriers.Length >= MaxNumBarriers)
            {
                FlushBarriers();
            }

            barriers.CopyTo(_barrierBuffer.AsSpan(_currentBarrierCount));
        }

        internal void FlushBarriers()
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

        internal struct ResourceBarrier8
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
            public Span<D3D12_RESOURCE_BARRIER> AsSpan(uint offset = 0) => MemoryMarshal.CreateSpan(ref Unsafe.Add(ref E0, (int)offset), 8);
        }

        /// <summary>
        /// Submits this context to the device
        /// </summary>
        public void Dispose()
        {
            FlushBarriers();
            Guard.ThrowIfFailed(List->Close());
            if (_executeOnClose)
            {
                ((GraphicsDevice)Device).Execute(ref this);
            }
        }
    }
}
