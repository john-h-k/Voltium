using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Core;
using Voltium.Core.Managers;

namespace Voltium.RenderEngine.Passes
{
    internal sealed class BasicForwardPass : RenderPass
    {
        public override void Record(ref GraphicsContext context)
        {
            throw new NotImplementedException();
        }

        public override void Register(ref RenderPassBuilder builder)
        {
            var sceneColor = new SceneColor { ColorBuffer = builder.CreateRenderTarget() };
            builder.Components.Add(sceneColor);
        }
    }

    internal struct SceneColor
    {
        public TexHandle ColorBuffer;
    }

    internal sealed class LdrPresentPass : PresentPass
    {
        public LdrPresentPass(GraphicsDevice device)
        {
            DefaultPipelineState = 
        }

        public override void Record(ref GraphicsContext context)
        {
            context.DrawIndexed(3);
        }

        public override void Register(ref RenderPassBuilder builder, TexHandle backBuffer)
        {
            var sceneColor = builder.Components.Get<SceneColor>();
            builder.Read(sceneColor.ColorBuffer, ResourceReadFlags.PixelShaderResource);
            builder.Write(backBuffer, ResourceWriteFlags.RenderTarget);
        }
    }
}
