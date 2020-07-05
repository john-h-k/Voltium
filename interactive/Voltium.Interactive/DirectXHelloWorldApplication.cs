using System.Drawing;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Core;
using Voltium.Core.Devices;
using Voltium.Core.Managers;

namespace Voltium.Interactive
{
    internal class DirectXHelloWorldApplication<TRenderer> : Application where TRenderer : Renderer, new()
    {
        public override string Title => "Hello DirectX!";
        private Renderer _renderer = new TRenderer();

        private GraphicsDevice _device = null!;
        private Output _output = null!;
        private GraphicalConfiguration _config = null!;
        private Size _screen;
        private bool _isPaused;

        public override unsafe void Init(Size data, HWND hwnd)
        {
            var config = new GraphicalConfiguration
            {
                RequiredFeatureLevel = FeatureLevel.GraphicsLevel11_0,
                DebugLayerConfiguration = new DebugLayerConfiguration().DisableDeviceRemovedMetadata()
            };

            _config = config;
            _screen = data;
            _device = GraphicsDevice.Create(null, config);

            var desc = new OutputDesc
            {
                BackBufferFormat = BackBufferFormat.R8G8B8A8UnsignedNormalized,
                BackBufferCount = 3,
                SyncInterval = 0
            };

            _output = Output.CreateForWin32(_device, desc, hwnd);

            _renderer.Init(_device, config, data);
        }

        public override void Update(ApplicationTimer timer)
        {
            if (_isPaused)
            {
                return;
            }
            _renderer.Update(timer);
        }
        public override unsafe void Render()
        {
            if (_isPaused)
            {
                return;
            }
            using (var recorder = _device.BeginGraphicsContext(_renderer.GetInitialPso()))
            {
                _renderer.Render(ref recorder.AsMutable(), out var render);

                if (render.Msaa.IsMultiSampled)
                {
                    recorder.ResolveSubresource(render, _output.BackBuffer);
                }
                else
                {
                    recorder.CopyResource(render, _output.BackBuffer);
                }
                recorder.ResourceTransition(_output.BackBuffer, ResourceState.Present);
            }

            _output.Present();
        }
        public override void Destroy()
        {
            _device.Dispose();
        }

        public override void OnResize(Size newScreenData)
        {
            _screen = newScreenData;
            _output.Resize(newScreenData);
            _renderer.Resize(newScreenData);
        }

        public override void OnKeyDown(byte key)
        {
            if (key == /* P */ 0x50)
            {
                _isPaused = !_isPaused;
            }
            if (key == /* M */ 0x4D)
            {
                _device.Idle();
                _renderer.ToggleMsaa();
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
