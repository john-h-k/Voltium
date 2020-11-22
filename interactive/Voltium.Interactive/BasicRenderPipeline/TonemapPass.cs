using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Common.Pix;
using Voltium.Core;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Contexts;
using Voltium.Core.Devices;
using Voltium.RenderEngine;

namespace Voltium.Interactive.BasicRenderPipeline
{
    [ExpectsComponent(typeof(PipelineResources), typeof(PipelineSettings))]
    internal class TonemapPass : GraphicsRenderPass
    {
        public override OutputDesc Output { get; }

        private Output _output;

        public TonemapPass(Output output)
        {
            _output = output;
            Output = OutputDesc.FromBackBuffer(OutputClass.Primary, _output);
        }

        public override bool Register(ref RenderPassBuilder builder, ref Resolver resolver)
        {
            var resources = resolver.GetComponent<PipelineResources>();
            builder.MarkUsage(resources.SampledOutput, ResourceState.CopySource);

            return true;
        }

        public override void Record(GraphicsContext context, ref Resolver resolver)
        {
            using var _ = context.BeginEvent(Argb32.Green, "Tonemap");

            var resources = resolver.GetComponent<PipelineResources>();
            var sampledOutput = resolver.ResolveResource(resources.SampledOutput);

            context.Barrier(ResourceBarrier.Transition(_output.OutputBuffer, ResourceState.Present, ResourceState.CopyDestination));
            context.CopyResource(sampledOutput, _output.OutputBuffer);
            context.Barrier(ResourceBarrier.Transition(_output.OutputBuffer, ResourceState.CopyDestination, ResourceState.Present));
        }
    }
}
