using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Toolkit.HighPerformance.Extensions;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using static TerraFX.Interop.D3D12_DESCRIPTOR_HEAP_FLAGS;
using static TerraFX.Interop.D3D12_DESCRIPTOR_HEAP_TYPE;

namespace Voltium.Core
{
    public enum ShaderComponentMapping
    {
        Default,
        Red,
        Green,
        Blue,
        Alpha,
        One,
        Zero
    }

    public struct DescriptorAllocation
    {
        internal DescriptorSetHandle Handle;
        private Disposal<DescriptorSetHandle> _dispose;
        public readonly DescriptorType Type;
        public readonly uint Length;

        public DescriptorAllocation(uint length, DescriptorType type, DescriptorSetHandle handle, Disposal<DescriptorSetHandle> dispose)
        {
            Length = length;
            Type = type;
            Handle = handle;
            _dispose = dispose;
        }

        public void Dispose() => _dispose.Dispose(ref Handle);
    }

    public readonly struct DescriptorSpan
     {
        public readonly int Length => (int)_length;

        internal readonly D3D12_GPU_DESCRIPTOR_HANDLE Gpu;
        private readonly uint _length;
        internal readonly ushort IncrementSize;
        internal readonly byte Type;

        internal DescriptorSpan(D3D12_GPU_DESCRIPTOR_HANDLE gpu, uint length, ushort incrementSize, byte type)
        {
            _length = length;
            Gpu = gpu;
            IncrementSize = incrementSize;
            Type = type;
        }

        public DescriptorHandle this[uint index] => this[(int)index];
        public DescriptorHandle this[int index] => new DescriptorHandle(OffsetGpu(index));

        private D3D12_GPU_DESCRIPTOR_HANDLE OffsetGpu(int count) => Gpu.Offset(count, IncrementSize);

        public DescriptorSpan Slice(int start)
        {
            if ((uint)start > (uint)_length)
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(start));

            return new DescriptorSpan(OffsetGpu(start), _length - (uint)start, IncrementSize, Type);
        }

        public DescriptorSpan Slice(int start, int length)
        {
            if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)_length)
                ThrowHelper.ThrowArgumentOutOfRangeException("start/length");

            return new DescriptorSpan(OffsetGpu(start), (uint)length, IncrementSize, Type);
        }
    }

    public enum DescriptorAllocationHint
    {
        LongTerm,
        ShortTerm
    }
}
