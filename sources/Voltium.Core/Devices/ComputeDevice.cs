using System;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Core.Devices.Shaders;
using Voltium.Core.Infrastructure;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;
using Buffer = Voltium.Core.Memory.Buffer;

using SysDebug = System.Diagnostics.Debug;
using Voltium.Core.Queries;
using Voltium.Core.Exceptions;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using Voltium.Core.NativeApi;

namespace Voltium.Core.NativeApi
{
    public struct CommandBuffer
    {
        public PipelineHandle? FirstPipeline;
        public ReadOnlyMemory<byte> Buffer;
    }

    public enum FenceFlags
    {
        None = D3D12_FENCE_FLAGS.D3D12_FENCE_FLAG_NONE,
        ProcessShared = D3D12_FENCE_FLAGS.D3D12_FENCE_FLAG_SHARED,
        AdapterShared = D3D12_FENCE_FLAGS.D3D12_FENCE_FLAG_SHARED_CROSS_ADAPTER
    }

    /// <summary>
    ///
    /// </summary>
    public unsafe partial class ComputeDevice
    {
        private protected INativeDevice _device;

        /// <summary>
        /// The <see cref="Adapter"/> this device uses
        /// </summary>
        public Adapter Adapter { get; private set; }

        /// <summary>
        /// The default allocator for the device
        /// </summary>
        public ComputeAllocator Allocator { get; private protected set; }

        /// <summary>
        /// Creates a new query heap on the device
        /// </summary>
        /// <param name="type">The <see cref="QuerySetType"/> for the heap</param>
        /// <param name="numQueries">The number of queries  for the heap to hold</param>
        /// <returns></returns>
        public QuerySet CreateQuerySet(QuerySetType type, uint numQueries)
        {
            static void Dispose(object o, ref QuerySetHandle handle)
            {
                Debug.Assert(o is ComputeDevice);
                Unsafe.As<ComputeDevice>(o)._device.DisposeQuerySet(handle);
            }

            return new QuerySet(_device.CreateQuerySet(type, numQueries), numQueries, type, new(this, &Dispose));
        }

        [DoesNotReturn]
        internal void ThrowGraphicsException(string message, Exception? inner = null) => throw new GraphicsException(this, message, inner);


        /// <summary>
        /// The number of physical adapters, referred to as nodes, that the device uses
        /// </summary>
        public uint NodeCount { get; }

        /// <summary>
        /// The highest <see cref="ShaderModel"/> supported by this device
        /// </summary>
        public ShaderModel HighestSupportedShaderModel { get; private set; }

        /// <summary>
        /// Whether DXIL is supported, rather than the old DXBC bytecode form.
        /// This is equivalent to cheking if <see cref="HighestSupportedShaderModel"/> supports shader model 6
        /// </summary>
        public bool IsDxilSupported => HighestSupportedShaderModel.IsDxil;

        public static ComputeDevice Create<TNativeDevice>(TNativeDevice device) where TNativeDevice : INativeDevice
        {
            return new(device);
        }

        private protected ComputeDevice(INativeDevice device)
        {
            _device = device;
            Allocator = new(this);
        }

        public (ulong Alignment, ulong Length) GetAllocationInfo(in TextureDesc desc) => _device.GetTextureAllocationInfo(desc);

        public Heap CreateHeap(ulong size, in HeapInfo info)
        {
            static void Dispose(object o, ref HeapHandle handle) => Unsafe.As<ComputeDevice>(o)._device.DisposeHeap(handle);

            var heap = _device.CreateHeap(size, info);

            return new Heap(heap, size, info, new(this, &Dispose));
        }

        public Buffer AllocateBuffer(BufferDesc desc, MemoryAccess access)
        {
            var buffer = _device.AllocateBuffer(desc, access);

            static void Dispose(object o, ref BufferHandle handle)
            {
                SysDebug.Assert(o is ComputeDevice);
                Unsafe.As<ComputeDevice>(o)._device.DisposeBuffer(handle);
            }

            return new Buffer(desc.Length,_device.GetDeviceVirtualAddress(buffer), _device.Map(buffer), buffer, new(this, &Dispose));
        }

        public Buffer AllocateBuffer(BufferDesc desc, in Heap heap, ulong offset)
        {
            var buffer = _device.AllocateBuffer(desc, heap.Handle, offset);

            static void Dispose(object o, ref BufferHandle handle)
            {
                SysDebug.Assert(o is ComputeDevice);
                Unsafe.As<ComputeDevice>(o)._device.DisposeBuffer(handle);
            }

            return new Buffer(desc.Length, _device.GetDeviceVirtualAddress(buffer), _device.Map(buffer), buffer, new(this, &Dispose));
        }

        internal Buffer AllocateBuffer(BufferDesc desc, in Heap heap, ulong offset, Disposal<BufferHandle> dispose)
        {
            var buffer = _device.AllocateBuffer(desc, heap.Handle, offset);

            return new Buffer(desc.Length, _device.GetDeviceVirtualAddress(buffer), _device.Map(buffer), buffer, dispose);
        }

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            Allocator.Dispose();
        }
    }
}
