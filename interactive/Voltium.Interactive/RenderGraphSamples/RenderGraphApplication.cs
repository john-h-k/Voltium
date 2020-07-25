using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Core;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Devices;
using Voltium.RenderEngine;

namespace Voltium.Interactive.RenderGraphSamples
{
    internal class RenderGraphApplication : Application
    {
        public override string Title => "Hello DirectX!";

        private GraphicsDevice _device = null!;
        private MandelbrotRenderPass _renderer = null!;
        private TonemapPass _outputPass = null!;
        private Output _output = null!;
        private bool _isPaused;
        private PipelineSettings _settings;

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
                BackBufferFormat = BackBufferFormat.R8G8B8A8UnsignedNormalized,
                BackBufferCount = 3,
                SyncInterval = 0
            };

            _settings = new PipelineSettings
            {
                Msaa = MultisamplingDesc.None
            };

            _output = Output.Create(_device, desc, output, implicitExecuteOnPresent: true);

            _renderer = new(_device, data);
            _outputPass = new(_output);
        }

        public override void Update(ApplicationTimer timer)
        {
            if (_isPaused)
            {
                return;
            }
        }
        public override unsafe void Render()
        {
            if (_isPaused)
            {
                return;
            }

            var graph = new RenderGraph(_device);

            graph.CreateComponent(_settings);

            graph.AddPass(_renderer);
            graph.AddPass(_outputPass);
            graph.ExecuteGraph();

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
                if (_settings.Msaa == MultisamplingDesc.None)
                {
                    _settings.Msaa = MultisamplingDesc.X8;
                }
                else
                {
                    _settings.Msaa = MultisamplingDesc.None;
                }    
            }
        }

        public override void OnKeyUp(ConsoleKey key)
        {
        }
    }
}
