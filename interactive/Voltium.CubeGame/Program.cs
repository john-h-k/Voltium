using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Voltium.Common;
using Voltium.Core;
using Voltium.Core.CommandBuffer;
using Voltium.Core.Configuration.Graphics;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Voltium.Core.Pipeline;
using Voltium.Input;
using Voltium.RenderEngine;
using static Voltium.RenderEngine.RenderGraph;

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
        private FxaaPass _fxaaPass = null!;
        private TonemapPass _outputPass = null!;

        private MsaaDesc _msaa;

        [MemberNotNull(nameof(_worldPass))]
        public override void Initialize(Size outputSize, IOutputOwner outputOwner)
        {
            _device = GraphicsDevice.Create(FeatureLevel.GraphicsLevel11_0, null, DebugLayerConfiguration.Debug.WithDebugFlags(DebugFlags.DebugLayer | DebugFlags.GpuBasedValidation));
            _output = Output.Create(OutputConfiguration.Default, _device, outputOwner);

            _camera = new();

            const uint latency = 1;
            _graph = new RenderGraph(_device, latency);
            _worldPass = new WorldPass(_device, _camera);
            _fxaaPass = new FxaaPass(_device);
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

        

        public override void Dispose()
        {
        }
    }
}
