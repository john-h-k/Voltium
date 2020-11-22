using TerraFX.Interop;

namespace Voltium.Core.Memory
{
    /// <summary>
    /// Flags used in resource creation
    /// </summary>
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
        /// Prevents the resource being used by shaders
        /// </summary>
        DenyShaderResource = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_DENY_SHADER_RESOURCE,

        /// <summary>
        /// Allows the resource to be used as a stream out resource
        /// </summary>
        AllowStreamOut = 1 << 30
    }


    internal static class ResourceFlagsExtensions
    {
        public static bool IsShaderWritable(this ResourceFlags flags)
            => flags.HasFlag(ResourceFlags.AllowRenderTarget) || flags.HasFlag(ResourceFlags.AllowUnorderedAccess) || flags.HasFlag(ResourceFlags.AllowStreamOut);
    }
}
