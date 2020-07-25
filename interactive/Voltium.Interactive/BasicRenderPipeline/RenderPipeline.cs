using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voltium.Core;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Devices;
using Voltium.Core.Infrastructure;
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

        public override unsafe void Initialize(Size data, IOutputOwner output)
        {
            var debug = new DebugLayerConfiguration();
            debug.BreakpointLogLevel = LogLevel.Error;

            var config = new DeviceConfiguration
            {
                RequiredFeatureLevel = FeatureLevel.GraphicsLevel11_0,
#if DEBUG
                DebugLayerConfiguration = debug
#else
                DebugLayerConfiguration = null
#endif
            };

            using var factory = DeviceFactory.Create();
            foreach (var device in factory)
            {
                Console.WriteLine(device);
            }

            _device = new GraphicsDevice(config, null);

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

            _graph = new RenderGraph(_device);
        }

        public override void Update(ApplicationTimer timer)
        {
            if (_isPaused)
            {
                return;
            }
        }

        private RenderGraph _graph = null!;
        public override unsafe void Render()
        {
            _graph.CreateComponent(_settings);

            _graph.AddPass(_renderer);
            _graph.AddPass(_msaaPass);
            _graph.AddPass(_outputPass);

            _graph.ExecuteGraph();

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
