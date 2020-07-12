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
        private RenderGraphApplication _app;

        public OutputPass(Output output, RenderGraphApplication application)
        {
            _output = output;
            _app = application;
            Output = OutputDesc.FromOutput(OutputType.Primary, _output);
        }

        public override void Record(ref GraphicsContext context, ref ComponentResolver resolver)
        {
            var sceneColor = resolver.Resolve(_app.SceneColorHandle);
            context.CopyResource(sceneColor, _output.BackBuffer);
            context.ResourceTransition(_output.BackBuffer, ResourceState.Present);
        }

        public override void Register(ref RenderPassBuilder builder)
        {
            builder.MarkUsage(_app.SceneColorHandle, ResourceState.CopyDestination);
        }
    }
}
