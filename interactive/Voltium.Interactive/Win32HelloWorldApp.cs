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
        public override string Name => "Hello DirectX!";
        private Renderer _renderer = new TRenderer();

        private GraphicsDevice _device = null!;
        private Output _output = null!;
        private bool _isPaused = false;

        public override unsafe void Initialize(Size data, IOutputOwner output)
        {
            _device =GraphicsDevice.Create(FeatureLevel.GraphicsLevel11_0, null);

            var desc = new OutputConfiguration
            {
                BackBufferFormat = BackBufferFormat.R16G16B16A16Single,
                BackBufferCount = 3,
                SyncInterval = 0
            };

            _output = Output.Create(desc, _device, output);

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
                _renderer.Render(recorder, out var render);
                if (render.Msaa.IsMultiSampled)
                {
                    recorder.ResolveSubresource(render, _output.OutputBuffer);
                }
                else
                {
                    recorder.CopyResource(render, _output.OutputBuffer);
                }
                //recorder.ResourceTransition(_output.OutputBuffer, ResourceState.Present);
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
    }
}
