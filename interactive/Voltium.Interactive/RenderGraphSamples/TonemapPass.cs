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
    [ExpectsComponent(typeof(PipelineResources))]
    internal class TonemapPass : GraphicsRenderPass
    {
        public override OutputDesc Output { get; }

        private Output2D _output;

        public TonemapPass(Output2D output)
        {
            _output = output;
            Output = OutputDesc.FromBackBuffer(OutputClass.Primary, _output);
        }

        public override void Record(GraphicsContext context, ref Resolver resolver)
        {
            var resources = resolver.GetComponent<PipelineResources>();

            var sceneColor = resolver.ResolveResource(resources.SceneColor);

            context.CopyResource(sceneColor, _output.OutputBuffer);

            context.ResourceTransition(_output.OutputBuffer, ResourceState.Present);
        }

        public override void Register(ref RenderPassBuilder builder, ref Resolver resolver)
        {
            var resources = resolver.GetComponent<PipelineResources>();
            builder.MarkUsage(resources.SceneColor, ResourceState.CopySource);
        }
    }
}
