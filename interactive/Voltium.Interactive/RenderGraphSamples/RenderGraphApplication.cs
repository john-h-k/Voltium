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
        public override string Name => "Hello DirectX!";

        private GraphicsDevice _device = null!;
        private MandelbrotRenderPass _renderer = null!;
        private TonemapPass _outputPass = null!;
        private Output2D _output = null!;
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

            _output = Output2D.Create(desc, _device, output);

            _renderer = new MandelbrotRenderPass(_device, data);
            _outputPass = new TonemapPass(_output);
        }

        public override void Update(ApplicationTimer timer)
        {
        }
        public override unsafe void Render()
        {
            var graph = new RenderGraph(_device, 3);

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
    }
}
