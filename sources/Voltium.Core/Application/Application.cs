using Voltium.Core;
using Voltium.Core.Managers;

namespace Voltium.Core
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public abstract class Application
    {
        public abstract string Title { get; }

        public abstract void Init(ScreenData data);
        public abstract void Update(ApplicationTimer timer);
        public abstract void Render();
        public abstract void Destroy();


        public abstract void OnResize(ScreenData newScreenData);
        public abstract void OnKeyDown(byte key);
        public abstract void OnKeyUp(byte key);

        public abstract void OnMouseScroll(int scroll);
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
