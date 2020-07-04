using System.Drawing;
using TerraFX.Interop;

namespace Voltium.Core
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public abstract class Application
    {
        public abstract string Title { get; }

        public abstract void Init(Size data, HWND hwnd);
        public abstract void Update(ApplicationTimer timer);
        public abstract void Render();
        public abstract void Destroy();


        public virtual void OnResize(Size newScreenData) { }
        public virtual void OnKeyDown(byte key) { }
        public virtual void OnKeyUp(byte key) { }

        public virtual void OnMouseScroll(int scroll) { }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
