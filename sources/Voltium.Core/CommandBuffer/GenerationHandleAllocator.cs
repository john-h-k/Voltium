using System;
using System.Runtime.CompilerServices;
using Voltium.Common;

namespace Voltium.Core.CommandBuffer
{
    public sealed class GenerationHandleAllocator<THandle, THandleData> where THandle : struct, IHandle<THandle>
    {
        private const uint InvalidGeneration = uint.MaxValue;

        private struct HandleDataPair
        {
            public bool IsAllocated;
            public GenerationalHandle Handle;
            public THandleData HandleData;
        }

        private HandleDataPair[] _array;

        public GenerationHandleAllocator(int capacity)
        {
            _array = new HandleDataPair[capacity];
        }

        public THandle AllocateHandle() => AllocateHandle(Unsafe.NullRef<THandleData>());
        public THandle AllocateHandle(in THandleData data)
        {
            for (int i = 0; i < _array.Length; i++)
            {
                ref var pair = ref _array[i];
                if (!pair.IsAllocated)
                {
                    // Next generation
                    pair.Handle = new GenerationalHandle(pair.Handle.Generation + 1, pair.Handle.Id);
                    pair.IsAllocated = true;
                    pair.HandleData = Unsafe.IsNullRef(ref Unsafe.AsRef(in data)) ? default! : data; // zero for safety

                    return default(THandle).FromGenerationHandle(pair.Handle);
                }
            }

            // resize
            throw new NotImplementedException();
        }


        public bool IsHandleValid(THandle handle)
        {
            return IsValid(handle, GetPairRef(handle));
        }

        private bool IsValid(THandle handle, in HandleDataPair pair) => pair.IsAllocated && pair.Handle.Generation == handle.Generational.Generation;

        private ref HandleDataPair GetValidPairRef(THandle handle)
        {
            ref var pair = ref GetPairRef(handle);
            if (!IsValid(handle, pair))
            {
                ThrowHelper.ThrowArgumentException("Handle is invalid");
            }
            return ref pair;
        }

        private ref HandleDataPair GetPairRef(THandle handle)
        {
            var genHandle = handle.Generational;
            return ref _array[genHandle.Id];
        }

        public bool TryGetHandleData(THandle handle, out THandleData data)
        {
            ref var pair = ref GetPairRef(handle);
            if (IsValid(handle, pair))
            {
                data = pair.HandleData;
                return true;
            }
            data = default!;
            return false;
        }

        public THandleData GetHandleData(THandle handle) => GetValidPairRef(handle).HandleData;

        public void SetHandleData(THandle handle, in THandleData data)
        {
            GetValidPairRef(handle).HandleData = data;
        }
        public unsafe void ModifyHandleData(THandle handle, delegate* <in THandleData, void> modifier)
        {
            modifier(in GetValidPairRef(handle).HandleData);
        }

        public THandleData GetAndFreeHandle(THandle handle)
        {
            ref var pair = ref GetValidPairRef(handle);
            pair.IsAllocated = false;
            return pair.HandleData;
        }

        public void FreeHandle(THandle handle)
        {
            GetValidPairRef(handle).IsAllocated = false;
        }
    }
}
