using System;
using System.Drawing;
using TerraFX.Interop;
using Voltium.Core.Devices;

namespace Voltium.Core
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public abstract class Application : IDisposable
    {
        public abstract string Title { get; }

        public abstract void Initialize(Size outputSize, IOutputOwner output);
        public abstract void OnResize(Size newOutputSize);


        public abstract void Update(ApplicationTimer timer);
        public abstract void Render();


        public abstract void Dispose();
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
