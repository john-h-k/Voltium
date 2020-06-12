using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Core;
using Voltium.Core.Pipeline;
using Voltium.RenderEngine.RenderGraph;

namespace Voltium.RenderEngine.Passes
{
    abstract class RenderPass
    {
        public abstract void Register(ref RenderPassBuilder builder);

        public PipelineStateObject? DefaultPipelineState { get; protected set; }

        public abstract void Record(ref GraphicsContext context, ref ComponentResolver resolver);
    }

    abstract class PresentPass
    {
        public abstract void Register(ref RenderPassBuilder builder, TexHandle backBuffer);

        public PipelineStateObject? DefaultPipelineState { get; protected set; }

        public abstract void Record(ref GraphicsContext context, ref ComponentResolver resolver);
    }
}
