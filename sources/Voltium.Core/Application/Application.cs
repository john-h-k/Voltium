using TerraFX.Interop;
using Voltium.Core;
using Voltium.Core.Managers;

namespace Voltium.Core
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public abstract class Application
    {
        public abstract string Title { get; }

        public abstract void Init(ScreenData data, HWND hwnd);
        public abstract void Update(ApplicationTimer timer);
        public abstract void Render();
        public abstract void Destroy();


        public virtual void OnResize(ScreenData newScreenData) { }
        public virtual void OnKeyDown(byte key) { }
        public virtual void OnKeyUp(byte key) { }

        public virtual void OnMouseScroll(int scroll) { }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
