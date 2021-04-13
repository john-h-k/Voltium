using System;
using System.Drawing;
using Voltium.Core.Devices;

namespace Voltium.Core
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public abstract class Application : IDisposable
    {
        public Application()
        {
            Name = GetType().Name;
        }

        public virtual string Name { get; }

        public virtual string WindowTitle => Name;

        public abstract void Initialize(Size outputSize, IOutputOwner output);
        public abstract void OnResize(Size newOutputSize);


        public abstract void Update(ApplicationTimer timer);
        public abstract void Render();


        public abstract void Dispose();
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
