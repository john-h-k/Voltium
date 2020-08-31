using System;
using System.Runtime.ConstrainedExecution;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Devices;

namespace Voltium.Core.Memory
{
    /// <summary>
    /// Represents a GPU resource
    /// </summary>
    internal unsafe sealed class GpuResource : IDisposable, IInternalD3D12Object, IEvictable
    {
        ID3D12Object* IInternalD3D12Object.GetPointer() => (ID3D12Object*)GetResourcePointer();
        ID3D12Pageable* IEvictable.GetPageable() => (ID3D12Pageable*)GetResourcePointer();
        bool IEvictable.IsBlittableToPointer => false; // we are a class

        internal ID3D12Object* GetPointer() => ((IInternalD3D12Object)this).GetPointer();

        internal GpuResource(
            ComputeDevice device,
            UniqueComPtr<ID3D12Resource> resource,
            in InternalAllocDesc desc,
            GpuAllocator? allocator,
            int heapIndex = -1 /* no relevant heap block */,
            HeapBlock block = default
        )
        {
            _device = device;
            _value = resource.Move();
            State = (ResourceState)desc.InitialState;
            ResourceFormat = (DataFormat)desc.Desc.Format;
            HeapIndex = heapIndex;
            Flags = desc.Desc.Flags;
            Block = block;
            _allocator = allocator;
        }

        private GpuResource() { }

        // this is a hack. TODO make it right
        internal static GpuResource FromBackBuffer(
            UniqueComPtr<ID3D12Resource> resource
        )
        {
            var desc = resource.Ptr->GetDesc();
            return new GpuResource
            {
                _value = resource.Move(),
                ResourceFormat = (DataFormat)desc.Format,
                State = (ResourceState)D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COMMON
            };
        }

        /// <summary>
        /// The format of the buffer, if typed, else <see cref="DataFormat.Unknown"/>
        /// </summary>
        public DataFormat ResourceFormat;

        public unsafe ID3D12Resource* GetResourcePointer() => _value.Ptr;

        /// The current state of the resource
        public ResourceState State;

        /// Whether a resource transition was began on this resource, making it temporarily inaccessible
        public bool TransitionBegan;

        private UniqueComPtr<ID3D12Resource> _value;
        public D3D12_RESOURCE_FLAGS Flags;

        // Null if the resource is unmapped or not CPU accessible (default heap)
        public unsafe void* CpuAddress;


        private ComputeDevice _device = null!;

        private GpuAllocator? _allocator;
        public int HeapIndex;
        public HeapBlock Block;

        /// <summary>
        /// If the resource is not currently mapped, maps the resource
        /// </summary>
        /// <param name="subresource">The subresource index to map</param>
        public unsafe void* Map(uint subresource)
        {
            // Apparently the range for map and unmap are for debugging purposes and yield no perf benefit. Maybe we could still support em
            void* pData;
            _device.ThrowIfFailed(GetResourcePointer()->Map(subresource, null, &pData));
            return pData;
        }

        /// <summary>
        /// If the resource is currently mapped, unmaps the resource
        /// </summary>
        /// <param name="subresource">The subresource index to unmap</param>
        public unsafe void Unmap(uint subresource)
        {
            if (!_value.Exists)
            {
                return;
            }
            // Apparently the range for map and unmap are for debugging purposes and yield no perf benefit. Maybe we could still support em
            GetResourcePointer()->Unmap(subresource, null);
        }

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose()
        {
            if (_allocator is not null)
            {
                _allocator.Return(this);
            }
            else
            {
                _value.Dispose();
            }

            // Prevent use after free
            _value = default;
#if TRACE_DISPOSABLES || DEBUG
            GC.SuppressFinalize(this);
#endif
        }


#if TRACE_DISPOSABLES || DEBUG
        /// <summary>
        /// go fuck yourself roslyn why the fucking fuckeroni do finalizers need xml comments fucking fuck off fucking twatty compiler
        /// </summary>
        ~GpuResource()
        {
            var name = DebugHelpers.GetName(this);
            Guard.MarkDisposableFinalizerEntered();
        }
#endif
    }
}
