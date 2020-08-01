using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.Pool;

namespace Voltium.Core
{
    /// <summary>
    /// Represents a generic Gpu context
    /// </summary>
    public unsafe class GpuContext : IDisposable
    {
        internal ContextParams Params;

        internal ID3D12GraphicsCommandList* GetListPointer() => List;

        internal ID3D12GraphicsCommandList* List => Params.List.Get();
        internal ID3D12CommandAllocator* Allocator => Params.Allocator.Get();
        internal ComputeDevice Device => Params.Device;

        internal ExecutionContext Context => Params.Context;

        private ResourceBarrier8 _barrierBuffer;
        private uint _currentBarrierCount;
        internal const uint MaxNumBarriers = 8;

        internal GpuContext(in ContextParams @params)
        {
            Params = @params;
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
            if (barriers.Length == 0)
            {
                return;
            }

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
            _currentBarrierCount += (uint)barriers.Length;
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
            public Span<D3D12_RESOURCE_BARRIER> AsSpan(uint offset = 0) => MemoryMarshal.CreateSpan(ref Unsafe.Add(ref E0, (int)offset), 8 - (int)offset);
        }

        /// <summary>
        /// Submits this context to the device
        /// </summary>
        public virtual void Close() => Dispose();

        /// <summary>
        /// Submits this context to the device
        /// </summary>
        public virtual void Dispose()
        {
            FlushBarriers();
            Guard.ThrowIfFailed(List->Close());
            if (Params.ExecuteOnClose)
            {
                _ = Params.Device.Execute(this);
            }
        }
    }
}
