using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;
using Voltium.Core;
using Voltium.Core.Devices;

namespace Voltium.Interactive.RenderGraphSamples
{
    internal class RenderGraphApplication : Application
    {
        public override string Title => "Hello DirectX!";

        private GraphicsDevice _device = null!;
        private MandelbrotRenderPass _renderer = null!;
        private OutputPass _outputPass = null!;
        private Output _output = null!;
        private bool _isPaused;

        public override unsafe void Init(Size data, IOutputOwner output)
        {
            var config = new GraphicalConfiguration
            {
                RequiredFeatureLevel = FeatureLevel.GraphicsLevel11_0,
                DebugLayerConfiguration =
#if DEBUG
                new DebugLayerConfiguration()
#else
                null
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
        }

        public override void OnKeyUp(ConsoleKey key)
        {
        }
    }
}
