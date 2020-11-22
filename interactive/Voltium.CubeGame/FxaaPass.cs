using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Core;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.RenderEngine;

namespace Voltium.CubeGame
{
    public class FxaaPass : ComputeRenderPass
    {
        private ComputeDevice _device;

        public FxaaPass(ComputeDevice device)
        {
            _device = device;
        }

        public bool IsEnabled { get; set; }

        public override void Record(ComputeContext context, ref Resolver resolver) => throw new NotImplementedException();
        public override bool Register(ref RenderPassBuilder builder, ref Resolver resolver)
        {
            if (!IsEnabled)
            {
                return false;
            }

            Span<(ulong Address, uint Value)> pairs = stackalloc (ulong Address, uint Value)[10];

            var settings = resolver.GetComponent<RenderSettings>();
            var resources = resolver.GetComponent<RenderResources>();

            //builder.CreatePrimaryOutputRelativeTexture(TextureDesc.CreateUnorderedAccessResourceDesc(settings.))

            return true;
        }
    }
}
