using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Voltium.Common
{
    internal readonly unsafe struct UnmanagedSpan<T> where T : unmanaged
    {
        private readonly T* _pointer;
        private readonly int _length;

        public UnmanagedSpan(void* pointer, int length)
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                ThrowHelper.ThrowArgumentException(nameof(T));
            if (length < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(length));

            _pointer = (T*)pointer;
            _length = length;
        }

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)_length)
                    ThrowHelper.ThrowIndexOutOfRangeException();
                return ref _pointer[index];
            }
        }
         
        public T* GetPointer(int index)
            => (T*)Unsafe.AsPointer(ref this[index]);

        public int Length => _length;

        public bool IsEmpty => 0 >= (uint)_length;
    }
}
