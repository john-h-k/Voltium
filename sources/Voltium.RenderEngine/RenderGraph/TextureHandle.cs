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
}
