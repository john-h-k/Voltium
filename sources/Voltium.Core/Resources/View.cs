using Voltium.Core.Memory;
using Voltium.Core.NativeApi;

namespace Voltium.Core.CommandBuffer
{
    public struct View
    {
        internal ViewHandle Handle;
        private Disposal<ViewHandle> _dispose;

        internal View(ViewHandle handle, Disposal<ViewHandle> dispose)
        {
            Handle = handle;
            _dispose = dispose;
        }

        public void Dispose() => _dispose.Dispose(ref Handle);
    }
}
