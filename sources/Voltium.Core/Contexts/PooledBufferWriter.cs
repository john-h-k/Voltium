using System;
using System.Buffers;
using System.Threading;

namespace Voltium.Core
{
    public struct PooledBufferWriter<T> : IBufferWriter<T>, IDisposable
    {
        private MemoryPool<T> _pool;
        private IMemoryOwner<T> _owner;
        private Memory<T> _memory;


        private const int DefaultCapacity = 0;

        public PooledBufferWriter(MemoryPool<T>? pool = null)
        {
            _pool = pool ?? MemoryPool<T>.Shared;
            _owner = _pool.Rent(DefaultCapacity);
            _memory = _owner.Memory;
        }

        public void Resize(int newSize)
        {
            var old = _owner;

            _owner = _pool.Rent(newSize);
            _memory = _owner.Memory;

            old.Memory.CopyTo(_memory);
            old.Dispose();
        }

        public void Advance(int count)
        {
            if (_memory.Length < count)
            {
                _memory = _memory.Slice(count);
                return;
            }

            Resize(Math.Max(_owner.Memory.Length * 2, count));
        }

        public Memory<T> GetMemory(int sizeHint = 0)
        {
            sizeHint = Math.Max(sizeHint, 1);

            if (_memory.Length < sizeHint)
            {
                Resize(Math.Max(_owner.Memory.Length * 2, sizeHint));
            }

            return _memory;
        }

        public Span<T> GetSpan(int sizeHint = 0) => GetMemory().Span;

        public void Dispose()
        {
            var owner = Interlocked.Exchange(ref _owner, null!);
            var pool = Interlocked.Exchange(ref _pool, null!);

            owner?.Dispose();
            pool?.Dispose();
        }
    }
}
