using Voltium.Core.Managers;
using Voltium.Core.Pipeline;
using static TerraFX.Interop.D3D12_RESOURCE_STATES;

namespace Voltium.Core
{
    /// <summary>
    /// Represents the state of a GPU resource
    /// </summary>
    public enum ResourceState : uint
    {
        /// <summary>
        /// A state used when accessing resources across multiple <see cref="ExecutionContext"/>s
        /// </summary>
        Common = D3D12_RESOURCE_STATE_COMMON,

        /// <summary>
        /// The resource is being used by the swapchain for presenting
        /// </summary>
        Present = D3D12_RESOURCE_STATE_PRESENT,

        /// <summary>
        /// The resource is being used as a vertex buffer. This state is read-only
        /// </summary>
        VertexBuffer = D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER,

        /// <summary>
        /// The resource is being used as a constant buffer. This state is read-only
        /// </summary>
        ConstantBuffer = D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER,

        /// <summary>
        /// The resource is being used as a vertex or constant buffer. This state is read-only
        /// </summary>
        VertexOrConstantBuffer = D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER,

        /// <summary>
        /// The resource is being used as an index buffer. This state is read-only
        /// </summary>
        IndexBuffer = D3D12_RESOURCE_STATE_INDEX_BUFFER,

        /// <summary>
        /// The resource is being used as a render target. This state is write-only
        /// </summary>
        RenderTarget = D3D12_RESOURCE_STATE_RENDER_TARGET,

        /// <summary>
        /// The resource is being used as an unordered access resource. This state is read/write
        /// </summary>
        UnorderedAccess = D3D12_RESOURCE_STATE_UNORDERED_ACCESS,

        /// <summary>
        /// The resource is being written to as a depth buffer, e.g when <see cref="DepthStencilDesc.EnableDepthTesting"/> is <see langword="true"/>.
        /// This state is write-only and cannot be combined with <see cref="DepthRead"/>
        /// </summary>
        DepthWrite = D3D12_RESOURCE_STATE_DEPTH_WRITE,

        /// <summary>
        /// The resource is being read from as a depth buffer/
        /// This state is read-only and cannot be combined with <see cref="DepthWrite"/>
        /// </summary>
        DepthRead = D3D12_RESOURCE_STATE_DEPTH_READ,

        /// <summary>
        /// The resource is being used by a shader other than <see cref="ShaderType.Pixel"/>
        /// </summary>
        NonPixelShaderResource = D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE,

        /// <summary>
        /// The resource is being used by a <see cref="ShaderType.Pixel"/> shader
        /// </summary>
        PixelShaderResource = D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE,

        /// <summary>
        /// The resource is being used as a stream-out destination. This is a write-only state
        /// </summary>
        StreamOut = D3D12_RESOURCE_STATE_STREAM_OUT,

        /// <summary>
        /// The resource is being used as an indirect argument. This is a read-only state
        /// </summary>
        IndirectArgument = D3D12_RESOURCE_STATE_INDIRECT_ARGUMENT,

        /// <summary>
        /// The resource is being used for GPU predication
        /// </summary>
        Predication = D3D12_RESOURCE_STATE_PREDICATION,

        /// <summary>
        /// The resource is being used as the destination of a GPU copy operation
        /// </summary>
        CopyDestination = D3D12_RESOURCE_STATE_COPY_DEST,

        /// <summary>
        /// The resource is being used as the source of a GPU copy operation
        /// </summary>
        CopySource = D3D12_RESOURCE_STATE_COPY_SOURCE,

        /// <summary>
        /// The resource can be read as several other <see cref="ResourceState"/>s. This is the starting
        /// state for a <see cref="MemoryAccess.CpuUpload"/> resource
        /// </summary>
        GenericRead = D3D12_RESOURCE_STATE_GENERIC_READ,

        /// <summary>
        /// The resource is being used as the destination of a MSAA resolve operation
        /// </summary>
        ResolveDestination = D3D12_RESOURCE_STATE_RESOLVE_DEST,

        /// <summary>
        /// The resource is being used as the source of a MSAA resolve operation
        /// </summary>
        ResolveSource = D3D12_RESOURCE_STATE_RESOLVE_SOURCE
    }
}
