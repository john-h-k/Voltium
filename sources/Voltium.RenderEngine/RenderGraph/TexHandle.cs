#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member


namespace Voltium.Core
{
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
