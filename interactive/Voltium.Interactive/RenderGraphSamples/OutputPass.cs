using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Core;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Devices;
using Voltium.RenderEngine;

namespace Voltium.Interactive.RenderGraphSamples
{
    [ExpectsComponent(typeof(PipelineResources), typeof(PipelineSettings))]
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
            var resources = resolver.GetComponent<PipelineResources>();
            var settings = resolver.GetComponent<PipelineSettings>();

            var sceneColor = resolver.ResolveResource(resources.SceneColor);

            if (settings.Msaa != MultisamplingDesc.None)
            {
                context.ResolveSubresource(sceneColor, _output.BackBuffer);
            }
            else
            {
                context.CopyResource(sceneColor, _output.BackBuffer);
            }

            context.ResourceTransition(_output.BackBuffer, ResourceState.Present);
        }

        public override void Register(ref RenderPassBuilder builder, ref Resolver resolver)
        {
            var resources = resolver.GetComponent<PipelineResources>();
            var settings = resolver.GetComponent<PipelineSettings>();

            if (settings.Msaa != MultisamplingDesc.None)
            {
                builder.MarkUsage(resources.SceneColor, ResourceState.ResolveSource);
            }
            else
            {
                builder.MarkUsage(resources.SceneColor, ResourceState.CopySource);
            }
        }
    }
}
