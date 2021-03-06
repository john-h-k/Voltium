using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Toolkit.HighPerformance.Extensions;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.NativeApi;
using static TerraFX.Interop.D3D12_DESCRIPTOR_HEAP_FLAGS;
using static TerraFX.Interop.D3D12_DESCRIPTOR_HEAP_TYPE;

namespace Voltium.Core
{

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
}
