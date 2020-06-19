using System.Diagnostics;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Common;
using static TerraFX.Interop.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.D3D12_RESOURCE_DIMENSION;

namespace Voltium.Core.GpuResources.OldStyle
{
    /// <summary>
    /// Represents a strongly-typed GPU default buffer
    /// </summary>
    /// <typeparam name="TElement">The type of the buffer element</typeparam>
    public unsafe class GpuBuffer<TElement> : GraphicsResource where TElement : struct
    {
        /// <summary>
        /// The size of each element
        /// </summary>
        public uint ElementSize => (uint)Unsafe.SizeOf<TElement>();

        /// <summary>
        /// The number of elements
        /// </summary>
        public uint ElementCount { get; private set; }

        /// <summary>
        /// The size, in bytes, of the buffer
        /// </summary>
        public uint BufferSize => ElementSize * ElementCount;

        /// <summary>
        /// Create a new default buffer with a specified capacity
        /// </summary>
        /// <param name="device">The device to create the buffer on</param>
        /// <param name="resourceDesc">The resource to create</param>
        /// <param name="state">The initial state of the resource</param>
        /// <param name="type">The type of the buffer</param>
        /// <param name="flags">Any additional flags used in heap creation</param>
        /// <param name="optimizedClearValue">If not <code>null</code>, the optimized clear value for the buffer</param>
        public unsafe GpuBuffer(
            ID3D12Device* device,
            D3D12_RESOURCE_DESC resourceDesc,
            D3D12_RESOURCE_STATES state,
            D3D12_HEAP_TYPE type,
            D3D12_HEAP_FLAGS flags = D3D12_HEAP_FLAG_NONE,
            D3D12_CLEAR_VALUE? optimizedClearValue = null
        )
        {
            ElementCount = checked((uint)(resourceDesc.Height * resourceDesc.Width * resourceDesc.Depth));

            Debug.Assert(device != null);

            Debug.Assert(optimizedClearValue is null || resourceDesc.Dimension != D3D12_RESOURCE_DIMENSION_BUFFER,
                "optimizedClearValue cannot be used with D3D12_RESOURCE_DIMENSION_BUFFER");

            //var sz = DXGISurfaceInfo.BitsPerPixel(resourceDesc.Format) / 8;
            //Guard.True(sz == ElementSize || ElementSize == 1, // 1 is untyped buffer
            //    $"Invalid TElement (size: {ElementSize}) for DXGI_FORMAT {resourceDesc.Format} (size: {sz})");
        }

    }
}
