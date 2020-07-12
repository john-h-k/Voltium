#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using Voltium.Core;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;

namespace Voltium.RenderEngine
{
    /// <summary>
    /// Represents a render pass. This type cannot be inherited from directly,
    /// use <see cref="ComputeRenderPass"/> or <see cref="GraphicsRenderPass"/> instead
    /// </summary>
    public abstract class RenderPass
    {
        // prevent people directly inheriting from render pass
        private protected RenderPass() { }

        /// <summary>
        /// Registers the pass with the <see cref="RenderGraph"/>, by stating all its dependencies and resources it creates
        /// </summary>
        /// <param name="builder">The <see cref="RenderPassBuilder"/> used to build the pass</param>
        public abstract void Register(ref RenderPassBuilder builder);

        /// <summary>
        /// The <see cref="OutputDesc"/> produced by this pass, if any
        /// </summary>
        public virtual OutputDesc Output => OutputDesc.None;
    }

    public struct OutputDesc
    {
        public static OutputDesc None => new OutputDesc { Type = OutputType.None };

        public static OutputDesc FromOutput(OutputType type, Output output)
        {
            var back = output.BackBuffer;
            return CreateTexture(type, back.Width, back.Height, back.DepthOrArraySize);
         }

        public static OutputDesc CreateTexture(OutputType type, ulong width, uint height = 1, ushort depthOrArraySize = 1)
            => new OutputDesc { ResourceType = ResourceType.Texture, Type = type, TextureWidth = width, TextureHeight = height, TextureDepthOrArraySize = depthOrArraySize };


        public static OutputDesc CreateBuffer(OutputType type, ulong length)
            => new OutputDesc { ResourceType = ResourceType.Texture, Type = type, BufferLength = length };

        internal OutputType Type;
        internal ResourceType ResourceType;
        internal ulong BufferLength;

        internal ulong TextureWidth;
        internal uint TextureHeight;
        internal ushort TextureDepthOrArraySize;
    }

    public abstract class ComputeRenderPass : RenderPass
    {
        protected ComputeRenderPass() { }
        public abstract void Record(ref ComputeContext context, ref ComponentResolver resolver);

        public ComputePipelineStateObject? DefaultPipelineState { get; protected set; }

    }

    public abstract class GraphicsRenderPass : RenderPass
    {
        protected GraphicsRenderPass() { }
        public abstract void Record(ref GraphicsContext context, ref ComponentResolver resolver);

        public PipelineStateObject? DefaultPipelineState { get; protected set; }

    }
}
