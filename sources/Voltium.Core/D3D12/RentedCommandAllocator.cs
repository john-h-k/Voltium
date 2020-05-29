using System;
using System.Diagnostics;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Managers;

namespace Voltium.Core
{
    /// <summary>
    /// A rented <see cref="ID3D12CommandAllocator"/> from <see cref="CommandAllocatorPool"/>
    /// </summary>
    public unsafe struct RentedCommandAllocator : IDisposable
    {
        /// <summary>
        /// The underlying allocator
        /// </summary>
        public ID3D12CommandAllocator* Get() => _value.Get();

        internal ComPtr<ID3D12CommandAllocator> MovePtr() => _value.Move();

        private ComPtr<ID3D12CommandAllocator> _value;

        internal RentedCommandAllocator(ComPtr<ID3D12CommandAllocator> value)
        {
            Debug.Assert(value.Get() != null);
            _value = value;
        }

        /// <summary>
        /// Reset the underlying allocator
        /// </summary>
        public void Reset() => Guard.ThrowIfFailed(_value.Get()->Reset());

        /// <inheritdoc/>
        public RentedCommandAllocator Move()
        {
            var copy = this;
            copy._value = _value.Move();
            return copy;
        }

        /// <inheritdoc/>
        public RentedCommandAllocator Copy()
        {
            var copy = this;
            copy._value = _value.Copy();
            return copy;
        }

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose()
        {
            _value.Dispose();
        }
    }
}
