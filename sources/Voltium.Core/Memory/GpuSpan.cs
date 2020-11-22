using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Voltium.Common;

namespace Voltium.Core.Memory
{
    internal readonly unsafe struct GpuSpan<T> : IEquatable<GpuSpan<T>> where T : unmanaged
    {
        public ulong GpuPointer { get; }
        public ulong Length { get; }

        // Keep as byte* to make arithmetic the same as GpuPointer
        public readonly byte* _pointer;

        public T* Pointer => (T*)_pointer;

        public static GpuSpan<T> Empty => default;

        public bool IsEmpty => 0 >= Length;

        public GpuSpan(ulong gpuPointer, void* pointer, ulong length)
        {
            GpuPointer = gpuPointer;
            _pointer = (byte*)pointer;
            Length = length;
        }

        private static ulong GetOffset(ulong offset) => (uint)sizeof(T) * offset;

        /// <summary>
        /// Forms a slice out of the given read-only span, beginning at 'start'.
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GpuSpan<T> Slice(ulong start)
        {
            if (start > Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(start));
            }

            var offset = GetOffset(start);

            return new GpuSpan<T>(GpuPointer + offset, _pointer + offset, Length - start);
        }

        /// <summary>
        /// Forms a slice out of the given read-only span, beginning at 'start', of given length
        /// </summary>
        /// <param name="start">The index at which to begin this slice.</param>
        /// <param name="length">The desired length for the slice (exclusive).</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="start"/> or end index is not in range (&lt;0 or &gt;Length).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GpuSpan<T> Slice(ulong start, ulong length)
        {
            if (start > Length || start + length > Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(start));
            }

            var offset = GetOffset(start);

            return new GpuSpan<T>(GpuPointer + offset, _pointer + offset, length);
        }

        public ref T this[ulong index] => ref ((T*)_pointer)[index];

        public static bool operator ==(GpuSpan<T> left, GpuSpan<T> right)
            => left.GpuPointer == right.GpuPointer && left.Length == right.Length;


        public static bool operator !=(GpuSpan<T> left, GpuSpan<T> right) => !(left == right);

        public bool Equals(GpuSpan<T> other) => this == other;

        public override bool Equals(object? obj) => obj is GpuSpan<T> other && this == other;

        public override int GetHashCode() => HashCode.Combine(GpuPointer, Length);

        public override string ToString() => $"Voltium.Core.Memory.GpuSpan<{typeof(T).Name}>{Length}";
    }
}
