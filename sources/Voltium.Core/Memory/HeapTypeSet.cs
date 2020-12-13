using System.Runtime.CompilerServices;

namespace Voltium.Core.Memory
{
    internal enum GpuResourceType
    {
        Meaningless = 0,
        Tex = 1,
        RenderTargetOrDepthStencilTexture = 2,
        Buffer = 3
    }
}
