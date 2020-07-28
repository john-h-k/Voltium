using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Voltium.Core;
using Voltium.Core.Devices;

namespace Voltium.CubeGame
{
    internal unsafe class Program
    {
        private static int Main() => Win32Application.Run(new CubeApp());
    }

    internal sealed class CubeApp : Application
    {
        public override string Title => nameof(CubeApp);


        private WorldRenderer _renderer = null!;

        [MemberNotNull(nameof(_renderer))]
        public override void Initialize(Size outputSize, IOutputOwner output)
        {
            throw new NotImplementedException();
        }

        public override void OnResize(Size newOutputSize)
        {
            throw new NotImplementedException();
        }

        public override void Render()
        {
            _renderer.Render();
        }

        public override void Update(ApplicationTimer timer)
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
        }
    }
}
