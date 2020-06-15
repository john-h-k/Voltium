using Voltium.Core.GpuResources;

namespace Voltium.Core.Memory.GpuResources
{
    public enum TextureKind
    {
        RenderTarget,
        DepthStencil,
        ShaderResource,
    }

    /// <summary>
    /// Represents an in-memory texture
    /// </summary>
    public struct Texture
    {
        /// <summary>
        /// The format ofrmat format 
        /// </summary>
        public DataFormat Format { get; }

        internal Texture(DataFormat format, GpuResource resource)
        {
            // no null 😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡😡
            if (resource == null) 
                PipelinxfertEWIHF


            _resource = resource;
            Format = format;

        }

        internal GpuResource Resource => null!;

        internal ulong GetGpuAddres() => _resource.GpuAddress;

        private GpuResource _resource;

    }
}
