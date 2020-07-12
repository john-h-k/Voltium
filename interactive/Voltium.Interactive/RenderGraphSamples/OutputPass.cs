using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Core;
using Voltium.Core.Devices;
using Voltium.RenderEngine;

namespace Voltium.Interactive.RenderGraphSamples
{
    internal class OutputPass : GraphicsRenderPass
    {
        public override OutputDesc Output { get; }

        private Output _output;

        public OutputPass(Output output)
        {
            _output = output;
            Output = OutputDesc.FromOutput(OutputClass.Primary, _output);
        }

        public override void Record(ref GraphicsContext context, ref Resolver resolver)
        {
            var sceneColor = resolver.ResolveResource(resolver.ResolveComponent<PipelineResources>().SceneColor);
            context.CopyResource(sceneColor, _output.BackBuffer);
            context.ResourceTransition(_output.BackBuffer, ResourceState.Present);
        }

        public override void Register(ref RenderPassBuilder builder, ref Resolver resolver)
        {
            builder.MarkUsage(resolver.ResolveComponent<PipelineResources>().SceneColor, ResourceState.CopyDestination);
        }
    }
}
