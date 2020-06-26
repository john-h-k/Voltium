
using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using Voltium.Core;
using Voltium.Core.Managers;
using Voltium.Core.Configuration.Graphics;
using TerraFX.Interop;

namespace Voltium.Interactive
{
    internal class DirectXHelloWorldApplication : Application
    {
        public override string Title => "Hello DirectX!";
        private Renderer _renderer = new DefaultRenderer();

        private GraphicsDevice _device = null!;
        private GraphicalConfiguration _config = null!;
        private ScreenData _screen;

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

            _config = config;
            _screen = data;
            _device = GraphicsDevice.Create(config, data);

            _renderer.Init(_device, config, data);
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
            _screen = newScreenData;
            _device.Resize(newScreenData);
            _renderer.Init(_device, _config, newScreenData);
        }

        public override void OnKeyDown(byte key)
        {
            if (key == 0x4D)
            {
                _renderer.ToggleMsaa();
                _renderer.Init(_device, _config, _screen);
            }
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
