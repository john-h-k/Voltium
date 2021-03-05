using System;
using TerraFX.Interop;

namespace Voltium.Core.Memory
{
    /// <summary>
    /// Flags used in resource creation
    /// </summary>
    [Flags]
    public enum ResourceFlags : uint
    {
        /// <summary>
        /// None
        /// </summary>
        None = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_NONE,

        /// <summary>
        /// Allows the resource to be used as a depth stencil. This is only relevant if the resource is a texture
        /// </summary>
        AllowDepthStencil = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL,

        /// <summary>
        /// Allows the resource to be used as a render target
        /// </summary>
        AllowRenderTarget = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET,

        /// <summary>
        /// Allows the resource to be used as an unordered access resource
        /// </summary>
        AllowUnorderedAccess = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS,

        /// <summary>
        /// Allows the resource being simultaneously used on multiple queues at the same time, provided only one queue is writing
        /// and no queues read the pixels being written
        /// </summary>
        AllowSimultaneousAccess = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_ALLOW_SIMULTANEOUS_ACCESS,

        /// <summary>
        /// Prevents the resource being used by shaders
        /// </summary>
        DenyShaderResource = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_DENY_SHADER_RESOURCE
    }


    internal static class ResourceFlagsExtensions
    {
        public static bool IsShaderWritable(this ResourceFlags flags)
            => flags.HasFlag(ResourceFlags.AllowRenderTarget) || flags.HasFlag(ResourceFlags.AllowUnorderedAccess);
    }
}
