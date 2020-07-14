using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Common.Pix;
using Voltium.Core;
using Voltium.Core.Configuration.Graphics;
using Voltium.RenderEngine;

namespace Voltium.Interactive.BasicRenderPipeline
{
    public sealed class MsaaPass : GraphicsRenderPass
    {
        public override void Register(ref RenderPassBuilder builder, ref Resolver resolver)
        {
            var resources = resolver.GetComponent<PipelineResources>();
            var settings = resolver.GetComponent<PipelineSettings>();

            bool hasMsaa = settings.Msaa == MultisamplingDesc.None;
            builder.MarkUsage(resources.SceneColor, hasMsaa ? ResourceState.ResolveSource: ResourceState.CopySource);
            builder.MarkUsage(resources.SampledOutput, hasMsaa ? ResourceState.ResolveDestination : ResourceState.CopyDestination);
        }

        public override void Record(ref GraphicsContext context, ref Resolver resolver)
        {
            using var _ = context.BeginScopedEvent(Argb32.Red, "Msaa");

            var resources = resolver.GetComponent<PipelineResources>();
            var settings = resolver.GetComponent<PipelineSettings>();


            var sceneColor = resolver.ResolveResource(resources.SceneColor);
            var sampleOutput = resolver.ResolveResource(resources.SampledOutput);

            if (settings.Msaa == MultisamplingDesc.None)
            {
                context.CopyResource(sceneColor, sampleOutput);
            }
            else
            {
                context.ResolveSubresource(sceneColor, sampleOutput);
            }
        }
    }
}
