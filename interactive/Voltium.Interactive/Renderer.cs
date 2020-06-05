using System;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core;
using Voltium.Core.Managers;
using Voltium.Core.Pipeline;

namespace Voltium.Interactive
{
    public abstract unsafe class Renderer
    {
        public abstract void Init(GraphicsDevice device, GraphicalConfiguration config, in ScreenData screen);

        public abstract PipelineStateObject GetInitialPso();

        public abstract void Render(GraphicsContext recorder);

        public abstract void Destroy();

        public virtual void Resize(ScreenData newScreenData) { }

        public abstract void Update(ApplicationTimer timer);

        public abstract void OnMouseScroll(int scroll);
    }
}
