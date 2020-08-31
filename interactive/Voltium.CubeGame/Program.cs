using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Voltium.Common;
using Voltium.Core;
using Voltium.Core.Devices;
using Voltium.Core.Pipeline;
using Voltium.Input;
using Voltium.RenderEngine;

namespace Voltium.CubeGame
{
    internal unsafe class Program
    {
        private static int Main() => ApplicationRunner.RunWin32(new CubeApp());
    }

    internal sealed class CubeApp : Application
    {
        public override string Name => nameof(CubeApp);

        private GraphicsDevice _device = null!;
        private Output _output = null!;


        private RenderGraph _graph = null!;

        private Camera _camera = null!;
        private WorldPass _worldPass = null!;
        private TonemapPass _outputPass = null!;

        [MemberNotNull(nameof(_worldPass))]
        public override void Initialize(Size outputSize, IOutputOwner outputOwner)
        {
            _device = GraphicsDevice.Create(FeatureLevel.GraphicsLevel11_0, null, DebugLayerConfiguration.Debug.WithDebugFlags(DebugFlags.DebugLayer));
            _output = Output.Create(OutputConfiguration.Default, _device, outputOwner);

            _camera = new();

            _graph = new RenderGraph(_device, 3);
            _worldPass = new WorldPass(_device, _camera);
            _outputPass = new TonemapPass(_output);
        }

        public override void OnResize(Size newOutputSize)
        {
        }

        public override void Update(ApplicationTimer timer)
        {
            if (KeyboardHandler.Key(ConsoleKey.W).IsDown())
            {
                _camera.TranslateZ(1);
            }
        }

        public override void Render()
        {
            _graph.AddPass(_worldPass);
            _graph.AddPass(_outputPass);

            _graph.ExecuteGraph();

            _output.Present();
        }

        public override void Dispose()
        {
        }
    }
}
