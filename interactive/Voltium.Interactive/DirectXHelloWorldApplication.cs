
using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using Voltium.Core;
using Voltium.Core.Managers;
using Voltium.Core.Configuration.Graphics;

namespace Voltium.Interactive
{
    internal class DirectXHelloWorldApplication : Application
    {
        public override string Title => "Hello DirectX!";
        private Renderer _renderer = new DefaultRenderer();

        private GraphicsDevice _device = null!;

        [MemberNotNull(nameof(_device))]
        public override unsafe void Init(ScreenData data)
        {
            var config = new GraphicalConfiguration
            {
                ForceFullscreenAsWindowed = false,
                VSyncCount = 0,
                BackBufferFormat = DataFormat.R8G8B8A8UnsignedNormalized,
                DepthStencilFormat = DataFormat.D32Single,
                MultiSamplingStrategy = new MsaaDesc(1, 0),
                RequiredFeatureLevel = FeatureLevel.Level11_0,
                SwapChainBufferCount = 3
            };

            _device = GraphicsDevice.Create(config, data);

            _renderer.Init(_device, config, data);
            _renderer.Resize(data);
        }

        public override void Update(ApplicationTimer timer)
        {
            _renderer.Update(timer);
        }
        public override unsafe void Render()
        {
            using (var commandList = _device.BeginGraphicsContext(_renderer.GetInitialPso()))
            {
                commandList.SetViewportAndScissor(_device.ScreenData);
                _renderer.Render(commandList);
            }

            _device.Present();
        }
        public override void Destroy()
        {
            _device.Dispose();
        }

        public override void OnResize(ScreenData newScreenData)
        {
            _device.Resize(newScreenData);
            _renderer.Resize(newScreenData);
        }

        public override void OnKeyDown(byte key)
        {

        }

        public override void OnKeyUp(byte key)
        {

        }

        public override void OnMouseScroll(int scroll)
        {
            _renderer.OnMouseScroll(scroll);
        }
    }
}
