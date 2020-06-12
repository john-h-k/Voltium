using System.Diagnostics;
using TerraFX.Interop;

namespace Voltium.Core.GpuResources
{
    /// <summary>
    /// A description used to fully represent an allocatable and constructable GPU resource
    /// </summary>
    public struct GpuResourceDesc
    {
        /// <summary>
        /// Creates a new <see cref="GpuResourceDesc"/>
        /// </summary>
        /// <param name="resourceFormat">The <see cref="GpuResourceFormat"/> describing the resource format and dimensions</param>
        /// <param name="gpuMemoryType">The type of GPU memory this resource will be on</param>
        /// <param name="initialState">The initial state of the resource</param>
        /// <param name="allocFlags">Any additional allocation flags passed to the allocator</param>
        /// <param name="clearValue">If this resource is a texture, the clear value for which it is optimised</param>
        /// <param name="heapFlags">Any additional flags used for creating or selecting the allocation heap</param>
        public GpuResourceDesc(
            GpuResourceFormat resourceFormat,
            GpuMemoryType gpuMemoryType,
            ResourceState initialState,
            GpuAllocFlags allocFlags = GpuAllocFlags.None,
            D3D12_CLEAR_VALUE? clearValue = null,
            D3D12_HEAP_FLAGS heapFlags = D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE
        )
        {
            ResourceFormat = resourceFormat;
            GpuMemoryType = gpuMemoryType;
            InitialState = initialState;
            AllocFlags = allocFlags;
            ClearValue = clearValue;
            HeapFlags = heapFlags;
        }

        /// <summary>
        /// The description of the resource type, format, and dimensions
        /// </summary>
        public GpuResourceFormat ResourceFormat;

        /// <summary>
        /// Any additional flags used during allocation of the resource
        /// </summary>
        public GpuAllocFlags AllocFlags;

        // TODO document best formats for this https://gpuopen.com/performance/#clears
        /// <summary>
        /// If not <c>null</c>, the clear value that the driver will optimise for.
        /// This is not valid for buffer resources
        /// </summary>
        public D3D12_CLEAR_VALUE? ClearValue;

        /// <summary>
        /// Any additional flags used during creation or selection of the heap during allocation,
        /// if this resource is placed
        /// </summary>
        public D3D12_HEAP_FLAGS HeapFlags;

        /// <summary>
        /// The state to create the resource as
        /// </summary>
        public ResourceState InitialState;

        /// <summary>
        /// The type of the underlying GPU memory
        /// </summary>
        public GpuMemoryType GpuMemoryType;

        /// <inheritdoc cref="GpuResourceFormat"/>
        public DataFormat Format => ResourceFormat.Format;
    }


    /// <summary>
    /// A description used to represent a generic, incomplete description of a GPU resource for creation.
    /// This type must be converted to <see cref="GpuResourceDesc"/> before use, which contains additional metadata
    /// necessary for allocation
    /// </summary>
    public struct GpuResourceFormat
    {
        /// <summary>
        /// Creates a new <see cref="GpuResourceFormat"/> representing a 1, 2, or 3 dimensional texture
        /// </summary>
        /// <param name="dimension">The dimension of the texture</param>
        /// <param name="format">The format of the texture</param>
        /// <param name="width">The width of the texture</param>
        /// <param name="height">The height of the texture</param>
        /// <param name="depth">The depth of the texture</param>
        /// <returns>A new <see cref="GpuResourceFormat"/> representing the texture</returns>
        public static GpuResourceFormat Texture(TextureDimension dimension, DataFormat format, ulong width, uint height = 1, ushort depth = 1)
                => TextureArray(dimension, format, width, height, depth); // Tex arrays and 3D texs share the same field for depth/arrayCount so same creation process


        /// <summary>
        /// Creates a new <see cref="GpuResourceFormat"/> representing an array of 1 or 2 dimension textures
        /// </summary>
        /// <param name="dimension">The dimension of the textures in the array</param>
        /// <param name="format">The format of the textures in the array</param>
        /// <param name="width">The width of each texture</param>
        /// <param name="height">The height of each texture</param>
        /// <param name="arrayCount">The number of elements in the texture array</param>
        /// <returns>A new <see cref="GpuResourceFormat"/> representing an array of 1 or 2 dimensional textures</returns>
        public static GpuResourceFormat TextureArray(TextureDimension dimension, DataFormat format, ulong width, uint height = 1, ushort arrayCount = 1)
        {
            // can't have 3D tex arrays
            Debug.Assert(dimension != TextureDimension.Tex3D);
            return new GpuResourceFormat(D3D12_RESOURCE_DESC.Tex3D((DXGI_FORMAT)format, width, height, arrayCount));
        }

        /// <summary>
        /// Creates a new <see cref="GpuResourceFormat"/> representing a depth stencil
        /// </summary>
        /// <param name="format">The format of the depth stencil</param>
        /// <param name="width">The width of the depth stencil</param>
        /// <param name="height">The height of the depth stencil</param>
        /// <returns>A new <see cref="GpuResourceFormat"/> representing a depth stencil</returns>
        public static GpuResourceFormat DepthStencil(DataFormat format, ulong width, uint height)
        {
            return new GpuResourceFormat(D3D12_RESOURCE_DESC.Tex2D((DXGI_FORMAT)format, width, height, mipLevels: 1, flags: D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL));
        }

        /// <summary>
        /// Creates a new <see cref="GpuResourceFormat"/> representing a buffer
        /// </summary>
        /// <param name="size">The size, in bytes, of the buffer</param>
        /// <returns>A new <see cref="GpuResourceFormat"/> representing a buffer</returns>
        public static GpuResourceFormat Buffer(ulong size)
        {
            return new GpuResourceFormat(D3D12_RESOURCE_DESC.Buffer(size));
        }

        /// <summary>
        /// The format of the resource
        /// </summary>
        public DataFormat Format => (DataFormat)D3D12ResourceDesc.Format;

        internal D3D12_RESOURCE_DESC D3D12ResourceDesc;

        /// <summary>
        /// Creates a new <see cref="GpuResourceFormat"/> from a <see cref="D3D12_RESOURCE_DESC"/>
        /// </summary>
        public GpuResourceFormat(D3D12_RESOURCE_DESC d3d12ResourceDesc)
        {
            D3D12ResourceDesc = d3d12ResourceDesc;
        }
    }

    /// <summary>
    /// Represents the allowed dimensions of a GPU texture
    /// </summary>
    public enum TextureDimension
    {
        /// <summary>
        /// The texture has 1 dimension
        /// </summary>
        Tex1D = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE1D,

        /// <summary>
        /// The texture has 2 dimensions
        /// </summary>
        Tex2D = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE2D,

        /// <summary>
        /// The texture has 3 dimensions. 3 dimensional textures cannot
        /// be used as texture arrays
        /// </summary>
        Tex3D = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE3D
    }
}
