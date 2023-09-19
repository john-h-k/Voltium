using System;
using System.Buffers;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Veldrid.MetalBindings;
using Voltium.Common;
using Voltium.Core.Contexts;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;
using Voltium.Core.Queries;
using System.Runtime.InteropServices;
using System.Collections.Immutable;
using Voltium.Core.NativeApi;
using Voltium.Allocators;
using System.Runtime.Versioning;

namespace Voltium.Core.Devices;

internal unsafe struct MetalBuffer
{
    public MTLBuffer Buffer;
    public MTLResourceOptions Flags;
    public void* CpuAddress;
    public ulong Length;
}

internal unsafe struct MetalHeap
{
    public MTLHeap Buffer;
    public MTLResourceOptions Flags;
    public void* CpuAddress;
    public ulong Length;
}

internal unsafe struct MetalHandleMapper
{
    public MetalHandleMapper(bool _)
    {
        const int capacity = 32;
        _buffers = new(capacity);
    }

    private GenerationHandleAllocator<BufferHandle, MetalBuffer> _buffers;


    public BufferHandle Create(in MetalBuffer data) => _buffers.AllocateHandle(data);
    public MetalBuffer GetInfo(BufferHandle handle) => _buffers.GetHandleData(handle);
    public MetalBuffer GetAndFree(BufferHandle handle) => _buffers.GetAndFreeHandle(handle);
}
