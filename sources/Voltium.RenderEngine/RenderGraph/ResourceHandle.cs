#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member


namespace Voltium.Core
{
    internal readonly struct ResourceHandle
    {
        // index into the graph's list of resources
        internal readonly uint Index;

        internal ResourceHandle(uint index)
        {
            Index = index;
        }

        internal BufferHandle AsBufferHandle() => new BufferHandle(Index);
        internal TextureHandle AsTextureHandle() => new TextureHandle(Index);
    }
}
