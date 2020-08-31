using Voltium.Common;
using Voltium.Core.Devices;
using Voltium.Core.Memory;
using Buffer = Voltium.Core.Memory.Buffer;

namespace Voltium.RenderEngine
{
    /// <summary>
    /// The type used for resolving resources and application-defined components between render passes
    /// </summary>
    public struct Resolver
    {
        private RenderGraph _graph;
        private RenderComponents _components;

        // false when in the Register method, true in Record
        internal bool CanResolveResources;

        internal Resolver(RenderGraph graph)
        {
            _graph = graph;
            _components = RenderComponents.Create();
            CanResolveResources = false;
        }

        /// <summary>
        /// Resolves an individual application-defined component
        /// </summary>
        /// <typeparam name="TComponent">The type of the component to resolve</typeparam>
        /// <returns>The value of the component if it was found</returns>
        public TComponent GetComponent<TComponent>() => _components.Get<TComponent>();


        /// <summary>
        /// Writes to an existing application-defined component
        /// </summary>
        /// <typeparam name="TComponent">The type of the component to write to</typeparam>
        public void SetComponent<TComponent>(TComponent component) => _components.Set(component);

        /// <summary>
        /// Creates a new application-defined component
        /// </summary>
        /// <typeparam name="TComponent">The type of the component to create</typeparam>
        public void CreateComponent<TComponent>(TComponent component) => _components.Add(component);

        /// <summary>
        /// Resolves a <see cref="TextureHandle"/> created during pass registration to an allocated <see cref="Texture"/>
        /// </summary>
        /// <param name="handle">The <see cref="TextureHandle"/></param>
        /// <returns>A <see cref="Texture"/></returns>
        public Texture ResolveResource(TextureHandle handle)
        {
            AssertCanResolveResources();
            return _graph.GetResource(handle.AsResourceHandle()).Desc.Texture;
        }

        /// <summary>
        /// Resolves a <see cref="BufferHandle"/> created during pass registration to an allocated <see cref="Buffer"/>
        /// </summary>
        /// <param name="handle">The <see cref="BufferHandle"/></param>
        /// <returns>A <see cref="Buffer"/></returns>
        public Buffer ResolveResource(BufferHandle handle)
        {
            AssertCanResolveResources();
            return _graph.GetResource(handle.AsResourceHandle()).Desc.Buffer;
        }

        private void AssertCanResolveResources()
        {
            if (!CanResolveResources)
            {
                ThrowHelper.ThrowInvalidOperationException("Cannot call ResolveResource in the Register phase of a pass");
            }
        }
    }
}
