using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using Voltium.Core;
using Voltium.Core.Configuration.Graphics;

namespace Voltium.Interactive.RenderGraphSamples
{
    public struct PipelineResources
    {
        public TextureHandle SceneColor;
    }

    public struct PipelineSettings
    {
        public MultisamplingDesc Msaa;
    }
}
