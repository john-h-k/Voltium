using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Core;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Devices;
using Voltium.RenderEngine;

namespace Voltium.Interactive.BasicRenderPipeline
{
    public sealed class RenderPipeline : Application
    {
        public override string Title => "RenderPipeline";

        private GraphicsDevice _device = null!;

        private BasicSceneRenderer _renderer = null!;
        private MsaaPass _msaaPass = null!;
        private TonemapPass _outputPass = null!;

        private Output _output = null!;
        private bool _isPaused;
        private PipelineSettings _settings;

        public override unsafe void Init(Size data, IOutputOwner output)
        {
            var debug = new DebugLayerConfiguration().DisableDeviceRemovedMetadata();

            var config = new GraphicalConfiguration
            {
                RequiredFeatureLevel = FeatureLevel.GraphicsLevel11_0,
#if DEBUG
                DebugLayerConfiguration = debug
#else
                DebugLayerConfiguration = null
#endif
            };

            _device = GraphicsDevice.Create(null, config);

            var desc = new OutputConfiguration
            {
                BackBufferFormat = BackBufferFormat.R8G8B8A8UnsignedNormalized,
                BackBufferCount = 3,
                SyncInterval = 0
            };

            _output = Output.Create(_device, desc, output, implicitExecuteOnPresent: true);

            var resolution = new Size((int)_output.BackBuffer.Width, (int)_output.BackBuffer.Height);
            _settings = new PipelineSettings
            {
                Msaa = MultisamplingDesc.None,
                Resolution = resolution,
                AspectRatio = resolution.Width / (float)resolution.Height
            };


            _renderer = new BasicSceneRenderer(_device);
            _msaaPass = new MsaaPass();
            _outputPass = new TonemapPass(_output);
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
            var graph = new RenderGraph(_device);

            graph.CreateComponent(_settings);

            graph.AddPass(_renderer);
            graph.AddPass(_msaaPass);
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
