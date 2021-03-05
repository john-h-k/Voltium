using System;
using System.Drawing;
using System.Threading.Tasks;
using Voltium.Core.Memory;
using static TerraFX.Interop.Windows;

namespace Voltium.Core.Devices
{
    public enum PresentFlags : uint
    {
        None = 0,
        DoNotSequence = DXGI_PRESENT_DO_NOT_SEQUENCE,
        Restart = DXGI_PRESENT_RESTART,
        DoNotWait = DXGI_PRESENT_DO_NOT_WAIT,
        AllowTearing = DXGI_PRESENT_ALLOW_TEARING
    }

    public struct OutputInfo
    {

    }

    public enum PresentWaitState
    {
        PresentReady,
        Timeout,
        Error
    }

    public interface INativeOutput : IDisposable
    {
        public OutputInfo Info { get; }

        public uint Width { get; }
        public uint Height { get; }
        public bool IsVrStereo { get; }

        public BackBufferFormat Format { get; }

        public uint BackBufferIndex { get; }
        public uint BackBufferCount { get; }

        TextureHandle BackBuffer { get; }
        ViewHandle BackBufferView { get; }

        PresentWaitState WaitForNextPresent(TimeSpan span);
        ValueTask WaitForNextPresentAsync(TimeSpan span);

        void Present(PresentFlags flags = PresentFlags.None);

        void PresentPartial(
            ReadOnlySpan<Rectangle> dirtyRects,
            Rectangle scrollRect,
            Point scrollOffset,
            PresentFlags flags = PresentFlags.None
        )
            => Present(flags);

        void Resize(uint width, uint height, BackBufferFormat format = 0, uint backBufferCount = 0);

        // TODO
        // void ResizeWithAlternateNodeRendering(uint width, uint height, ReadOnlySpan<uint> nodes, BackBufferFormat format = 0);
    }
}
