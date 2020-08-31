using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voltium.Common;
using Voltium.Core;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Devices;
using Voltium.Core.Infrastructure;
using Voltium.RenderEngine;

namespace Voltium.Interactive.BasicRenderPipeline
{
    public sealed class RenderPipeline : Application
    {
        public override string Name => "RenderPipeline";

        private GraphicsDevice _device = null!;

        private BasicSceneRenderer _renderer = null!;
        private MsaaPass _msaaPass = null!;
        private TonemapPass _outputPass = null!;

        private Output _output = null!;
        private PipelineSettings _settings;

        public override unsafe void Initialize(Size data, IOutputOwner output)
        {
            using var factory = DeviceFactory.Create();
            foreach (var device in factory)
            {
                Console.WriteLine(device);
            }

            _device = GraphicsDevice.Create(FeatureLevel.GraphicsLevel11_0, null);

            var desc = new OutputConfiguration
            {
                BackBufferFormat = BackBufferFormat.R8G8B8A8UnsignedNormalized,
                BackBufferCount = 3,
                SyncInterval = 0
            };
            
            _output = Output.Create(desc, _device, output);

            _settings = new PipelineSettings
            {
                Msaa = MultisamplingDesc.None,
                Resolution = _output.Dimensions,
                AspectRatio = _output.AspectRatio
            };


            _renderer = new BasicSceneRenderer(_device);
            _msaaPass = new MsaaPass();
            _outputPass = new TonemapPass(_output);

            _graph = new RenderGraph(_device, _output.Configuration.BackBufferCount);
        }

        public override void Update(ApplicationTimer timer)
        {
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
    }
}
