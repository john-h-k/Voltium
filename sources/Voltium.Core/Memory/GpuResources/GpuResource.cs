using System;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Memory;

namespace Voltium.Core.GpuResources
{
    /// <summary>
    /// Represents a GPU resource
    /// </summary>
    internal unsafe class GpuResource : CriticalFinalizerObject, IDisposable
    {
        internal GpuResource(
            ComPtr<ID3D12Resource> resource,
            GpuResourceDesc desc,
            ulong size,
            ulong offset,
            AllocatorHeap heap,
            GpuAllocator allocator
        )
        {
            _value = resource.Move();
            State = desc.InitialState;
            ResourceFormat = desc.Format;
            Type = desc.GpuMemoryType;
            _size = size;
            _offset = offset;
            _heap = heap;
            _allocator = allocator;

            if (desc.ResourceFormat.D3D12ResourceDesc.Dimension == D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER)
            {
                GpuAddress = UnderlyingResource->GetGPUVirtualAddress();
            }
        }

        private GpuResource() { }

        // this is a hack. TODO make it right
        internal static GpuResource FromBackBuffer(
            ComPtr<ID3D12Resource> resource
        )
        {
            var desc = resource.Get()->GetDesc();
            return new GpuResource
            {
                _value = resource.Move(),
                Type = (GpuMemoryKind)(-1) /* TODO wtf should we have here */,
                ResourceFormat = (DataFormat)desc.Format,
                State = (ResourceState)D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COMMON
            };
        }

        /// <summary>
        /// The type of the resource
        /// </summary>
        public GpuMemoryKind Type { get; private set; }

        /// <summary>
        /// The format of the buffer, if typed, else <see cref="DataFormat.Unknown"/>
        /// </summary>
        public DataFormat ResourceFormat { get; private set; }

        /// <summary>
        /// The underlying value of the resource
        /// </summary>
        public ID3D12Resource* UnderlyingResource => _value.Get();

        /// <summary>
        /// The current state of the resource
        /// </summary>
        public ResourceState State { get; internal set; }

        private ComPtr<ID3D12Resource> _value;

        /// <summary>
        /// The GPU address of the underlying resource
        /// </summary>
        public ulong GpuAddress { get; private set; }

        /// <summary>
        /// The CPU address of the underlying resource. This may be null if the resource
        /// is unmapped or not CPU accessible
        /// </summary>
        public unsafe void* CpuAddress { get; private set; }

        /// <summary>
        /// A <see cref="Span{T}"/> encompassing the buffer data. This may be empty if the resource
        /// is unmapped or not CPU accessible
        /// </summary>
        public Span<byte> CpuData => new Span<byte>(CpuAddress, (int)GetBufferSize());

        private GpuAllocator? _allocator;
        private AllocatorHeap _heap;
        private ulong _size;
        private ulong _offset;

        internal AllocatorHeap GetAllocatorHeap() => _heap;

        internal ulong GetOffsetFromUnderlyingResource() => _offset;


        /// <summary>
        /// The size of the allocation
        /// </summary>
        public ulong Size => _size;

        /// <summary>
        /// Retuns a <see cref="ScopedResourceMap{T}"/> that allows a <see cref="Map"/> call to be scoped
        /// </summary>
        /// <param name="subresource">The subresource index to map</param>
        public unsafe ScopedResourceMap<T> MapScoped<T>(uint subresource)
        {
            Map(subresource);
            return new ScopedResourceMap<T>(UnderlyingResource, subresource, CpuAddress, GetBufferSize());
        }

        /// <summary>
        /// If the resource is not currently mapped, maps the resource
        /// </summary>
        /// <param name="subresource">The subresource index to map</param>
        public unsafe void Map(uint subresource)
        {
            Debug.Assert(Type != GpuMemoryKind.GpuOnly);

            if (CpuAddress == null)
            {
                // Apparently the range for map and unmap are for debugging purposes and yield no perf benefit. Maybe we could still support em
                void* pData;
                Guard.ThrowIfFailed(UnderlyingResource->Map(subresource, null, &pData));
                CpuAddress = (byte*)pData + _offset;
            }
        }

        /// <summary>
        /// If the resource is currently mapped, unmaps the resource
        /// </summary>
        /// <param name="subresource">The subresource index to unmap</param>
        public unsafe void Unmap(uint subresource)
        {
            Debug.Assert(Type != GpuMemoryKind.GpuOnly);

            if (CpuAddress != null)
            {
                // Apparently the range for map and unmap are for debugging purposes and yield no perf benefit. Maybe we could still support em
                UnderlyingResource->Unmap(subresource, null);
                CpuAddress = null;
            }
        }

        internal uint GetBufferSize()
        {
            var desc = UnderlyingResource->GetDesc();
            Debug.Assert(desc.Dimension == D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_BUFFER);
            return (uint)(desc.Width * desc.Height * desc.DepthOrArraySize);
        }

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose()
        {
            if (_allocator is object)
            {
                _allocator.Return(this);
            }
            else
            {
                _value.Dispose();
            }
            GC.SuppressFinalize(this);
        }

#if TRACE_DISPOSABLES || DEBUG
        /// <summary>
        /// go fuck yourself roslyn why the fucking fuckeroni do finalizers need xml comments fucking fuck off fucking twatty compiler
        /// </summary>
        ~GpuResource()
        {
            Guard.MarkDisposableFinalizerEntered();
            ThrowHelper.NeverReached();
        }
#endif
    }
}
