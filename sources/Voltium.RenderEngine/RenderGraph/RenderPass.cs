using Voltium.Core;
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

    /// <summary>
    /// Represents a compute render pass. This allows execution on the async compute queue, but does not guarantee it.
    /// Compute passes can perform a limited set of operations compared to <see cref="GraphicsRenderPass"/>s
    /// </summary>
    public abstract class ComputeRenderPass : RenderPass
    {
        /// <summary>
        /// Creates a new <see cref="ComputeRenderPass"/>
        /// </summary>
        protected ComputeRenderPass() { }

        /// <summary>
        /// The method which records the pass's commands
        /// </summary>
        /// <param name="context">The <see cref="ComputeContext"/> the pass records to</param>
        /// <param name="resolver">The <see cref="Resolver"/> the pass uses to resolve components and resources</param>
        public abstract void Record(ComputeContext context, ref Resolver resolver);

        /// <summary>
        /// The <see cref="ComputePipelineStateObject"/> that this pass expects at the start of
        /// <see cref="Record"/>
        /// </summary>
        public ComputePipelineStateObject? DefaultPipelineState { get; protected set; }

    }

    /// <summary>
    /// Represents a graphics render pass
    /// </summary>
    public abstract class GraphicsRenderPass : RenderPass
    {
        /// <summary>
        /// Creates a new <see cref="GraphicsRenderPass"/>
        /// </summary>
        protected GraphicsRenderPass() { }

        /// <summary>
        /// The method which records the pass's commands
        /// </summary>
        /// <param name="context">The <see cref="GraphicsContext"/> the pass records to</param>
        /// <param name="resolver">The <see cref="Resolver"/> the pass uses to resolve components and resources</param>
        public abstract void Record(GraphicsContext context, ref Resolver resolver);

        /// <summary>
        /// The <see cref="PipelineStateObject"/> that this pass expects at the start of
        /// <see cref="Record"/>
        /// </summary>
        public PipelineStateObject? DefaultPipelineState { get; protected set; }

    }
}
