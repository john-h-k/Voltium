using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Voltium.Common.Threading;

namespace Voltium.Common
{
    internal sealed class ArrayBuilder<T>
    {
        private static LockedQueue<T[], MonitorLock> _oldBuffers;

        private T[] _buffer;
        private int _length;
        private const int DefaultSize = 16;

        public int Length => _length;

        public static void Trim()
        {
            _oldBuffers.Clear();
        }

        public ArrayBuilder()
        {
            if (_oldBuffers.TryDequeue(out var buff))
            {
                _buffer = buff;
            }
            else
            {
                _buffer = new T[DefaultSize];
            }
        }

        public void Add(T t)
        {
            EnsureCanAdd(1);

            _buffer[_length++] = t;
        }

        public void Add(in T t)
        {
            _buffer[_length++] = t;
        }

        private void EnsureCanAdd(int count)
        {
            if (_length + count < _buffer.Length)
            {
                return;
            }

            ResizeBuffer(_length + count);
        }

        public T[] MoveTo(ArrayPool<T>? pool = null)
        {
            var result = pool is null ? new T[_buffer.Length] : pool.Rent(_buffer.Length);
            MoveTo(result);
            return result;
        }

        public void MoveTo(Span<T> buffer)
        {
            CopyBufferTo(buffer);
            KillBuffer();
        }

        private void KillBuffer()
        {
            if (ShouldCacheBuffer())
            {
                _oldBuffers.Enqueue(_buffer);
            }
            _buffer = null!;
        }

        private bool ShouldCacheBuffer()
        {
            return _oldBuffers.Count < 4;
        }

        private void CopyBufferTo(Span<T> result)
        {
            _buffer.AsSpan().CopyTo(result);
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                _buffer.AsSpan().Clear();
            }
        }

        private void ResizeBuffer(int newSize)
        {
            var newBuffer = new T[newSize];
            CopyBufferTo(newBuffer);
            _buffer = newBuffer;
        }
    }
}
