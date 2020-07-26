using System;
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
        private static void Main(string[] args)
        {
            Win32Application.Run(new CubeApp());
        }
    }

    internal sealed class CubeApp : Application
    {
        public override string Title => throw new NotImplementedException();

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

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
            throw new NotImplementedException();
        }

        public override void Update(ApplicationTimer timer)
        {
            throw new NotImplementedException();
        }
    }
}
