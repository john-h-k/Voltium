using System;
using System.Drawing;
using Voltium.Core;
using Voltium.Core.D3D12;
using Voltium.Core.Managers;
using Voltium.Core.Pipeline;

namespace Voltium.Interactive
{
    public abstract unsafe class Renderer : IDisposable
    {
        public abstract void Init(GraphicsDevice device, GraphicalConfiguration config, in Size screen);

        public abstract void Render(ref GraphicsContext recorder);

        public abstract void Update(ApplicationTimer timer);

        public virtual PipelineStateObject? GetInitialPso() => null;

        public abstract void Resize(Size newScreenData);

        public virtual void OnMouseScroll(int scroll) { }

        public virtual void ToggleMsaa() { }

        public abstract void Dispose();
    }
}
