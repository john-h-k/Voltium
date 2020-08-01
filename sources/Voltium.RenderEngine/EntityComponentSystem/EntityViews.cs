#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltium.Common;

namespace Voltium.RenderEngine.EntityComponentSystem
{
    public struct EntityView<T0> : IEnumerable<Entity>, IEnumerator<Entity> where T0 : struct
    {
        private int _index;

        public Entity Current => Components[_index].Entity;

        object? IEnumerator.Current => Current;

        private List<TaggedComponent<T0>> Components => ComponentPool<T0>.Components;


        public ref T0 GetComponent(Entity entity)
            => ref Components.GetRef(entity.Id).Component;

        public bool MoveNext() => ++_index < Components.Count;

        public void Reset() => _index = 0;
        public void Dispose() { }

        public EntityView<T0> GetEnumerator() => this;
        IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
    }
}
