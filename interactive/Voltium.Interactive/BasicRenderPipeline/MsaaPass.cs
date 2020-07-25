using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Common.Pix;
using Voltium.Core;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Memory;
using Voltium.RenderEngine;

namespace Voltium.Interactive.BasicRenderPipeline
{
    public sealed class MsaaPass : GraphicsRenderPass
    {
        public override void Register(ref RenderPassBuilder builder, ref Resolver resolver)
        {
            var resources = resolver.GetComponent<PipelineResources>();
            var settings = resolver.GetComponent<PipelineSettings>();


            if (settings.Msaa.IsMultiSampled)
            {
                resources.SampledOutput = builder.CreatePrimaryOutputRelativeTexture(
                    TextureDesc.CreateRenderTargetDesc(DataFormat.R8G8B8A8UnsignedNormalized, Rgba128.CornflowerBlue),
                    ResourceState.ResolveDestination,
                    debugName: "SampledOutput"
                );

                builder.MarkUsage(resources.SceneColor, ResourceState.ResolveSource);
            }
            else
            {
                resources.SampledOutput = resources.SceneColor;
            }

            resolver.SetComponent(resources);
        }

        public override void Record(ref GraphicsContext context, ref Resolver resolver)
        {
            using var _ = context.BeginEvent(Argb32.Red, "Msaa");

            var resources = resolver.GetComponent<PipelineResources>();
            var settings = resolver.GetComponent<PipelineSettings>();


            var sceneColor = resolver.ResolveResource(resources.SceneColor);
            var sampleOutput = resolver.ResolveResource(resources.SampledOutput);

            if (settings.Msaa == MultisamplingDesc.None)
            {
                //context.CopyResource(sceneColor, sampleOutput);
            }
            else
            {
                context.ResolveSubresource(sceneColor, sampleOutput);
            }
        }
    }
}
