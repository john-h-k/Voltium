using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Voltium.Common;
using Voltium.Core;
using Voltium.Core.Configuration.Graphics;
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

        private MsaaDesc _msaa;

        [MemberNotNull(nameof(_worldPass))]
        public override void Initialize(Size outputSize, IOutputOwner outputOwner)
        {
            _device = GraphicsDevice.Create(FeatureLevel.GraphicsLevel11_0, null, DebugLayerConfiguration.Debug.WithDebugFlags(DebugFlags.DebugLayer));
            _output = Output.Create(OutputConfiguration.Default, _device, outputOwner);

            _camera = new();

            _graph = new RenderGraph(_device, 1);
            _worldPass = new WorldPass(_device, _camera);
            _outputPass = new TonemapPass(_output);
        }

        public override void OnResize(Size newOutputSize) => _output.Resize(newOutputSize);

        public override void Update(ApplicationTimer timer)
        {
            var elapsed = (float)timer.ElapsedSeconds;
            var movement = elapsed;

            if (KeyboardHandler.Key(ConsoleKey.W).IsDown())
            {
                _camera.TranslateZ(movement);
            }
            if (KeyboardHandler.Key(ConsoleKey.S).IsDown())
            {
                _camera.TranslateZ(-movement);
            }

            if (KeyboardHandler.Key(ConsoleKey.A).IsDown())
            {
                _camera.TranslateX(movement);
            }
            if (KeyboardHandler.Key(ConsoleKey.D).IsDown())
            {
                _camera.TranslateX(-movement);
            }

            if (KeyboardHandler.Key(ConsoleKey.Spacebar).IsDown())
            {
                _camera.TranslateY(-movement);
            }
            if (KeyboardHandler.Modifier(ConsoleModifiers.Shift).IsDown())
            {
                _camera.TranslateY(movement);
            }

            if (KeyboardHandler.Key(ConsoleKey.UpArrow).IsDown())
            {
                _camera.RotateX(movement);
            }
            if (KeyboardHandler.Key(ConsoleKey.DownArrow).IsDown())
            {
                _camera.RotateX(-movement);
            }

            if (KeyboardHandler.Key(ConsoleKey.RightArrow).IsDown())
            {
                _camera.RotateY(movement);
            }
            if (KeyboardHandler.Key(ConsoleKey.LeftArrow).IsDown())
            {
                _camera.RotateY(-movement);
            }

            if (KeyboardHandler.Key(ConsoleKey.M).IsDown())
            {
                _msaa = _msaa.IsMultiSampled ? MsaaDesc.None : MsaaDesc.X8;
            }
        }

        public override void Render()
        {
            _graph.CreateComponent(new RenderSettings { Msaa = _msaa });

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
