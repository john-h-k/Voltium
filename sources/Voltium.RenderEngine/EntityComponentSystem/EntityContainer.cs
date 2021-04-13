#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Collections.Extensions;
using TerraFX.Interop;
using Voltium.Common;

namespace Voltium.RenderEngine.EntityComponentSystem
{
    [GenerateEquality]
    public partial struct Entity
    {
        // using short for version doesn't actually result in a shorter struct
        internal int Version;
        internal int Id;

        internal Entity(int id)
        {
            Id = id;
            Version = 0;
        }
    }

    public struct EntityType
    {
        internal ValueList<int> ComponentTypes;
        internal Array Components;
    }

    public sealed class EntityContainer
    {
        private static int _componentCount = 0;
        private static class ComponentId<T> { public static readonly int Id = Interlocked.Increment(ref _componentCount) - 1; }
        private static class ComponentId<T0, T1> { public static readonly int Id = Interlocked.Increment(ref _componentCount) - 1; }
        private static class ComponentId<T0, T1, T2> { public static readonly int Id = Interlocked.Increment(ref _componentCount) - 1; }
        private static class ComponentId<T0, T1, T2, T3> { public static readonly int Id = Interlocked.Increment(ref _componentCount) - 1; }

        private int RuntimeComponentId() => Interlocked.Increment(ref _componentCount) - 1;

        private DictionarySlim<int, /* List<...> */ object> _archetypes = new();
        private DictionarySlim<Entity, EntityType> _typeMap = new();

        public int _entityId = 0;

        public Entity Entity()
        {
            return new Entity(Interlocked.Increment(ref _entityId) - 1);
        }

        public bool HasComponent<T>(Entity entity)
            => _typeMap.TryGetValue(entity, out var types) && types.ComponentTypes.Contains(ComponentId<T>.Id);



        public void Add<T>(Entity entity, T component)
        {
            _archetypes[ComponentId<T>.Id] = 
        }

        public struct View<T> : IEnumerable<T>
        {
            internal View(EntityContainer container)
            {
                _container = container;
            }

            private EntityContainer _container;

            public struct Enumerator : IEnumerator<T>
            {

            }
        }

        public View<T0> WithComponents<T0>()
        {
            return new View<T0>(this);
        }
    }
}
