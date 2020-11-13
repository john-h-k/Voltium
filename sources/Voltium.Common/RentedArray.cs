using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Voltium.Common
{
    // A thin wrapper over a rented array to allow 'using' blocks and minimise resource leaks
    internal readonly struct RentedArray<T> : IDisposable
    {
        public readonly T[] Value;
        public readonly int Length;
        public readonly ArrayPool<T> Pool;

        private RentedArray(T[] value, int length, ArrayPool<T> pool)
        {
            Value = value;
            Length = length;
            Pool = pool;
        }

        public Span<T> AsSpan() => Value.AsSpan(0, Length);

        // do this to avoid the extra GCHandle where not necessary
        internal unsafe struct Pinnable : IPinnable
        {
            private RentedArray<T> _array;
            private GCHandle _handle;
            private bool _isPrePinned;

            public Pinnable(in RentedArray<T> array, bool isPrePinned = false)
            {
                _array = array;
                _handle = default;
                _isPrePinned = isPrePinned;
            }

            public MemoryHandle Pin(int elementIndex = 0)
            {
                if (!_handle.IsAllocated && !_isPrePinned)
                {
                    _handle = GCHandle.Alloc(_array.Value, GCHandleType.Pinned);
                }

                return new MemoryHandle(Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(_array.Value)), pinnable: this);
            }

            public void Unpin()
            {
                if (_handle.IsAllocated)
                {
                    _handle.Free();
                }
            }
        }

        public static RentedArray<T> Create(int minimumLength, ArrayPool<T> pool = null!)
        {
            pool ??= ArrayPool<T>.Shared;

            return new RentedArray<T>(pool.Rent(minimumLength), minimumLength, pool);
        }

        public ref T GetPinnableReference() => ref MemoryMarshal.GetArrayDataReference(Value);

        public Pinnable CreatePinnable(bool underlyingArrayIsPrePinned = false) => new Pinnable(this, underlyingArrayIsPrePinned);
        public void Dispose() => Pool.Return(Value);
        public void Dispose(bool clear) => Pool.Return(Value, clear);
    }
}
