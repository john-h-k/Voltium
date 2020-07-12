#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using Voltium.Core.Memory;

using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.Core
{
    public struct ComponentResolver
    {
        private RenderGraph _graph;

        public ComponentResolver(RenderGraph graph)
        {
            _graph = graph;
        }

        public Texture Resolve(TexHandle handle) => _graph.GetResource(handle.AsResourceHandle()).Desc.Texture;
        public Buffer Resolve(BufferHandle handle) => _graph.GetResource(handle.AsResourceHandle()).Desc.Buffer;
    }
}
