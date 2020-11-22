using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpGLTF.Schema2;
using Voltium.Common.Pix;
using Voltium.Core;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Memory;
using Voltium.RenderEngine;

namespace Voltium.Interactive.BasicRenderPipeline
{
    public sealed class MsaaPass : GraphicsRenderPass
    {
        public override bool Register(ref RenderPassBuilder builder, ref Resolver resolver)
        {
            var resources = resolver.GetComponent<PipelineResources>();
            var settings = resolver.GetComponent<PipelineSettings>();

            if (settings.Msaa.IsMultiSampled)
            {
                resources.SampledOutput = builder.CreatePrimaryOutputRelativeTexture(
                    TextureDesc.CreateRenderTargetDesc(DataFormat.R8G8B8A8UnsignedNormalized, Rgba128.CornflowerBlue),
                    ResourceState.ResolveDestination,
                    debugName: nameof(resources.SampledOutput)
                );

                resolver.SetComponent(resources);
                builder.MarkUsage(resources.SceneColor, ResourceState.ResolveSource);
                return true;
            }
            else
            {
                resolver.SetComponent(resources);
                resources.SampledOutput = resources.SceneColor;
                return false;
            }


        }

        public override void Record(GraphicsContext context, ref Resolver resolver)
        {
            var settings = resolver.GetComponent<PipelineSettings>();
            if (settings.Msaa.IsMultiSampled)
            {
                using var _ = context.BeginEvent(Argb32.Red, "Msaa");

                var resources = resolver.GetComponent<PipelineResources>();

                var sceneColor = resolver.ResolveResource(resources.SceneColor);
                var sampleOutput = resolver.ResolveResource(resources.SampledOutput);

                context.ResolveSubresource(sceneColor, sampleOutput);
            }
        }
    }
}
