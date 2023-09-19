using System;
using System.Buffers;
using System.Collections.Generic;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Allocators;
using Voltium.Common;
using Voltium.Core.Contexts;
using Voltium.Core.Memory;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core.Devices
{
    public unsafe partial class ComputeDevice
    {
        // this fence is specifically used by the device for MakeResidentAsync. unrelated to queue fences
        //private D3D12Fence _residencyFence;
        //private ulong _lastFenceSignal;

        ///// <summary>
        ///// Asynchronously makes <paramref name="resource"/> resident on the device
        ///// </summary>
        ///// <param name="resource">The resource to make resident</param>
        ///// <returns>A <see cref="GpuTask"/> that can be used to work out when the resource is resident</returns>
        //public GpuTask MakeResidentAsync(Buffer resource) => MakeResidentAsync((ID3D12Pageable*)_mapper.GetResourcePointer(resource.Handle));

        ///// <inheritdoc cref="MakeResidentAsync(Buffer)"/>
        //public GpuTask MakeResidentAsync(in Texture resource) => MakeResidentAsync((ID3D12Pageable*)_mapper.GetResourcePointer(resource.Handle));

        ///// <inheritdoc cref="MakeResidentAsync(Buffer)"/>
        //public GpuTask MakeResidentAsync(RaytracingAccelerationStructure resource) => MakeResidentAsync((ID3D12Pageable*)_mapper.GetResourcePointer(resource.Handle));

        //private GpuTask MakeResidentAsync(ID3D12Pageable* pageable)
        //{
        //    var newValue = Interlocked.Increment(ref _lastFenceSignal);

        //    ThrowIfFailed(As<ID3D12Device3>()->EnqueueMakeResident(
        //        D3D12_RESIDENCY_FLAGS.D3D12_RESIDENCY_FLAG_DENY_OVERBUDGET,
        //        1,
        //        &pageable,
        //        _residencyFence.GetPointer(),
        //        newValue
        //    ));

        //    return new GpuTask(_residencyFence, newValue);
        //}

        //// TODO batched versions
    }
}
