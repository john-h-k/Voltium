using System;
using System.IO;
using System.Numerics;
using Voltium.Core.Devices;
using Voltium.Core.Pipeline;
using static TerraFX.Interop.D3D12_RESOURCE_STATES;

namespace Voltium.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    internal class ResourceStateInfo : Attribute
    {
        public ResourceStateInfo(Access access)
        {

        }
    }

    // DO NOT CHANGE! - must be matched by ResourceStateAnalyzer.cs
    internal enum Access
    {
        Opaque = 0,
        Read = 1,
        Write = 2,
        ReadWrite = Read | Write
    }

    /// <summary>
    /// Represents the state of a GPU resource
    /// </summary>
    [Flags]
    public enum ResourceState : uint
    {
        /// <summary>
        /// A state used when accessing resources across multiple <see cref="ExecutionContext"/>s
        /// </summary>
        [ResourceStateInfo(Access.ReadWrite)]
        Common = D3D12_RESOURCE_STATE_COMMON,

        /// <summary>
        /// The resource is being used by the swapchain for presenting
        /// </summary>
        [ResourceStateInfo(Access.ReadWrite)]
        Present = D3D12_RESOURCE_STATE_PRESENT,

        /// <summary>
        /// The resource is being used as a vertex buffer. This state is read-only
        /// </summary>
        [ResourceStateInfo(Access.Read)]
        VertexBuffer = D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER,

        /// <summary>
        /// The resource is being used as a constant buffer. This state is read-only
        /// </summary>
        [ResourceStateInfo(Access.Read)]
        ConstantBuffer = D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER,

        /// <summary>
        /// The resource is being used as a vertex or constant buffer. This state is read-only
        /// </summary>
        [ResourceStateInfo(Access.Read)]
        VertexOrConstantBuffer = D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER,

        /// <summary>
        /// The resource is being used as an index buffer. This state is read-only
        /// </summary>
        [ResourceStateInfo(Access.Read)]
        IndexBuffer = D3D12_RESOURCE_STATE_INDEX_BUFFER,

        /// <summary>
        /// The resource is being used as a render target. This state is write-only
        /// </summary>
        [ResourceStateInfo(Access.Write)]
        RenderTarget = D3D12_RESOURCE_STATE_RENDER_TARGET,

        /// <summary>
        /// The resource is being used as an unordered access resource. This state is read/write
        /// </summary>
        [ResourceStateInfo(Access.ReadWrite)]
        UnorderedAccess = D3D12_RESOURCE_STATE_UNORDERED_ACCESS,

        /// <summary>
        /// The resource is being written to as a depth buffer, e.g when <see cref="DepthStencilDesc.EnableDepthTesting"/> is <see langword="true"/>.
        /// This state is read/write and cannot be combined with <see cref="DepthRead"/>
        /// </summary>
        [ResourceStateInfo(Access.ReadWrite)]
        DepthWrite = D3D12_RESOURCE_STATE_DEPTH_WRITE,

        /// <summary>
        /// The resource is being read from as a depth buffer.
        /// This state is read-only and cannot be combined with <see cref="DepthWrite"/>
        /// </summary>
        [ResourceStateInfo(Access.Read)]
        DepthRead = D3D12_RESOURCE_STATE_DEPTH_READ,

        /// <summary>
        /// The resource is being used by a shader other than <see cref="ShaderType.Pixel"/>
        /// </summary>
        [ResourceStateInfo(Access.Read)]
        NonPixelShaderResource = D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE,

        /// <summary>
        /// The resource is being used by a <see cref="ShaderType.Pixel"/> shader
        /// </summary>
        [ResourceStateInfo(Access.Read)]
        PixelShaderResource = D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE,

        ///// <summary>
        ///// The resource is being used as a stream-out destination. This is a write-only state
        ///// </summary>
        //[ResourceStateInfo(Access.Write)]
        //StreamOut = D3D12_RESOURCE_STATE_STREAM_OUT,

        /// <summary>
        /// The resource is being used as an indirect argument. This is a read-only state
        /// </summary>
        [ResourceStateInfo(Access.Read)]
        IndirectArgument = D3D12_RESOURCE_STATE_INDIRECT_ARGUMENT,

        /// <summary>
        /// The resource is being used for GPU predication
        /// </summary>
        [ResourceStateInfo(Access.Read)]
        Predication = D3D12_RESOURCE_STATE_PREDICATION,

        /// <summary>
        /// The resource is being used as the destination of a GPU copy operation. This is the starting
        /// state for a <see cref="MemoryAccess.CpuReadback"/> resource
        /// </summary>
        [ResourceStateInfo(Access.Write)]
        CopyDestination = D3D12_RESOURCE_STATE_COPY_DEST,

        /// <summary>
        /// The resource is being used as the source of a GPU copy operation
        /// </summary>
        [ResourceStateInfo(Access.Read)]
        CopySource = D3D12_RESOURCE_STATE_COPY_SOURCE,

        /// <summary>
        /// The resource can be read as several other <see cref="ResourceState"/>s. This is the starting
        /// state for a <see cref="MemoryAccess.CpuUpload"/> resource
        /// </summary>
        [ResourceStateInfo(Access.Read)]
        GenericRead = D3D12_RESOURCE_STATE_GENERIC_READ,

        /// <summary>
        /// The resource is being used as the destination of a MSAA resolve operation
        /// </summary>
        [ResourceStateInfo(Access.Write)]
        ResolveDestination = D3D12_RESOURCE_STATE_RESOLVE_DEST,

        /// <summary>
        /// The resource is being used as the source of a MSAA resolve operation
        /// </summary>
        [ResourceStateInfo(Access.Read)]
        ResolveSource = D3D12_RESOURCE_STATE_RESOLVE_SOURCE,

        /// <summary>
        /// The resource is being used as the source image to determine variable shading rate (VRS) granularity
        /// </summary>
        [ResourceStateInfo(Access.Read)]
        VariableShadeRateSource = D3D12_RESOURCE_STATE_SHADING_RATE_SOURCE,

        /// <summary>
        /// The resource is being used as a raytracing acceleration structure. This state can't be moved into or out from, and must persist across the resource's lifetime
        /// </summary>
        [ResourceStateInfo(Access.Opaque)]
        RayTracingAccelerationStructure = D3D12_RESOURCE_STATE_RAYTRACING_ACCELERATION_STRUCTURE,

        /// <summary>
        /// A bitwise-or of all read-only resource states
        /// </summary>
        [ResourceStateInfo(Access.Read)]
        AllReadOnlyFlags = VertexBuffer | ConstantBuffer | IndexBuffer | DepthRead | NonPixelShaderResource | PixelShaderResource | IndirectArgument | CopySource | ResolveSource | VariableShadeRateSource,
        // this currently excludes video states

#pragma warning disable VR0000, VR0001, VR0002
        /// <summary>
        /// A bitwise-or of all write-only or read-write states
        /// </summary>
        [ResourceStateInfo(Access.ReadWrite)]
        AllWritableFlags = RenderTarget | UnorderedAccess | DepthWrite /* | StreamOut */ | CopyDestination | ResolveDestination
#pragma warning restore VR0000, VR0001, VR0002
    }

    /// <summary>
    /// Extensions on <see cref="ResourceState"/>
    /// </summary>
    public static class ResourceStateFlagExtensions
    {

        /// <summary>
        /// Determines if a given <see cref="ResourceState"/> contains only read states
        /// </summary>
        public static bool IsReadOnly(this ResourceState flags) => (flags & ResourceState.AllReadOnlyFlags) == flags;

        /// <summary>
        /// Determines if a given <see cref="ResourceState"/> contains a write state
        /// </summary>
        public static bool HasWriteFlag(this ResourceState flags) => (flags & ResourceState.AllWritableFlags) != 0 || /* common is a writable state */ flags == 0;

        /// <summary>
        /// Determines if a given <see cref="ResourceState"/> contains a read state
        /// </summary>
        public static bool HasReadOnlyFlags(this ResourceState flags) => (flags & ResourceState.AllReadOnlyFlags) != 0;

        /// <summary>
        /// Determines if a given <see cref="ResourceState"/> contains an unorded access
        /// </summary>
        public static bool HasUnorderedAccess(this ResourceState flags) => (flags & ResourceState.UnorderedAccess) != 0;

        /// <summary>
        /// Determines if a given <see cref="ResourceState"/> is invalid, which means it contains both write and read states, or multiple write states
        /// </summary>
        public static bool IsInvalid(this ResourceState flags)
        {
            int numWriteFlags = BitOperations.PopCount((uint)(flags & ResourceState.AllWritableFlags));

            // you can only have 1 or 0 write flags
            // you can have multiple read flags, but only if there are no write flags
            return numWriteFlags > 1 || (numWriteFlags == 1 && flags.HasReadOnlyFlags());
        }
    }
}
