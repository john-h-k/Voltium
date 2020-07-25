using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using TerraFX.Interop;
using Voltium.Core;
using Voltium.Core.Devices;

namespace Voltium.Interactive
{
    internal class Win32HelloWorldApp<TRenderer> : Application where TRenderer : Renderer, new()
    {
        public override string Title => "Hello DirectX!";
        private Renderer _renderer = new TRenderer();

        private GraphicsDevice _device = null!;
        private Output _output = null!;
        private bool _isPaused;

        public override unsafe void Initialize(Size data, IOutputOwner output)
        {
            var config = new DeviceConfiguration
            {
                RequiredFeatureLevel = FeatureLevel.GraphicsLevel11_0,
                DebugLayerConfiguration =
#if DEBUG
                new DebugLayerConfiguration()
#else
                null
#endif
            };

            _device = new GraphicsDevice(config, null);

            var desc = new OutputConfiguration
            {
                BackBufferFormat = BackBufferFormat.R16G16B16A16Single,
                BackBufferCount = 3,
                SyncInterval = 0
            };

            _output = Output.Create(_device, desc, output, implicitExecuteOnPresent: true);

            _renderer.Init(_device, data);
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
        public override void Dispose()
        {
            _device.Dispose();
        }

        public override void OnResize(Size newScreenData)
        {
            _output.Resize(newScreenData);
            _renderer.Resize(newScreenData);
        }

        public override void OnKeyDown(ConsoleKey key)
        {
            if (key == ConsoleKey.P)
            {
                _isPaused = !_isPaused;
            }
            if (key == ConsoleKey.M)
            {
                _device.Idle();
                _renderer.ToggleMsaa();
            }
        }

        public override void OnKeyUp(ConsoleKey key)
        {
        }

        public override void OnMouseScroll(int scroll)
        {
            _renderer.OnMouseScroll(scroll);
        }
    }
}
