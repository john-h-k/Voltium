#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        public Entity(int id)
        {
            Id = id;
            Version = 0;
        }
    }

    public sealed class EntityContainer
    {
        private int _lastEntity;

        public Entity CreateEntity()
        {
            return new Entity(Interlocked.Increment(ref _lastEntity));
        }
        
        public void AddComponent<TComponent>(Entity entity, TComponent component) where TComponent : struct
        {
            var components = ComponentPool<TComponent>.Components;
            components.Add(new TaggedComponent<TComponent>(entity, component));
        }

        public EntityView<T0> ViewOf<T0>() where T0 : struct 
            => new EntityView<T0>();
    }

    internal static class ComponentPool<TComponent> where TComponent : struct
    {
        public static List<TaggedComponent<TComponent>> Components = new();
    }

    internal struct SparseSet<T> where T : struct
    {
        public int[] Dense { get; private set; }
        public TaggedComponent<T>[] Sparse { get; private set; }

        public ref T this[int index] => ref Sparse[Dense[index]].Component;

        public void Add(in T val)
        {
        }
    }

    internal struct TaggedComponent<TComponent> where TComponent : struct
    {
        public Entity Entity;
        public TComponent Component;

        public TaggedComponent(Entity entity, TComponent component)
        {
            Entity = entity;
            Component = component;
        }
    }
}
