using Voltium.Core.Managers;

namespace Voltium.Interactive
{
    public abstract class Application
    {
        public abstract string Title { get; }

        public abstract void Init(ScreenData data);
        public abstract void Update();
        public abstract void Render();
        public abstract void Destroy();

        public abstract void OnKeyDown(byte key);
        public abstract void OnKeyUp(byte key);
    }
}
