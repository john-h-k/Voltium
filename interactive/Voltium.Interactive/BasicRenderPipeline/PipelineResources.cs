using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Core;
using Voltium.Core.Configuration.Graphics;
using Voltium.RenderEngine;

namespace Voltium.Interactive.BasicRenderPipeline
{
    public struct PipelineResources
    {
        public TextureHandle SceneColor;
        public TextureHandle SceneDepth;

        public TextureHandle SampledOutput;
    }

    public struct PipelineSettings
    {
        public MultisamplingDesc Msaa;
        public float AspectRatio;
        public Size Resolution;
    }
}
