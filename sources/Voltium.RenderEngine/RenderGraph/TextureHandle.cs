using Voltium.Core.CommandBuffer;
using Voltium.Core.Memory;

namespace Voltium.RenderEngine
{
    /// <summary>
    /// An opaque handle representing a <see cref="Texture"/>
    /// </summary>
    public readonly struct TextureHandle
    {
        // index into the graph's list of resources
        internal readonly uint Index;

        internal TextureHandle(uint index)
        {
            Index = index;
        }

        internal ResourceHandle AsResourceHandle() => new ResourceHandle(Index);
    }

    /// <summary>
    /// An opaque handle representing a <see cref="View"/>
    /// </summary>
    public readonly struct ViewHandle
    {
        // index into the graph's list of views
        internal readonly uint Index;

        internal ViewHandle(uint index)
        {
            Index = index;
        }
        internal bool IsInvalid => Index == 0;
    }
}
