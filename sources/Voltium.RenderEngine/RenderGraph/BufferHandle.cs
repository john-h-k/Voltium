using Voltium.Core.Memory;

namespace Voltium.RenderEngine
{
    /// <summary>
    /// An opaque handle representing a <see cref="Buffer"/>
    /// </summary>
    public readonly struct BufferHandle
    {
        // index into the graph's list of resources
        internal readonly uint Index;

        internal BufferHandle(uint index)
        {
            Index = index;
        }

        internal ResourceHandle AsResourceHandle() => new ResourceHandle(Index);
    }
}
