using System;
using Voltium.Core;
using Voltium.Core.D3D12;
using Voltium.Core.Managers;
using Voltium.Core.Pipeline;

namespace Voltium.Interactive
{
    public abstract unsafe class Renderer : IDisposable
    {
        public abstract void Init(GraphicsDevice device, GraphicalConfiguration config, in ScreenData screen);

        public abstract void Render(ref GraphicsContext recorder);

        public abstract void Update(ApplicationTimer timer);

        public virtual PipelineStateObject? GetInitialPso() => null;

        public virtual void Resize(ScreenData newScreenData) { }

        public virtual void OnMouseScroll(int scroll) { }

        public virtual void ToggleMsaa() { }

        public abstract void Dispose();
    }
}
