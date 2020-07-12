#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using Voltium.Core;
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
        /// <param name="resolver"></param>
        public abstract void Register(ref RenderPassBuilder builder, ref Resolver resolver);

        /// <summary>
        /// The <see cref="OutputDesc"/> produced by this pass, if any
        /// </summary>
        public virtual OutputDesc Output => OutputDesc.None;
    }

    public abstract class ComputeRenderPass : RenderPass
    {
        protected ComputeRenderPass() { }
        public abstract void Record(ref ComputeContext context, ref Resolver resolver);

        public ComputePipelineStateObject? DefaultPipelineState { get; protected set; }

    }

    public abstract class GraphicsRenderPass : RenderPass
    {
        protected GraphicsRenderPass() { }
        public abstract void Record(ref GraphicsContext context, ref Resolver resolver);

        public PipelineStateObject? DefaultPipelineState { get; protected set; }

    }
}
