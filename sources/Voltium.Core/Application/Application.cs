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

        public abstract void Initialize(Size data, IOutputOwner output);
        public abstract void Update(ApplicationTimer timer);
        public abstract void Render();
        public abstract void Dispose();


        public virtual void OnResize(Size newScreenData) { }
        public virtual void OnKeyDown(ConsoleKey key) { }
        public virtual void OnKeyUp(ConsoleKey key) { }

        public virtual void OnMouseScroll(int scroll) { }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
