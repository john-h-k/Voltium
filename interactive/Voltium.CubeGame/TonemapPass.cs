using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Common.Pix;
using Voltium.Core;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Devices;
using Voltium.RenderEngine;

namespace Voltium.CubeGame
{
    internal class TonemapPass : GraphicsRenderPass
    {
        public override OutputDesc Output => OutputDesc.FromBackBuffer(OutputClass.Primary, _output);

        private Output _output;

        public TonemapPass(Output output)
        {
            _output = output;
        }

        public override void Register(ref RenderPassBuilder builder, ref Resolver resolver)
        {
            var resources = resolver.GetComponent<RenderResources>();
            var settings = resolver.GetComponent<RenderSettings>();

            builder.MarkUsage(resources.SceneColor, settings.Msaa.IsMultiSampled ? ResourceState.ResolveSource : ResourceState.CopySource);
        }

        public override void Record(GraphicsContext context, ref Resolver resolver)
        {
            using var _ = context.BeginEvent(Argb32.Green, "Tonemap");

            var resources = resolver.GetComponent<RenderResources>();
            var settings = resolver.GetComponent<RenderSettings>();

            var sampledOutput = resolver.ResolveResource(resources.SceneColor);

            context.ResourceTransition(_output.OutputBuffer, settings.Msaa.IsMultiSampled ? ResourceState.ResolveDestination : ResourceState.CopyDestination);

            if (settings.Msaa.IsMultiSampled)
            {
                context.ResolveSubresource(sampledOutput, _output.OutputBuffer);
            }
            else
            {
                context.CopyResource(sampledOutput, _output.OutputBuffer);
            }

            context.ResourceTransition(_output.OutputBuffer, ResourceState.Present);
        }
    }
}
