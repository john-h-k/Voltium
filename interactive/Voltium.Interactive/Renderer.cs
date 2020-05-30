using System;
using TerraFX.Interop;
using Voltium.Common;
using Voltium.Core;
using Voltium.Core.Managers;

namespace Voltium.Interactive
{
    public abstract unsafe class Renderer
    {
        public abstract void Init(GraphicalConfiguration config, in ScreenData screen, ID3D12Device* device);

        public abstract ComPtr<ID3D12PipelineState> GetInitialPso();

        public abstract void Render(GraphicsContext recorder);

        public abstract void Destroy();

        public abstract void Update();
    }
}
