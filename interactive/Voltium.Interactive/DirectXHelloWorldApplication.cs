
using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using Voltium.Core;
using Voltium.Core.Managers;
using Voltium.Core.Configuration.Graphics;
using TerraFX.Interop;
using System.Runtime.CompilerServices;
using System.Drawing;

namespace Voltium.Interactive
{
    internal class DirectXHelloWorldApplication<TRenderer> : Application where TRenderer : Renderer, new()
    {
        public override string Title => "Hello DirectX!";
        private Renderer _renderer = new TRenderer();

        private GraphicsDevice _device = null!;
        private GraphicalConfiguration _config = null!;
        private Size _screen;

        public override unsafe void Init(Size data, HWND hwnd)
        {
            var config = new GraphicalConfiguration
            {
                VSyncCount = 0,
                BackBufferFormat = BackBufferFormat.R8G8B8A8UnsignedNormalized,
                RequiredFeatureLevel = FeatureLevel.Level11_0,
                SwapChainBufferCount = 3
            };

            _config = config;
            _screen = data;
            _device = GraphicsDevice.CreateWithOutput(config, data, hwnd);

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
                _renderer.Render(ref Unsafe.AsRef(in commandList));
            }

            _device.Present();
        }
        public override void Destroy()
        {
            _device.Dispose();
        }

        public override void OnResize(Size newScreenData)
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
